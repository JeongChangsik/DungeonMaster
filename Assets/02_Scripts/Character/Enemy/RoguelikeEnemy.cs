using DungeonMaster.Core;
using Unity.VisualScripting;
using UnityEngine;

namespace DungeonMaster.Character.Enemy
{
    // 뱀서라이크용 잡몹
    // 스폰 직후부터 플레이어를 향해 계속 직진하고, 겹쳐 있는 동안 주기적으로 데미지를 준다.
    //
    // 기존 Enemy/Swampy를 상속하지 않는 이유:
    //  - Enemy는 FSM(Idle/Chase/Attack) 전제로 만들어져 있어서
    //    탐색 반경 / 공격 사거리 / 넉백 스턴이 전부 "추적을 끊는" 방향으로 동작함
    //  - 여기서 필요한 건 상태 1개뿐이라 FSM, 탐색(OverlapCircle+LINQ), 공격이 전부 불필요
    //  - GamePlay 씬이 쓰는 Enemy.cs / Swampy.cs를 한 줄도 건드리지 않기 위함
    // 스탯 데이터(EnemySO)만 재사용한다.
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class RoguelikeEnemy : MonoBehaviour, IDamagable
    {
        [Header("기본 스탯")]
        // maxHp / moveSpeed / attackDamage / attackCooldown 만 사용
        // chaseDistance, attackDistance는 FSM 전용이라 이 클래스에선 쓰지 않음
        [SerializeField] private EnemySO _enemySO;
        [SerializeField] GameObject _dropCoin;

        // 컴포넌트 캐싱
        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        // 추적 대상. 스폰될 때 한 번만 찾고 이후로는 다시 탐색하지 않음
        private Transform _target;

        private float _currHp;
        private bool _isDead;

        // 겹쳐 있는 동안 데미지가 매 프레임 들어가지 않도록 하는 쿨타임
        private float _lastContactTime;

        // 애니메이션 해시(RLSwampyAnim: IsWalk(bool), Hit(trigger))
        private static readonly int hashIsWalk = Animator.StringToHash("IsWalk");
        private static readonly int hashHit = Animator.StringToHash("Hit");

        #region 유니티 생명주기
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();

            _currHp = _enemySO.maxHp;
        }

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("PLAYER");
            if (player == null)
            {
                Debug.LogError($"RoguelikeEnemy::Start() PLAYER 태그를 가진 오브젝트가 없습니다.");
                return;
            }
            _target = player.transform;

            // 멈추는 상태가 없으므로 계속 걷는 애니메이션
            _animator.SetBool(hashIsWalk, true);
        }

        // linearVelocity를 다루므로 Update가 아니라 FixedUpdate
        private void FixedUpdate()
        {
            // 플레이어가 죽어서 파괴되면 target이 null이 됨
            if (_isDead || _target == null) return;

            Vector2 direction = ((Vector2)_target.position - _rb.position).normalized;
            _spriteRenderer.flipX = direction.x < 0f;
            _rb.linearVelocity = direction * _enemySO.moveSpeed;
        }
        #endregion

        #region 접촉 데미지
        // Enter만 쓰면 한 번 겹친 뒤 계속 붙어 있어도 더 이상 데미지가 들어가지 않음
        // 몸으로 미는 것이 유일한 공격 수단이므로 Stay + 쿨타임으로 처리
        private void OnTriggerStay2D(Collider2D other)
        {
            if (_isDead) return;
            if (!other.CompareTag("PLAYER")) return;
            if (Time.time < _lastContactTime + _enemySO.attackCooldown) return;

            _lastContactTime = Time.time;
            other.GetComponent<IDamagable>()?.TakeDamage(_enemySO.attackDamage);
        }
        #endregion

        #region 인터페이스 구현
        public void TakeDamage(float damage)
        {
            // 공전 무기 여러 개가 같은 프레임에 때리면 Die가 두 번 불릴 수 있음
            if (_isDead) return;

            _currHp -= damage;
            if (_currHp > 0f)
            {
                _animator.SetTrigger(hashHit);
                return;
            }

            Die();
        }
        #endregion

        private void Die()
        {
            _isDead = true;
            _currHp = 0f;

            DropCoin();

            // 사망 애니메이션이 없으므로 바로 제거
            Destroy(gameObject);
        }

        private void DropCoin()
        {
            if (_dropCoin == null) return;

            // 부모를 지정하지 않는 것이 중요함
            // Instantiate(_dropCoin, transform)처럼 자신을 부모로 주면
            // 바로 아래 Destroy(gameObject)에서 코인까지 같이 사라짐
            Instantiate(_dropCoin, transform.position, Quaternion.identity);
        }
    }
}
