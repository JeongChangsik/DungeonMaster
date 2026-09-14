using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonMaster.UI
{
    // [하는 일] 지금까지 고른 업그레이드를 화면 아래쪽에 아이콘으로 늘어놓는다. 2레벨 이상이면 레벨 숫자도 붙인다.
    //          레벨업을 열 번쯤 하고 나면 무엇을 골랐는지 기억나지 않는다.
    //          뱀서라이크는 "무엇을 모을지" 고르는 게임이라, 지금 내 구성을 볼 수 없으면
    //          다음 카드를 고를 때 판단할 근거가 없다.
    // [붙이는 곳] 씬의 Canvas/HUD/빌드 오브젝트에 붙어 있다.
    //          칸(아이콘 Image + 레벨 글자)은 씬에 미리 만들어 두고,
    //          Slot Icons 와 Slot Levels 에 같은 순서, 같은 개수로 끌어다 놓는다.
    //          Level Up UI 칸에는 Canvas 아래의 LevelUpUI 를 넣는다.
    // [연결] LevelUpUI : OnUpgradesChanged 알림을 듣고, CopyAcquired 로 고른 목록을 받아온다
    //        UpgradeSO : 아이콘 그림으로 Image 를 쓴다
    // [설계] 매 프레임 확인하지 않고, "업그레이드가 바뀌었다"는 알림이 올 때만 다시 그린다.
    //        칸을 코드로 새로 만들지 않고 미리 만든 칸만 쓴다. 칸보다 업그레이드가 많으면 넘치는 것은 안 보인다.
    public class BuildHUD : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private LevelUpUI _levelUpUI;
        [Tooltip("미리 만들어 둔 칸들. 이 개수만큼만 표시된다")]
        [SerializeField] private Image[] _slotIcons;
        // 칸마다 레벨 숫자를 쓸 글자. _slotIcons 와 같은 순서, 같은 개수여야 한다
        [SerializeField] private TextMeshProUGUI[] _slotLevels;

        // 매번 새로 만들지 않기 위해 재사용한다
        private readonly List<UpgradeSO> _upgrades = new List<UpgradeSO>();
        private readonly List<int> _levels = new List<int>();

        private void Awake()
        {
            CheckReferences();
            HideAll();
        }

        // 켜질 때 알림을 구독(+=)하고, 꺼질 때 해제(-=)한다. 짝을 맞춰야 꺼진 오브젝트에 알림이 가지 않는다.
        // 켜지자마자 한 번 Refresh 하는 것은, 꺼져 있던 동안 고른 업그레이드까지 바로 반영하기 위해서다
        private void OnEnable()
        {
            if (_levelUpUI == null) return;
            _levelUpUI.OnUpgradesChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_levelUpUI == null) return;
            _levelUpUI.OnUpgradesChanged -= Refresh;
        }

        // 업그레이드를 고를 때만 불린다. 매 프레임 도는 코드가 아니다
        private void Refresh()
        {
            if (_slotIcons == null || _levelUpUI == null) return;

            _levelUpUI.CopyAcquired(_upgrades, _levels);

            for (int i = 0; i < _slotIcons.Length; i++)
            {
                bool used = i < _upgrades.Count;

                if (_slotIcons[i] != null)
                {
                    _slotIcons[i].enabled = used;
                    if (used) _slotIcons[i].sprite = _upgrades[i].Image;
                }

                if (_slotLevels[i] != null)
                {
                    _slotLevels[i].enabled = used;
                    // 레벨 1짜리는 숫자를 안 띄운다. 칸마다 '1'이 붙으면 지저분하고
                    // 정작 중요한 "많이 올린 것"이 눈에 안 들어온다
                    if (used) _slotLevels[i].text = _levels[i] > 1 ? _levels[i].ToString() : "";
                }
            }
        }

        // 시작할 때 모든 칸을 숨긴다. 아직 고른 것이 없으니 빈 칸이 보이면 안 된다
        private void HideAll()
        {
            if (_slotIcons == null) return;
            for (int i = 0; i < _slotIcons.Length; i++)
            {
                if (_slotIcons[i] != null) _slotIcons[i].enabled = false;
                if (_slotLevels != null && i < _slotLevels.Length && _slotLevels[i] != null)
                    _slotLevels[i].enabled = false;
            }
        }

        // 인스펙터 칸이 비었거나 두 칸 목록의 개수가 다르면 콘솔에 무엇이 잘못됐는지 알려준다
        private void CheckReferences()
        {
            if (_levelUpUI == null)
                Debug.LogError("BuildHUD::CheckReferences() Level Up UI 가 비어 있습니다. Canvas/LevelUpUI 를 끌어다 놓으세요.");
            if (_slotIcons == null || _slotIcons.Length == 0)
                Debug.LogError("BuildHUD::CheckReferences() Slot Icons 가 비어 있습니다.");
            else if (_slotLevels == null || _slotLevels.Length != _slotIcons.Length)
                Debug.LogError("BuildHUD::CheckReferences() Slot Levels 개수가 Slot Icons 와 다릅니다.");
        }
    }
}
