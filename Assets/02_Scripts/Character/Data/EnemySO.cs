using UnityEngine;

// 적의 행동 유형. RoguelikeEnemy 가 이 값에 따라 다르게 움직인다.
// 구 GamePlay 씬의 Enemy/Swampy 는 FSM 을 쓰므로 이 값을 보지 않는다.
// ※ 뒤에만 추가할 것. 중간에 끼우면 기존 .asset 의 선택값이 밀린다.
public enum EnemyBehavior
{
    Chase,      // 직진 추격 (기본)
    Charger,    // 거리를 좁히면 잠깐 멈췄다가 돌진
    Ranged,     // 거리를 유지하며 원거리 공격
    Bomber,     // 달라붙으면 잠깐 멎었다가 자폭
}

// [하는 일] 적 한 종류의 능력치 표다. 체력, 속도, 공격력, 행동 유형, 떨구는 보상 같은 숫자를 적어 둔다.
// [붙이는 곳] 게임오브젝트에 붙이지 않는다. Project 창에서 우클릭 > Create > DungeonMaster > EnemySO 로
//            .asset 파일을 만들고, 적 프리팹(RL_Goblin 등)의 RoguelikeEnemy "Enemy SO" 칸에 끌어다 넣는다.
// [연결] RoguelikeEnemy 가 읽기만 한다. 구 GamePlay 씬의 Enemy/Swampy 도 "기본 스탯" 부분을 같이 쓴다.
// [설계] ScriptableObject 는 "여러 프리팹이 함께 보는 설정 파일"이다.
//        고블린이 100마리 나와도 표는 하나라서, 숫자 하나를 고치면 전부 같이 바뀐다.
//        그래서 게임 도중에 코드로 이 값을 바꾸면 안 된다. 모든 적이 한꺼번에 바뀌고,
//        에디터에서는 플레이를 멈춰도 바뀐 값이 파일에 그대로 남는다.
//        시간이 갈수록 강해지는 배율은 여기가 아니라 RoguelikeEnemy 쪽(_hpScale, _damageScale)에 따로 둔다.
// [설계] 단위: 거리는 유니티 단위(바닥 타일 한 칸 = 1), 시간은 초, moveSpeed 는 1초에 가는 칸 수다.
//        기본 스탯 중 chaseDistance / attackDistance 는 FSM 적 전용이라 뱀서라이크에서는 쓰지 않는다.
[CreateAssetMenu(fileName = "EnemySO", menuName = "DungeonMaster/EnemySO", order = 0)]
public class EnemySO : ScriptableObject
{
    [Header("기본 스탯")]
    public float maxHp = 100f;
    public float moveSpeed = 1f;
    public float chaseDistance = 5f;
    public float attackDistance = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    [Header("행동 유형 (뱀서라이크 전용)")]
    public EnemyBehavior behavior = EnemyBehavior.Chase;

    [Header("보상 (뱀서라이크 전용)")]
    [Tooltip("죽을 때 떨구는 경험치 코인 개수. 강한 적일수록 크게")]
    [Min(1)] public int coinDrop = 1;

    [Tooltip("죽을 때 회복 아이템을 떨굴 확률(0~1). 강한 적일수록 높게")]
    [Range(0f, 1f)] public float healthDropChance = 0f;

    [Header("피격 반응 (뱀서라이크 전용)")]
    [Tooltip("맞았을 때 밀려나는 것에 대한 저항. 0이면 그대로 밀리고, 1이면 꿈쩍도 안 한다")]
    [Range(0f, 1f)] public float knockbackResist = 0f;

    // 아래 "돌진형 / 자폭형 / 원거리형" 설정은 behavior 가 그 유형일 때만 읽힌다. 다른 유형이면 무시된다.
    //
    // 돌진형 한 바퀴: 걸어서 다가감 -> chargeTriggerDistance 안에 들어옴 -> chargeWindup 초 동안 멈춤
    //   -> chargeDuration 초 동안 (moveSpeed x chargeSpeedMul) 속도로 직선 돌진 -> chargeCooldown 초 동안 다시 걷기만
    [Header("돌진형 설정")]
    [Tooltip("이 거리 안에 들어오면 돌진을 준비한다")]
    public float chargeTriggerDistance = 4.5f;
    [Tooltip("돌진 직전에 멈춰서 기를 모으는 시간. 플레이어가 피할 여유")]
    public float chargeWindup = 0.5f;
    [Tooltip("돌진 중 이동속도 배율")]
    public float chargeSpeedMul = 4f;
    [Tooltip("돌진이 지속되는 시간")]
    public float chargeDuration = 0.45f;
    [Tooltip("돌진 후 다음 돌진까지 대기")]
    public float chargeCooldown = 2.5f;

    [Header("자폭형 설정")]
    [Tooltip("이 거리 안에 들어오면 자폭 준비를 시작한다")]
    public float bombTriggerDistance = 1.6f;
    [Tooltip("자폭 직전에 멎어서 부풀어오르는 시간. 플레이어가 도망칠 여유")]
    public float bombFuse = 0.7f;
    [Tooltip("폭발 반경. 이 안에 있으면 맞는다")]
    public float bombRadius = 2f;
    [Tooltip("폭발 피해. 접촉 피해와 별개다")]
    public float bombDamage = 35f;

    [Header("원거리형 설정")]
    [Tooltip("이 거리를 유지하려 한다. 가까우면 물러난다")]
    public float preferredDistance = 6f;
    [Tooltip("발사 주기(초)")]
    public float shootInterval = 2f;
    // 쏘는 투사체 프리팹(루트에 Projectile 컴포넌트가 있어야 한다)과 날아가는 속도(1초에 가는 칸 수).
    // 프리팹이 비어 있으면 원거리형이라도 쏘지 않고 거리만 유지한다
    public GameObject projectilePrefab;
    public float projectileSpeed = 5f;
}
