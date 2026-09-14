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
    // 이 게임에서 시간을 멈추는 주체가 이미 셋 있다 — 피격 히트스톱, 레벨업 카드, 사망.
    // 여기가 넷째다. 그래서 "멈추는 것"보다 "다시 흐르게 하는 것"이 어렵다.
    // 자세한 내용은 RestoreTime() 참고.
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

        private const string KeyVolume = "DM_Volume";

        private bool _isOpen;
        private bool _playerDead;

        public bool IsOpen { get { return _isOpen; } }

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
                _volumeSlider.SetValueWithoutNotify(saved);
                _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            ShowVolumeValue(saved);

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

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

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

        private void ShowVolumeValue(float value)
        {
            if (_volumeValueText == null) return;
            _volumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
        }

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
