using UnityEngine;
using DungeonMaster.Character.Player;
using DungeonMaster.Weapon;

// 무기를 해금하거나 강화하는 카드.
//
// [하는 일] 고르면 정해진 무기를 처음 열어 주거나(해금 카드), 이미 가진 무기의 값 하나를 올린다(강화 카드).
// [붙이는 곳] 에셋으로 만든다. Project 창 우클릭 > Create > DungeonMaster > Upgrade > 무기 강화.
//          대상 무기, 올릴 스탯, 증가량, 해금 카드 여부를 적고 LevelUpUI 의 _pool 목록에 넣으면 카드로 등장한다.
//          (예: "폭탄.asset" 은 해금 카드, "폭탄 추가.asset" 은 강화 카드다)
//          게임 시작부터 들고 있을 무기라면 해금 카드를 LevelUpUI 의 _startingUpgrades 에 넣는다.
// [연결] RoguelikePlayer.Weapons(WeaponManager)에서 WeaponId 로 무기를 찾아
//          WeaponBase.Unlock() 과 WeaponBase.AddStat() 을 부른다. 스탯을 실제로 어떻게 반영할지는 각 무기가 정한다.
// [설계] 무기 종류 × 스탯 조합을 이 클래스 하나로 전부 커버한다.
//          조합마다 .cs 를 만들면 무기 4종 × 스탯 4개만 해도 16개가 되므로 그렇게 하지 않는다.
//          새 무기 카드는 코드 없이 에셋만 만들면 된다. 새 "무기" 자체를 넣을 때만
//          WeaponId 에 한 줄을 추가하고, WeaponBase 를 물려받은 무기를 플레이어의 WeaponRoot 아래에 붙인다.
[CreateAssetMenu(menuName = "DungeonMaster/Upgrade/무기 강화")]
public class WeaponUpgradeSO : UpgradeSO
{
    // 어느 무기의(_target) 어떤 값을(_stat) 올릴지. 해금 카드라도 _stat 은 채워진다(기본 Damage).
    // 그 무기가 받지 않는 스탯을 고르면 조용히 무시된다(예: 폭탄에 Pierce)
    [Header("대상")]
    [SerializeField] private WeaponId _target;
    [SerializeField] private WeaponStat _stat = WeaponStat.Damage;

    [Header("증가량 (안 쓰는 쪽은 0 으로 둘 것)")]
    [Tooltip("고정 수치로 더한다. 개수 카드는 여기에 1 을 넣는다")]
    [SerializeField] private float _flat = 0f;

    [Tooltip("비율로 더한다. 0.1 = +10%")]
    [SerializeField] private float _percent = 0f;

    [Header("이 카드가 무기를 처음 열어주는 카드인가")]
    [Tooltip("체크하면 아직 안 가진 무기일 때만 후보에 오른다. Max Level 은 1 로 두는 것을 권장")]
    [SerializeField] private bool _isUnlockCard = false;

    // 아직 안 가진 무기의 '강화' 카드가 뽑히거나,
    // 이미 가진 무기의 '해금' 카드가 또 뽑히는 걸 막는다.
    // LevelUpUI 가 카드를 뽑기 직전에 부른다. 플레이어에게 그 무기 오브젝트가 아예 없으면 절대 뽑히지 않는다
    public override bool IsAvailable(RoguelikePlayer player)
    {
        WeaponBase w = FindWeapon(player);
        if (w == null) return false;

        return _isUnlockCard ? !w.IsUnlocked : w.IsUnlocked;
    }

    // 카드를 고를 때마다 LevelUpUI 가 한 번씩 부른다(level 은 쓰지 않는다. 고를 때마다 같은 양이 쌓인다).
    // 해금 카드와 강화 카드가 똑같이 "Unlock 하고 AddStat" 을 거친다.
    // 해금 카드는 증가량을 0 으로 두면 여는 일만 하게 되고, 강화 카드는 이미 열려 있으니 Unlock 이 그냥 넘어간다
    public override void Apply(RoguelikePlayer player, int level)
    {
        WeaponBase w = FindWeapon(player);
        if (w == null)
        {
            Debug.LogError($"WeaponUpgradeSO::Apply() '{_target}' 무기를 찾지 못했습니다. WeaponRoot 아래에 있는지 확인하세요.");
            return;
        }

        w.Unlock();     // 멱등. 이미 열려 있으면 아무 일도 안 함
        w.AddStat(_stat, _flat, _percent);
    }

    // 플레이어가 가진 무기 중 _target 종류를 찾는다. 없으면 null.
    // SO 에셋은 씬 오브젝트를 미리 연결해 둘 수 없어서(파일이라 씬 밖에 있다) 쓸 때마다 플레이어에게 물어본다
    private WeaponBase FindWeapon(RoguelikePlayer player)
    {
        if (player == null || player.Weapons == null) return null;
        return player.Weapons.Get(_target);
    }
}
