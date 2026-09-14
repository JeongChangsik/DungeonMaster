using DungeonMaster.Character.Player;
using DungeonMaster.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DungeonMaster.UI
{
    // ESC 로 여닫는 옵션 메뉴. 열려 있는 동안 게임은 멈춘다.
    //
    // [하는 일] ESC 로 일시정지 메뉴를 열고 닫는다. 메뉴 안에서 소리 크기 조절, 다시 시작, 게임 종료를 할 수 있다.
    // [붙이는 곳] 씬의 Canvas/PauseMenuUI 오브젝트. 이 오브젝트 자체는 켜 둔 채로 두고, Panel 과 Dim 만 코드가 켜고 끈다.
    //          (이 오브젝트가 꺼져 있으면 Update 가 돌지 않아 ESC 를 못 받는다)
    //          인스펙터에서 Player 에 씬의 Warrior, Level Up UI 에 Canvas 아래 LevelUpUI, 나머지 칸에 메뉴 안의 슬라이더 / 글자 / 버튼을 넣는다.
    //          버튼과 슬라이더의 기능은 인스펙터의 On Click 칸이 아니라 Awake 에서 코드로 연결한다.
    // [연결] RoguelikePlayer : OnDied 를 듣고, 메뉴를 열 때 CancelHitStop 을 부른다
    //        LevelUpUI       : 닫을 때 IsShowing 으로 카드가 떠 있는지 물어본다
    //        GameFlow        : 다시 시작 / 게임 종료
    // [설계] 이 게임에서 시간을 멈추는 주체가 이미 셋 있다 — 피격 히트스톱, 레벨업 카드, 사망.
    //        여기가 넷째다. 그래서 "멈추는 것"보다 "다시 흐르게 하는 것"이 어렵다.
    //        자세한 내용은 RestoreTime() 참고.
    //        소리 크기는 AudioListener.volume(게임 전체 소리에 걸리는 볼륨 손잡이 하나)에 걸고,
    //        PlayerPrefs(게임을 꺼도 남는 작은 저장 공간) 키 DM_Volume 에 저장한다. 이유는 OnVolumeChanged() 참고.
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private RoguelikePlayer _player;
        [Tooltip("카드가 떠 있는지 물어보기 위해 필요하다")]
        [SerializeField] private LevelUpUI _levelUpUI;

        [Header("화면 요소")]
        [Tooltip("메뉴 내용을 담은 오브젝트. 평소에는 꺼져 있다")]
        [SerializeField] private GameObject _panel;
        [Tooltip("화면을 덮는 반투명 이미지. 뒤쪽 버튼이 눌리는 것도 막아준다")]
        [SerializeField] private Image _dim;
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private TextMeshProUGUI _volumeValueText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;

        // PlayerPrefs 에 소리 크기를 넣고 꺼낼 때 쓰는 이름표
        private const string KeyVolume = "DM_Volume";

        private bool _isOpen;

        // 한 번 죽으면 true 로 남는다. 다시 시작하면 씬과 함께 이 컴포넌트도 새로 만들어져 false 로 돌아간다
        private bool _playerDead;

        // 밖에서는 읽기만 할 수 있게 내놓은 "지금 열려 있는가"
        public bool IsOpen { get { return _isOpen; } }

        // 시작할 때 저장된 소리 크기를 적용하고, 슬라이더와 버튼에 함수를 연결한 뒤 메뉴를 숨긴다
        private void Awake()
        {
            CheckReferences();

            // 저장해둔 소리 크기를 게임 시작 때부터 적용한다.
            // 메뉴를 열어야만 적용되면 첫 판이 항상 기본값으로 시작한다
            float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVolume, 1f));
            AudioListener.volume = saved;

            if (_volumeSlider != null)
            {
                _volumeSlider.minValue = 0f;
                _volumeSlider.maxValue = 1f;
                // SetValueWithoutNotify: 손잡이 위치만 옮기고 "값이 바뀌었다" 알림은 보내지 않는다.
                // 시작하자마자 괜히 저장이 한 번 더 일어나지 않게 한다
                _volumeSlider.SetValueWithoutNotify(saved);
                // AddListener: "값이 바뀌면 이 함수를 불러 달라"고 등록한다
                _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            ShowVolumeValue(saved);

            // GameFlow 는 static 클래스라 오브젝트 없이 함수 이름을 그대로 버튼에 걸 수 있다
            if (_restartButton != null) _restartButton.onClick.AddListener(GameFlow.RestartScene);
            if (_quitButton != null) _quitButton.onClick.AddListener(GameFlow.QuitGame);

            if (_panel != null) _panel.SetActive(false);
            if (_dim != null) _dim.enabled = false;
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
            _playerDead = true;

            // 죽는 순간 메뉴가 열려 있었다면 닫는다. 결과 화면과 겹치면 지저분하다
            if (_isOpen) Close();
        }

        private void Update()
        {
            // Update 는 timeScale 이 0이어도 계속 돈다. 그래서 멈춘 상태에서도 ESC 가 먹는다.
            // 입력 액션 대신 키를 직접 보는 이유: 이 메뉴는 플레이어가 죽은 뒤에도 동작해야 하는데,
            // 플레이어에 붙은 InputHandler 에 얹으면 거기에 묶여버린다
            if (Keyboard.current == null) return;
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

            Toggle();
        }

        // ESC 한 번에 열려 있으면 닫고, 닫혀 있으면 연다
        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        // 메뉴를 열고 시간을 멈춘다. 이미 열려 있거나 플레이어가 죽었으면 아무것도 안 한다
        public void Open()
        {
            if (_isOpen) return;

            // 죽은 뒤에는 결과 화면에 이미 다시 하기와 기록이 있다
            if (_playerDead) return;

            _isOpen = true;

            // 진행 중인 히트스톱의 시간 소유권을 뺏는다.
            // 안 그러면 0.05초 뒤에 히트스톱이 끝나면서 시간을 1로 되돌려,
            // 메뉴가 떠 있는데 게임이 계속 돌아간다
            if (_player != null) _player.CancelHitStop();

            Time.timeScale = 0f;

            if (_dim != null) _dim.enabled = true;
            if (_panel != null) _panel.SetActive(true);
        }

        // 메뉴를 닫는다. ESC 를 다시 누를 때, 그리고 메뉴가 열린 채로 플레이어가 죽을 때 불린다.
        // 시간을 되돌리는 일은 RestoreTime 에 맡긴다
        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;

            if (_panel != null) _panel.SetActive(false);
            if (_dim != null) _dim.enabled = false;

            RestoreTime();
        }

        // 닫을 때 시간을 무조건 1로 되돌리면 안 된다.
        //
        // 카드가 떠 있는 채로 메뉴를 열었다가 닫으면, 카드는 그대로인데 게임만 다시 흐른다.
        // "열기 전 값을 기억했다가 되돌리는" 방법도 안전하지 않다.
        // 경험치 바가 UnscaledTime 이라 멈춰 있는 동안에도 계속 차오르고,
        // 메뉴가 열려 있는 사이에 레벨업이 새로 떠버릴 수 있기 때문이다.
        //
        // 그래서 기억하지 않고, 닫는 순간에 실제 상태를 물어본다.
        private void RestoreTime()
        {
            if (_playerDead) return;                                   // 사망 연출이 시간을 쥐고 있다
            if (_levelUpUI != null && _levelUpUI.IsShowing) return;     // 카드가 계속 멈춰 있어야 한다

            Time.timeScale = 1f;
        }

        // 소리 크기 슬라이더를 움직일 때마다 불린다. 바로 적용하고 바로 저장한다
        private void OnVolumeChanged(float value)
        {
            value = Mathf.Clamp01(value);

            // 전체 소리 크기. BGM 과 효과음에 한 번에 걸린다.
            // AudioDataSO.volume 을 쓰지 않는 이유가 두 가지 있다.
            //  1) BGM 은 재생을 시작할 때 한 번만 읽어서, 바꿔도 즉시 반영되지 않는다
            //  2) ScriptableObject 라 에디터에서 값이 에셋에 영구 저장된다
            AudioListener.volume = value;

            PlayerPrefs.SetFloat(KeyVolume, value);
            PlayerPrefs.Save();

            ShowVolumeValue(value);
        }

        // 0~1 값을 0~100% 글자로 바꿔 슬라이더 옆에 보여준다
        private void ShowVolumeValue(float value)
        {
            if (_volumeValueText == null) return;
            _volumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        // 인스펙터 칸이 비어 있으면 무엇을 끌어다 놓아야 하는지 콘솔에 알려준다
        private void CheckReferences()
        {
            if (_player == null)
                Debug.LogError("PauseMenuUI::CheckReferences() Player 가 비어 있습니다. 씬의 Warrior 를 끌어다 놓으세요.");
            if (_levelUpUI == null)
                Debug.LogError("PauseMenuUI::CheckReferences() Level Up UI 가 비어 있습니다. Canvas/LevelUpUI 를 끌어다 놓으세요. 카드가 떠 있는지 확인하는 데 필요합니다.");
            if (_panel == null)
                Debug.LogError("PauseMenuUI::CheckReferences() Panel 이 비어 있습니다.");
            if (_volumeSlider == null)
                Debug.LogError("PauseMenuUI::CheckReferences() Volume Slider 가 비어 있습니다.");
        }
    }
}
