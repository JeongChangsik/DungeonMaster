using System.Collections;
using DungeonMaster.Character.Enemy;
using DungeonMaster.Character.Player;
using DungeonMaster.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonMaster.UI
{
    // 죽은 뒤 암전 -> 결과 화면 -> 재시작.
    //
    // [하는 일] 플레이어가 죽으면 화면을 서서히 어둡게 한 뒤, 생존 시간 / 처치 수 / 레벨과 최고 기록을 보여준다.
    //          생존 시간이 최고 기록보다 길면 새 기록으로 저장한다.
    // [붙이는 곳] 씬의 Canvas/GameOverUI 오브젝트. 이 오브젝트 자체는 켜 둬야 한다.
    //          꺼져 있으면 OnEnable 이 불리지 않아 사망 알림을 못 듣는다. 평소에 숨기는 것은 Panel 과 Dim 뿐이다.
    //          인스펙터에서 Player 에 씬의 Warrior, Hud 에 Canvas/HUD, 나머지 칸에 결과 화면의 이미지 / 글자 / 버튼을 끌어다 놓는다.
    // [연결] RoguelikePlayer : OnDied 를 듣는다. Level 을 읽는다
    //        SurvivalHUD     : 생존 시간(ElapsedSeconds)과 00:00 표시 규칙(FormatTime)을 빌린다
    //        RoguelikeEnemy  : TotalKills 로 처치 수를 읽는다
    //        GameFlow        : 다시 하기 버튼의 실제 처리
    // [설계] 무한 생존이라 "얼마나 버텼는가"가 유일한 성과다.
    //        그래서 결과 화면의 주인공은 생존 시간이고, 최고 기록과 나란히 보여준다.
    //        화면이 멈춘(timeScale 0) 상태에서 돌아가므로 시간 계산은 전부 unscaled 를 쓴다.
    //        최고 기록은 PlayerPrefs 키 DM_BestTime / DM_BestKills / DM_BestLevel 에 저장한다.
    //        (PlayerPrefs: 게임을 껐다 켜도 남는 작은 저장 공간. 이름표(키)를 붙여 숫자나 글자를 넣고 꺼낸다)
    public class GameOverUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private RoguelikePlayer _player;
        [SerializeField] private SurvivalHUD _hud;

        [Header("화면 요소")]
        [Tooltip("결과 내용을 담은 오브젝트. 평소에는 꺼져 있다")]
        [SerializeField] private GameObject _panel;
        [Tooltip("화면 전체를 덮는 검은 이미지")]
        [SerializeField] private Image _dim;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private TextMeshProUGUI _bestText;
        [SerializeField] private Button _restartButton;

        [Header("연출")]
        [Tooltip("화면이 어두워지는 시간")]
        [SerializeField] private float _dimDuration = 0.6f;
        [Tooltip("어두워진 뒤 결과가 뜰 때까지 잠깐 두는 시간")]
        [SerializeField] private float _panelDelay = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float _dimAlpha = 0.85f;

        // 최고 기록은 판이 끝나도 남아야 하므로 PlayerPrefs 에 저장한다
        private const string KeyBestTime = "DM_BestTime";
        private const string KeyBestKills = "DM_BestKills";
        private const string KeyBestLevel = "DM_BestLevel";

        // 시작할 때 결과 화면을 숨기고, 다시 하기 버튼에 Restart 를 연결한다
        private void Awake()
        {
            CheckReferences();

            // 평소에는 아무것도 안 보여야 한다
            if (_panel != null) _panel.SetActive(false);
            if (_dim != null)
            {
                _dim.enabled = false;
                Color c = _dim.color; c.a = 0f; _dim.color = c;
            }

            if (_restartButton != null) _restartButton.onClick.AddListener(Restart);
        }

        // 켜질 때 사망 알림을 구독(+=)하고, 꺼질 때 해제(-=)한다.
        // 짝을 맞춰 두어야 사라진 오브젝트에 알림이 가는 일이 없다
        private void OnEnable()
        {
            if (_player != null) _player.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (_player != null) _player.OnDied -= HandleDied;
        }

        // 플레이어가 죽으면 불린다. 연출이 여러 프레임에 걸치므로 코루틴으로 돌린다
        private void HandleDied()
        {
            StartCoroutine(ShowCo());
        }

        // 암전 -> 잠깐 대기 -> 결과 표시.
        // timeScale 이 0이라 WaitForSeconds 는 영원히 끝나지 않으므로 WaitForSecondsRealtime(실제 시간)을 쓴다
        private IEnumerator ShowCo()
        {
            // 1) 서서히 어두워진다
            if (_dim != null)
            {
                _dim.enabled = true;
                float t = 0f;
                while (t < _dimDuration)
                {
                    t += Time.unscaledDeltaTime;
                    Color c = _dim.color;
                    c.a = Mathf.Lerp(0f, _dimAlpha, t / _dimDuration);
                    _dim.color = c;
                    yield return null;
                }
                Color end = _dim.color; end.a = _dimAlpha; _dim.color = end;
            }

            yield return new WaitForSecondsRealtime(_panelDelay);

            // 2) 결과를 채워서 띄운다
            Fill();
            if (_panel != null) _panel.SetActive(true);
        }

        // 결과 글자를 채우고, 생존 시간이 최고 기록보다 길면 새 기록으로 저장한다
        private void Fill()
        {
            float elapsed = _hud != null ? _hud.ElapsedSeconds : 0f;
            int kills = RoguelikeEnemy.TotalKills;
            int level = _player != null ? _player.Level : 1;

            // 저장하기 전에 읽어 둔 값이라, 신기록일 때도 bestTime 은 "이전" 최고 기록이다.
            // 저장된 적이 없으면 두 번째 값(0)을 돌려준다
            float bestTime = PlayerPrefs.GetFloat(KeyBestTime, 0f);
            bool isNewRecord = elapsed > bestTime;

            // 신기록 판정은 생존 시간 하나로만 한다. 처치 수와 레벨은 그 판의 값을 함께 적어 두는 것이다
            if (isNewRecord)
            {
                PlayerPrefs.SetFloat(KeyBestTime, elapsed);
                PlayerPrefs.SetInt(KeyBestKills, kills);
                PlayerPrefs.SetInt(KeyBestLevel, level);
                // Save: 지금 바로 파일에 쓴다. 안 부르면 게임이 갑자기 꺼질 때 기록이 날아갈 수 있다
                PlayerPrefs.Save();
            }

            if (_titleText != null) _titleText.text = isNewRecord ? "신기록!" : "사망";
            if (_timeText != null) _timeText.text = SurvivalHUD.FormatTime(elapsed);
            if (_statsText != null) _statsText.text = "처치 " + kills + "   레벨 " + level;

            if (_bestText != null)
            {
                // 첫 판이면 비교할 기록이 없다
                if (bestTime <= 0f) _bestText.text = "첫 기록입니다";
                else if (isNewRecord) _bestText.text = "이전 최고 " + SurvivalHUD.FormatTime(bestTime);
                else _bestText.text = "최고 기록 " + SurvivalHUD.FormatTime(bestTime);
            }
        }

        // 버튼이 호출한다. 시간이 멈춰 있어도 UI 버튼은 눌린다.
        // 실제 처리는 GameFlow 에 모아뒀다. 일시정지 메뉴도 같은 것을 쓴다
        public void Restart()
        {
            GameFlow.RestartScene();
        }

        // 인스펙터 칸이 비어 있으면 조용히 안 뜨는 대신, 무엇을 끌어다 놓아야 하는지 콘솔에 알려준다
        private void CheckReferences()
        {
            if (_player == null)
                Debug.LogError("GameOverUI::CheckReferences() Player 가 비어 있습니다. 씬의 Warrior 를 끌어다 놓으세요.");
            if (_hud == null)
                Debug.LogError("GameOverUI::CheckReferences() Hud 가 비어 있습니다. Canvas/HUD 를 끌어다 놓으세요.");
            if (_panel == null)
                Debug.LogError("GameOverUI::CheckReferences() Panel 이 비어 있습니다.");
            if (_dim == null)
                Debug.LogError("GameOverUI::CheckReferences() Dim(검은 이미지)이 비어 있습니다.");
            if (_restartButton == null)
                Debug.LogError("GameOverUI::CheckReferences() Restart Button 이 비어 있습니다.");
        }
    }
}
