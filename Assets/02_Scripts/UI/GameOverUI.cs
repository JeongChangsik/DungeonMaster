using System.Collections;
using DungeonMaster.Character.Enemy;
using DungeonMaster.Character.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonMaster.UI
{
    // 죽은 뒤 암전 -> 결과 화면 -> 재시작.
    //
    // 무한 생존이라 "얼마나 버텼는가"가 유일한 성과다.
    // 그래서 결과 화면의 주인공은 생존 시간이고, 최고 기록과 나란히 보여준다.
    //
    // 화면이 멈춘(timeScale 0) 상태에서 돌아가므로 시간 계산은 전부 unscaled 를 쓴다.
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

        private void OnEnable()
        {
            if (_player != null) _player.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (_player != null) _player.OnDied -= HandleDied;
        }

        private void HandleDied()
        {
            StartCoroutine(ShowCo());
        }

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

        private void Fill()
        {
            float elapsed = _hud != null ? _hud.ElapsedSeconds : 0f;
            int kills = RoguelikeEnemy.TotalKills;
            int level = _player != null ? _player.Level : 1;

            float bestTime = PlayerPrefs.GetFloat(KeyBestTime, 0f);
            bool isNewRecord = elapsed > bestTime;

            if (isNewRecord)
            {
                PlayerPrefs.SetFloat(KeyBestTime, elapsed);
                PlayerPrefs.SetInt(KeyBestKills, kills);
                PlayerPrefs.SetInt(KeyBestLevel, level);
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

        // 버튼이 호출한다. 시간이 멈춰 있어도 UI 버튼은 눌린다
        public void Restart()
        {
            // 되돌리지 않으면 새 판이 멈춘 채로 시작한다
            Time.timeScale = 1f;

            // 창고는 씬을 다시 불러와도 살아남는다.
            // 비우지 않으면 죽기 직전의 적과 코인이 새 판에 그대로 남는다
            if (ObjectPool.Instance != null) ObjectPool.Instance.ReleaseAllActive();

            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

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
