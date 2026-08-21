using UnityEngine;
using DungeonMaster.InputSystem;
using DungeonMaster.Core;

namespace DungeonMaster.Character.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(InputHandler))]
    public abstract class Player : MonoBehaviour, IDamagble
    {
        #region 기본 스탯
        [Header("기본 스탯")]
        [SerializeField] protected float _maxHp = 100f;
        [SerializeField] protected float _currHp = 100f;
        [SerializeField] protected float _moveSpeed = 5f;
        [SerializeField] protected float _attackDamage = 20f;
        [SerializeField] protected float _attackCooldown = 0.5f;

        protected bool _isDead => _currHp <= 0f;
        #endregion

        #region 프로퍼티
        public float MaxHp => _maxHp;
        public float CurrHp => _currHp;
        public float MoveSpeed => _moveSpeed;
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        #endregion

        #region 컴포넌트 캐싱
        protected Rigidbody2D _rb;
        protected Animator _animator;
        protected SpriteRenderer _spriteRenderer;
        protected InputHandler _inputHandler;

        // Facing 처리를 위한 Weapon Arm
        protected Transform _weaponArm;

        // 애니메이터 파라미터 해시(Hash)값 미리 추출
        protected static readonly int hashIsWalk = Animator.StringToHash("IsWalk");
        protected static readonly int hashAttack = Animator.StringToHash("Attack");
        protected static readonly int hashHit = Animator.StringToHash("Hit");
        
        #endregion

        // 마지막 공격 시간 기록
        private float lastAttackTime = 0f;

        #region 유니티 생명주기
        protected virtual void Awake()
        {
            Debug.Log($"Player::Awake()");
            // 초기 체력 설정
            _currHp = _maxHp;

            // 컴포넌트 캐싱 (this.gameObject.GetComponent<T>())
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _inputHandler = GetComponent<InputHandler>();

            // Weapon Arm 설정
            _weaponArm = transform.Find("Arm");
            // Find() 함수는 Update(), FixedUpdate() 함수에서는 절대 사용하지 말 것 => 성능 저하
            // this.gameObject.Find => Root(Hierarchy)에서부터 찾음
            // this.gameObject.transform.Find => 해당 Transform의 위치에서부터 찾음
            // Transform는 GetComponent처럼 가져오는 방식이 아닌 직접 접근할 수 있는 shorthand를 유니티에서 지원함
        }

        protected void OnEnable()
        {
            _inputHandler.OnMoveAction += OnMove;
            _inputHandler.OnAttackAction += OnAttack;
            _inputHandler.OnInteractAction += OnInteract;
        }

        protected void OnDisable()
        {
            _inputHandler.OnMoveAction -= OnMove;
            _inputHandler.OnAttackAction -= OnAttack;
            _inputHandler.OnInteractAction -= OnInteract;
        }
        #endregion

        /* 벡터의 정규화(Normalize)
         * a + b = c
         * c.normalized
         */

        #region 공통 메서드
        // Facing 처리
        private void FlipDirection(bool facingRight)
        {
            if (facingRight)
            {
                // 오른쪽
                _spriteRenderer.flipX = false;
                _weaponArm.localRotation = Quaternion.Euler(0f,0f,0f);
            }
            else
            {
                // 왼쪽
                _spriteRenderer.flipX = true;
                _weaponArm.localRotation = Quaternion.Euler(0f,180f,0f);
            }
        }

        #endregion

        #region 입력 처리 메서드
        private void OnMove(Vector2 ctx)
        {
            if(_isDead) return;
            Debug.Log($"이동: {ctx}, 벡터 크기: {ctx.normalized}");

            // 이동 처리
            _rb.linearVelocity = ctx * _moveSpeed;

            // 방향 전환
            if(ctx.x != 0)
            {
                FlipDirection(ctx.x > 0);
            }

            // 애니메이션 처리
            // _animator.SetBool("IsWalk", ctx.sqrMagnitude > 0f);
            // 이렇게 "IsWalk"를 사용하지말고, hash값을 가져와 전달해야 함.
            _animator.SetBool(hashIsWalk, ctx.sqrMagnitude > 0f);

        }

        private void OnAttack()
        {
            if(_isDead) return;

            // 공격 쿨다운 체크
            // Time.time =  시간
            if(Time.time >= lastAttackTime + _attackCooldown)
            {
                lastAttackTime = Time.time;
                _animator.SetTrigger(hashAttack);
                Attack();
            }
        }

        private void OnInteract(bool ctx)
        {
            if(_isDead) return;
            Debug.Log($"상호작용: {ctx}");
        }
        #endregion
        
        #region 추상 메서드
        protected abstract void Attack();
        #endregion

        #region 가상 메서드
        public virtual void TakeDamage(float damage)
        {
            if(_isDead) return;
            Debug.Log($"피격 당함! (dmg: {damage})");

            _currHp -= damage;
            _animator.SetTrigger(hashHit);

            if(_currHp <= 0f)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            _currHp = 0f;
            Debug.Log($"주인공이 사망했습니다.");
        }
        #endregion
    }
}
