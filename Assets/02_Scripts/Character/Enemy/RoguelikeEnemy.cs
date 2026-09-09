using System.Collections;
using DungeonMaster.Core;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using DungeonMaster.Weapon;
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
    public class RoguelikeEnemy : MonoBehaviour, IDamagable, IPoolable
    {
        [Header("기본 스탯")]
        // maxHp / moveSpeed / attackDamage / attackCooldown 만 사용
        // chaseDistance, attackDistance는 FSM 전용이라 이 클래스에선 쓰지 않음
        [SerializeField] private EnemySO _enemySO;
        [SerializeField] GameObject _dropCoin;

        [Header("타격 연출")]
        [Tooltip("피격 시 번쩍이는 시간")]
        [SerializeField] private float _flashDuration = 0.08f;
        // SpriteRenderer.color 는 곱연산이라 흰색(1,1,1)은 항등원이다.
        // 스프라이트 기본 색이 흰색이므로 "흰색 플래시"는 화면상 아무 변화가 없다.
        // 진짜 흰색 발광을 하려면 전용 셰이더가 필요하므로, 여기서는 붉게 물들이는 방식을 쓴다.
        [SerializeField] private Color _flashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [Tooltip("피격 시 데미지 숫자를 띄운다. 씬에 MMFloatingTextSpawner 가 있어야 보인다")]
        [SerializeField] private bool _showDamageNumber = true;

        // 컴포넌트 캐싱
        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        // 추적 대상. 스폰될 때 한 번만 찾고 이후로는 다시 탐색하지 않음
        private Transform _target;

        private float _currHp;
        private bool _isDead;

        // 경과 시간에 따른 난이도 배율. 스포너가 생성 직후에 내려준다
        private float _hpScale = 1f;
        private float _damageScale = 1f;

        // 풀에서 나온 개체인지. 씬에 직접 배치된 적은 풀이 모르므로 Release 하면 안 된다.
        // (ObjectPool.Release 는 모르는 오브젝트를 경고만 내고 무시해서, 죽어도 안 사라지는
        //  좀비 오브젝트가 된다)
        private bool _fromPool;

        // 행동 유형별 상태
        private float _nextChargeTime;      // 다음 돌진 가능 시각
        private float _chargeStateEnd;      // 현재 단계(준비/돌진)가 끝나는 시각
        private bool _isWindingUp;          // 돌진 준비 중(멈춰서 기 모으는 중)
        private bool _isCharging;           // 돌진 중
        private Vector2 _chargeDir;
        private float _nextShootTime;

        public float MaxHp { get { return _enemySO.maxHp * _hpScale; } }
        public float ContactDamage { get { return _enemySO.attackDamage * _damageScale; } }

        // 겹쳐 있는 동안 데미지가 매 프레임 들어가지 않도록 하는 쿨타임
        private float _lastContactTime;

        // 타격 연출용
        private MMHealthBar _healthBar;     // 없으면 체력바를 안 그릴 뿐, 동작에는 지장 없음
        private Color _baseColor;
        private Coroutine _flashRoutine;

        // 애니메이션 해시(RLSwampyAnim: IsWalk(bool), Hit(trigger))
        private static readonly int hashIsWalk = Animator.StringToHash("IsWalk");
        private static readonly int hashHit = Animator.StringToHash("Hit");

        #region 유니티 생명주기
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();

            _healthBar = GetComponent<MMHealthBar>();
            _baseColor = _spriteRenderer.color;

            _currHp = MaxHp;
        }

        private void Start()
        {
            AcquireTarget();
        }

        // Start 는 최초 1회뿐이라 풀에서 재사용될 때는 안 불린다.
        // 씬을 다시 시작하면 파괴된 옛 플레이어를 물고 있게 되므로 매번 다시 잡는다.
        private void AcquireTarget()
        {
            GameObject player = GameObject.FindWithTag("PLAYER");
            if (player == null)
            {
                Debug.LogError($"RoguelikeEnemy::AcquireTarget() PLAYER 태그를 가진 오브젝트가 없습니다.");
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

            switch (_enemySO.behavior)
            {
                case EnemyBehavior.Charger: TickCharger(); break;
                case EnemyBehavior.Ranged:  TickRanged();  break;
                default:                    TickChase();   break;
            }
        }

        #region 행동 유형
        private Vector2 DirectionToTarget
        {
            get { return ((Vector2)_target.position - _rb.position).normalized; }
        }

        private void FaceTarget(Vector2 dir)
        {
            if (Mathf.Abs(dir.x) > 0.01f) _spriteRenderer.flipX = dir.x < 0f;
        }

        // 기본: 플레이어를 향해 계속 직진
        private void TickChase()
        {
            Vector2 dir = DirectionToTarget;
            FaceTarget(dir);
            _rb.linearVelocity = dir * _enemySO.moveSpeed;
        }

        // 돌진형: 가까워지면 잠깐 멈췄다가(피할 여유) 빠르게 돌진
        private void TickCharger()
        {
            // 1) 돌진 중
            if (_isCharging)
            {
                if (Time.time >= _chargeStateEnd)
                {
                    _isCharging = false;
                    _nextChargeTime = Time.time + _enemySO.chargeCooldown;
                }
                else
                {
                    _rb.linearVelocity = _chargeDir * (_enemySO.moveSpeed * _enemySO.chargeSpeedMul);
                    return;
                }
            }

            // 2) 돌진 준비 중 - 멈춰 서서 방향만 본다
            if (_isWindingUp)
            {
                _rb.linearVelocity = Vector2.zero;
                FaceTarget(DirectionToTarget);

                if (Time.time < _chargeStateEnd) return;

                _isWindingUp = false;
                _isCharging = true;
                _chargeDir = DirectionToTarget;      // 준비가 끝난 시점의 방향으로 고정
                _chargeStateEnd = Time.time + _enemySO.chargeDuration;
                return;
            }

            // 3) 평소에는 천천히 접근하다가 사거리에 들면 준비 시작
            Vector2 dir = DirectionToTarget;
            FaceTarget(dir);
            _rb.linearVelocity = dir * _enemySO.moveSpeed;

            if (Time.time < _nextChargeTime) return;

            float sqr = ((Vector2)_target.position - _rb.position).sqrMagnitude;
            if (sqr > _enemySO.chargeTriggerDistance * _enemySO.chargeTriggerDistance) return;

            _isWindingUp = true;
            _chargeStateEnd = Time.time + _enemySO.chargeWindup;
        }

        // 원거리형: 선호 거리를 유지하며 주기적으로 발사
        private void TickRanged()
        {
            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            float dist = toTarget.magnitude;
            Vector2 dir = dist > 0.001f ? toTarget / dist : Vector2.right;
            FaceTarget(dir);

            float preferred = _enemySO.preferredDistance;

            // 너무 가까우면 물러나고, 너무 멀면 다가가고, 적당하면 멈춘다
            if (dist < preferred * 0.8f)      _rb.linearVelocity = -dir * _enemySO.moveSpeed;
            else if (dist > preferred * 1.2f) _rb.linearVelocity = dir * _enemySO.moveSpeed;
            else                              _rb.linearVelocity = Vector2.zero;

            if (Time.time < _nextShootTime) return;
            _nextShootTime = Time.time + _enemySO.shootInterval;

            Shoot(dir);
        }

        private void Shoot(Vector2 dir)
        {
            if (_enemySO.projectilePrefab == null) return;

            GameObject go = ObjectPool.Instance != null
                ? ObjectPool.Instance.Spawn(_enemySO.projectilePrefab, transform.position)
                : Instantiate(_enemySO.projectilePrefab, transform.position, Quaternion.identity);
            if (go == null) return;

            Projectile p = go.GetComponent<Projectile>();
            if (p != null) p.Launch(dir, ContactDamage, _enemySO.projectileSpeed, 0, -90f);
        }
        #endregion
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
            other.GetComponent<IDamagable>()?.TakeDamage(ContactDamage);
        }
        #endregion

        #region 인터페이스 구현
        public void TakeDamage(float damage)
        {
            // 공전 무기 여러 개가 같은 프레임에 때리면 Die가 두 번 불릴 수 있음
            if (_isDead) return;

            _currHp -= damage;

            ShowDamageNumber(damage);
            Flash();
            UpdateHealthBar();
            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.enemyHitSFX : null);

            if (_currHp > 0f)
            {
                _animator.SetTrigger(hashHit);
                return;
            }

            Die();
        }
        #endregion

        // 스포너가 Instantiate 직후에 호출한다.
        // 이 시점엔 Awake 가 이미 끝나 _currHp 가 세팅되어 있으므로 다시 계산해 준다.
        public void ApplyDifficultyScale(float hpScale, float damageScale)
        {
            _hpScale = Mathf.Max(0.1f, hpScale);
            _damageScale = Mathf.Max(0.1f, damageScale);

            _currHp = MaxHp;
        }

        #region 타격 연출
        private void ShowDamageNumber(float damage)
        {
            if (!_showDamageNumber) return;

            // 씬에 MMFloatingTextSpawner 가 없으면 아무도 이 이벤트를 듣지 않아서 조용히 무시된다
            MMFloatingTextSpawnEvent.Trigger(
                new MMChannelData(MMChannelModes.Int, 0, null),
                transform.position,
                Mathf.Max(1, Mathf.RoundToInt(damage)).ToString(),
                Vector3.up,
                1f);
        }

        // 연속으로 맞으면 코루틴이 겹쳐서 원래 색으로 못 돌아오므로 항상 이전 것을 멈춘다
        private void Flash()
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashCo());
        }

        private IEnumerator FlashCo()
        {
            _spriteRenderer.color = _flashColor;
            yield return new WaitForSeconds(_flashDuration);
            _spriteRenderer.color = _baseColor;
            _flashRoutine = null;
        }

        private void UpdateHealthBar()
        {
            if (_healthBar == null) return;
            _healthBar.UpdateBar(Mathf.Max(0f, _currHp), 0f, MaxHp, true);
        }
        #endregion

        private void Die()
        {
            _isDead = true;
            _currHp = 0f;

            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.enemyDeathSFX : null);
            DropCoin();

            // 사망 애니메이션이 없으므로 바로 제거
            if (_fromPool && ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
            else Destroy(gameObject);
        }

        #region IPoolable
        // Awake/Start 는 최초 1회뿐이므로 재사용 시 초기화는 전부 여기서 한다.
        // 하나라도 빠뜨리면 "죽은 채로 스폰되는 적", "체력이 깎인 채 나오는 적",
        // "스폰하자마자 피격 애니메이션을 재생하는 적" 같은 버그가 된다.
        public void OnSpawnFromPool()
        {
            _fromPool = true;
            _isDead = false;
            _currHp = MaxHp;
            _lastContactTime = 0f;

            // 행동 상태도 반드시 되돌린다.
            // 안 그러면 돌진 도중에 죽은 적이 다음 스폰 때 돌진 상태로 튀어나온다
            _isWindingUp = false;
            _isCharging = false;
            _nextChargeTime = 0f;
            _chargeStateEnd = 0f;
            _nextShootTime = 0f;

            if (_rb != null) _rb.linearVelocity = Vector2.zero;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;   // 붉게 물든 채로 반납됐을 수 있다
                _spriteRenderer.enabled = true;
            }

            if (_animator != null)
            {
                // 래치된 트리거가 남아 있으면 스폰 즉시 피격 모션이 재생된다
                _animator.ResetTrigger(hashHit);
                _animator.SetBool(hashIsWalk, true);
            }

            AcquireTarget();
        }

        public void OnReturnToPool()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            if (_spriteRenderer != null) _spriteRenderer.color = _baseColor;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }
        #endregion

        private void DropCoin()
        {
            if (_dropCoin == null) return;

            // 부모를 지정하지 않는 것이 중요함
            // Instantiate(_dropCoin, transform)처럼 자신을 부모로 주면
            // 바로 아래 Destroy(gameObject)에서 코인까지 같이 사라짐
            if (ObjectPool.Instance != null) ObjectPool.Instance.Spawn(_dropCoin, transform.position);
            else Instantiate(_dropCoin, transform.position, Quaternion.identity);
        }
    }
}
