using System.Collections;
using DungeonMaster.Core;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
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
