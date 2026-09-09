using UnityEngine;

// 적의 행동 유형. RoguelikeEnemy 가 이 값에 따라 다르게 움직인다.
// 구 GamePlay 씬의 Enemy/Swampy 는 FSM 을 쓰므로 이 값을 보지 않는다.
// ※ 뒤에만 추가할 것. 중간에 끼우면 기존 .asset 의 선택값이 밀린다.
public enum EnemyBehavior
{
    Chase,      // 직진 추격 (기본)
    Charger,    // 거리를 좁히면 잠깐 멈췄다가 돌진
    Ranged,     // 거리를 유지하며 원거리 공격
}

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

    [Header("피격 반응 (뱀서라이크 전용)")]
    [Tooltip("맞았을 때 밀려나는 것에 대한 저항. 0이면 그대로 밀리고, 1이면 꿈쩍도 안 한다")]
    [Range(0f, 1f)] public float knockbackResist = 0f;

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

    [Header("원거리형 설정")]
    [Tooltip("이 거리를 유지하려 한다. 가까우면 물러난다")]
    public float preferredDistance = 6f;
    [Tooltip("발사 주기(초)")]
    public float shootInterval = 2f;
    public GameObject projectilePrefab;
    public float projectileSpeed = 5f;
}
