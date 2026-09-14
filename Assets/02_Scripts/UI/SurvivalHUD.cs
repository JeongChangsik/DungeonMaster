using DungeonMaster.Character.Enemy;
using DungeonMaster.Character.Player;
using TMPro;
using UnityEngine;

namespace DungeonMaster.UI
{
    // [하는 일] 무한 생존 모드의 현황판. 생존 시간 / 처치 수 / 레벨 세 가지를 화면에 보여준다.
    //          끝이 없는 게임이라 "얼마나 버텼는가"가 유일한 성과 지표인데,
    //          지금까지는 그걸 볼 방법이 전혀 없었다.
    // [붙이는 곳] Canvas 아래 빈 오브젝트에 붙여서 사용. 지금은 씬의 Canvas/HUD 에 붙어 있다.
    //          인스펙터의 Time Text / Stats Text 에는 HUD 아래 글자(생존시간, 현황)를,
    //          Player 에는 씬(Hierarchy)의 Warrior 를 끌어다 놓는다.
    // [연결] RoguelikePlayer.Level 을 읽어 레벨을 보여준다.
    //        RoguelikeEnemy.TotalKills 로 처치 수를 읽는다. static 값(모든 적이 함께 쓰는 숫자 하나)이라 적을 찾을 필요가 없다.
    //        GameOverUI 가 ElapsedSeconds 와 FormatTime 을 가져다 결과 화면에 쓴다.
    // [설계] 알림(이벤트)을 기다리지 않고 Update 에서 매 프레임 값을 확인한다.
    //        대신 숫자가 실제로 바뀐 프레임에만 글자를 다시 쓴다.
    //        (TextMeshPro: 유니티의 글자 표시 컴포넌트. text 에 문자열을 넣으면 화면 글자가 바뀐다)
    public class SurvivalHUD : MonoBehaviour
    {
        [Header("표시할 텍스트")]
        [Tooltip("화면 위쪽 가운데. 00:00 형식의 생존 시간")]
        [SerializeField] private TextMeshProUGUI _timeText;
        [Tooltip("화면 오른쪽 위. 레벨과 처치 수")]
        [SerializeField] private TextMeshProUGUI _statsText;

        [Header("참조")]
        [SerializeField] private RoguelikePlayer _player;

        // 판이 시작된 순간의 Time.time. 지금 Time.time 에서 이 값을 빼면 버틴 시간이 나온다
        private float _startTime;

        // 결과 화면이 읽어간다.
        // 죽는 순간 시간이 멈추므로(timeScale 0) 결과 화면이 읽을 때는 죽은 순간의 값에 머물러 있다
        public float ElapsedSeconds { get { return Time.time - _startTime; } }

        // 00:00 형식으로. 결과 화면과 HUD 가 같은 규칙을 쓰도록 여기 모아둔다
        // static 이라 SurvivalHUD 오브젝트를 찾지 않고도 SurvivalHUD.FormatTime(...) 으로 부를 수 있다
        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        // 마지막으로 화면에 쓴 값. 매 프레임 문자열을 새로 만들면 GC 가 계속 쌓인다
        private int _shownSeconds = -1;
        private int _shownKills = -1;
        private int _shownLevel = -1;

        private void Awake()
        {
            // Time.time 을 쓰므로 레벨업 카드로 시간이 멈춘 동안은 생존 시간도 멈춘다.
            // 카드 고르는 시간까지 기록에 넣으면 오래 들여다볼수록 유리해지므로 이게 맞다
            _startTime = Time.time;

            // 에디터의 "도메인 리로드 끄기" 설정에서는 static 값이 플레이 사이에 남는다.
            // 새 판을 시작할 때마다 반드시 0으로 되돌린다
            RoguelikeEnemy.TotalKills = 0;

            CheckReferences();
        }

        private void Update()
        {
            UpdateTime();
            UpdateStats();
        }

        // 생존 시간 글자를 갱신한다. 매 프레임 불리지만 초가 바뀐 프레임에만 실제로 글자를 쓴다
        private void UpdateTime()
        {
            if (_timeText == null) return;

            int total = Mathf.FloorToInt(ElapsedSeconds);
            if (total == _shownSeconds) return;     // 초가 바뀔 때만 다시 쓴다
            _shownSeconds = total;

            _timeText.text = FormatTime(total);
        }

        // 레벨과 처치 수 글자를 갱신한다. 둘 중 하나라도 바뀐 프레임에만 다시 쓴다
        private void UpdateStats()
        {
            if (_statsText == null) return;

            int kills = RoguelikeEnemy.TotalKills;
            int level = _player != null ? _player.Level : 1;
            if (kills == _shownKills && level == _shownLevel) return;

            _shownKills = kills;
            _shownLevel = level;
            _statsText.text = "Lv." + level + "\n처치 " + kills;
        }

        // 인스펙터 칸이 비어 있으면 조용히 아무것도 안 뜨는 대신,
        // 무엇을 어디에 끌어다 놓아야 하는지 한 번에 알려준다
        private void CheckReferences()
        {
            if (_timeText == null)
                Debug.LogError("SurvivalHUD::CheckReferences() Time Text 가 비어 있습니다. Canvas/HUD/생존시간 을 끌어다 놓으세요.");
            if (_statsText == null)
                Debug.LogError("SurvivalHUD::CheckReferences() Stats Text 가 비어 있습니다. Canvas/HUD/현황 을 끌어다 놓으세요.");
            if (_player == null)
                Debug.LogError("SurvivalHUD::CheckReferences() Player 가 비어 있습니다. 씬(Hierarchy)의 Warrior 를 끌어다 놓으세요. 프리팹이 아니라 씬 오브젝트입니다.");
        }
    }
}
