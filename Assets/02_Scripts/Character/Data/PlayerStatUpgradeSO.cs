using UnityEngine;
using DungeonMaster.Character.Player;

// 플레이어 본체 스탯을 올리는 패시브 카드.
// 무기와 무관한 강화는 전부 이 클래스 하나로 커버한다.
// 새 스탯을 추가하고 싶으면 enum 에 한 줄 + RoguelikePlayer.ApplyStatUpgrade 에 case 한 줄이면 되고,
// .cs 파일을 새로 만들 필요가 없다.
public enum PlayerStat
{
    MaxHp,              // 최대 체력
    MoveSpeed,          // 이동 속도
    PickupRadius,       // 경험치 자석 범위
    ExpGain,            // 경험치 획득량
    CooldownReduction,  // 공격 쿨다운 감소

    // --- 아래는 모든 무기에 공통으로 적용되는 배율 ---
    WeaponDamage,       // 모든 무기 피해량
    WeaponArea,         // 모든 무기 범위
    WeaponRate,         // 모든 무기 속도

    HealthRegen,        // 초당 체력 회복
}

[CreateAssetMenu(menuName = "DungeonMaster/Upgrade/플레이어 스탯")]
public class PlayerStatUpgradeSO : UpgradeSO
{
    [Header("무엇을 올릴 것인가")]
    [SerializeField] private PlayerStat _stat;

    [Header("증가량 (안 쓰는 쪽은 0 으로 둘 것)")]
    [Tooltip("고정 수치로 더한다. 예: 최대 체력 +20")]
    [SerializeField] private float _flat = 0f;

    [Tooltip("비율로 더한다. 0.1 = +10%")]
    [SerializeField] private float _percent = 0f;

    public override void Apply(RoguelikePlayer player, int level)
    {
        player.ApplyStatUpgrade(_stat, _flat, _percent);
    }
}
