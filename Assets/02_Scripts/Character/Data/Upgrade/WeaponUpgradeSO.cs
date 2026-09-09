using UnityEngine;
using DungeonMaster.Character.Player;
using DungeonMaster.Weapon;

// 무기를 해금하거나 강화하는 카드.
// 무기 종류 × 스탯 조합을 이 클래스 하나로 전부 커버한다.
// 무기 4종 × 스탯 4개마다 .cs 를 만들면 16개가 되므로 그렇게 하지 않는다.
[CreateAssetMenu(menuName = "DungeonMaster/Upgrade/무기 강화")]
public class WeaponUpgradeSO : UpgradeSO
{
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
    // 이미 가진 무기의 '해금' 카드가 또 뽑히는 걸 막는다
    public override bool IsAvailable(RoguelikePlayer player)
    {
        WeaponBase w = FindWeapon(player);
        if (w == null) return false;

        return _isUnlockCard ? !w.IsUnlocked : w.IsUnlocked;
    }

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

    private WeaponBase FindWeapon(RoguelikePlayer player)
    {
        if (player == null || player.Weapons == null) return null;
        return player.Weapons.Get(_target);
    }
}
