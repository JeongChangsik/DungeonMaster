using UnityEngine;
using DungeonMaster.Character.Player;

// 카드가 올릴 수 있는 플레이어 스탯 목록. 인스펙터의 _stat 드롭다운에 이 이름들이 나온다.
// 새 스탯은 여기에 한 줄 + RoguelikePlayer.ApplyStatUpgrade 에 case 한 줄이면 된다.
// ※ 맨 뒤에만 추가할 것. 유니티는 enum 을 이름이 아니라 순서 번호로 저장해서,
//   중간에 끼워 넣으면 이미 만든 카드 에셋들이 엉뚱한 스탯을 가리키게 된다.
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
    Defense,            // 방어력. 맞을 때마다 피해에서 깎인다
}

// 플레이어 본체 스탯을 올리는 패시브 카드.
//
// [하는 일] 고르면 최대 체력, 이동 속도, 모든 무기 피해량 같은 "플레이어 쪽" 값을 올린다.
// [붙이는 곳] 에셋으로 만든다. Project 창 우클릭 > Create > DungeonMaster > Upgrade > 플레이어 스탯.
//          만든 에셋에 제목/설명/그림, 올릴 스탯, 증가량을 적고 LevelUpUI 의 _pool 목록에 넣으면 카드로 등장한다.
//          (예: "최대 체력 증가.asset", "이동 속도 증가.asset" 이 이렇게 만들어졌다)
// [연결] 카드를 고르면 LevelUpUI 가 Apply 를 부르고, 이 카드는 RoguelikePlayer.ApplyStatUpgrade 에 값만 넘긴다.
//          실제로 어느 변수를 얼마나 올릴지는 RoguelikePlayer 쪽 switch 가 정한다.
// [설계] 무기와 무관한 강화는 전부 이 클래스 하나로 커버한다.
//          "체력 카드", "속도 카드"마다 .cs 를 따로 만들지 않고, 스탯 종류를 드롭다운으로 고르게 했다.
//          그래서 새 카드는 코드 없이 에셋만 하나 더 만들면 되고, .cs 파일을 새로 만들 필요가 없다.
[CreateAssetMenu(menuName = "DungeonMaster/Upgrade/플레이어 스탯")]
public class PlayerStatUpgradeSO : UpgradeSO
{
    [Header("무엇을 올릴 것인가")]
    [SerializeField] private PlayerStat _stat;

    // 스탯마다 flat 과 percent 중 한쪽만 쓰기도 한다(RoguelikePlayer.ApplyStatUpgrade 기준).
    // 예: 경험치 획득량/쿨다운 감소/무기 전역 배율은 percent 만, 체력 회복/방어력은 flat 만 본다.
    // 안 보는 쪽에 숫자를 넣으면 조용히 무시되니, 효과가 없으면 여기부터 확인한다
    [Header("증가량 (안 쓰는 쪽은 0 으로 둘 것)")]
    [Tooltip("고정 수치로 더한다. 예: 최대 체력 +20")]
    [SerializeField] private float _flat = 0f;

    [Tooltip("비율로 더한다. 0.1 = +10%")]
    [SerializeField] private float _percent = 0f;

    // 카드를 고를 때마다 LevelUpUI 가 한 번씩 부른다.
    // level 은 쓰지 않는다. 레벨이 몇이든 고를 때마다 같은 양이 한 번 더 쌓이는 방식이다
    public override void Apply(RoguelikePlayer player, int level)
    {
        player.ApplyStatUpgrade(_stat, _flat, _percent);
    }
}
