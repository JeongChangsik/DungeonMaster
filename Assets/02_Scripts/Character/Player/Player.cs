using UnityEngine;
using DungeonMaster.InputSystem;
using DungeonMaster.Core;
using Unity.Cinemachine;
using UnityEngine.UI;
// ReSharper disable All

namespace DungeonMaster.Character.Player
{
    // [하는 일] 모든 플레이어 캐릭터의 공통 부모. 체력, 이동, 피격, 사망, 스탯 강화처럼 직업과 상관없는 부분을 담는다.
    // [붙이는 곳] 직접 붙이지 않는다. abstract(추상) 클래스라 그 자체로는 만들 수 없고, 자식 클래스를 붙인다.
    // [연결] 자식은 둘로 나뉜다.
    //          - Warrior              : 구 GamePlay 씬의 전사 (Player 를 바로 상속)
    //          - RoguelikePlayer      : 뱀서라이크 씬의 공통 부모 (경험치, 무적, 사망 연출) -> RoguelikeWarrior
    //        InputHandler 의 이벤트를 듣고, IDamagable 약속을 지켜서 적에게 맞을 수 있다.
    //        레벨업 카드(PlayerStatUpgradeSO)는 RoguelikePlayer.ApplyStatUpgrade 를 거쳐 아래의 Add... 함수들을 부른다.
    // [설계] 스탯은 "기본값 + 보너스" 로 나눴다. 인스펙터에 적은 기본값은 그대로 두고, 강화분만 따로 쌓는다.
    //        보너스는 0, 배율은 1 에서 시작하므로 강화를 한 번도 안 받는 구 GamePlay 씬의 Warrior 는
    //        예전과 숫자가 완전히 똑같이 동작한다.
    //
    // abstract / virtual / override 간단 정리
    //   abstract : "자식이 반드시 채워야 하는 빈칸". 부모에는 내용이 없다 (Attack)
    //   virtual  : "기본 내용은 있지만 자식이 바꿔도 되는 칸" (Awake, TakeDamage, Die 등)
    //   override : 자식이 위의 칸을 자기 내용으로 바꿀 때 붙인다. base.함수() 로 부모 내용도 이어서 부를 수 있다
    //
    // [RequireComponent] 는 "이 스크립트를 붙이면 아래 컴포넌트도 반드시 같이 있어야 한다"는 표시다.
    // 없으면 유니티가 자동으로 붙여주고, 실수로 지우는 것도 막아준다.
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(InputHandler))]
    public abstract class Player : MonoBehaviour, IDamagable
    {
        #region 기본 스탯
        [Header("기본 스탯")]
        [SerializeField] protected float _maxHp = 100f;
        [SerializeField] protected float _currHp = 100f;
        [SerializeField] protected float _moveSpeed = 5f;
        [SerializeField] protected float _attackDamage = 20f;
        // _attackCooldown 은 초 단위. 공격과 공격 사이의 최소 간격이다
        [SerializeField] protected float _attackCooldown = 0.5f;

        // 따로 저장하는 값이 아니라, 물어볼 때마다 "체력이 0 이하인가"를 계산해서 돌려준다
        protected bool _isDead => _currHp <= 0f;
        #endregion

        #region 스탯 보정 (런타임 강화용)
        // 위의 SerializeField 값은 "기본값"으로 그대로 두고, 강화분만 여기에 누적한다.
        // 이렇게 해야 인스펙터/프리팹에 저장된 값이 살아있고, 보정치가 0/1인 동안은
        // 기존 씬(GamePlay)의 동작이 수치적으로 완전히 동일하다.
        // _bonus... : 더하는 값. 0 이면 강화 없음
        // _mul...   : 곱하는 배율. 1 이면 강화 없음 (1.2 = 20% 증가)
        protected float _bonusMaxHp;
        protected float _bonusMoveSpeed;
        protected float _bonusAttackDamage;

        protected float _mulMaxHp = 1f;
        protected float _mulMoveSpeed = 1f;
        protected float _mulAttackDamage = 1f;
        protected float _mulCooldown = 1f;
        #endregion

        #region 프로퍼티
        public float MaxHp => (_maxHp + _bonusMaxHp) * _mulMaxHp;
        public float CurrHp => _currHp;
        public float MoveSpeed => (_moveSpeed + _bonusMoveSpeed) * _mulMoveSpeed;
        public float AttackDamage => (_attackDamage + _bonusAttackDamage) * _mulAttackDamage;
        // 하한을 두지 않으면 쿨감 카드를 여러 장 먹었을 때 0으로 나누거나 무한 연사가 된다
        public float AttackCooldown => Mathf.Max(0.05f, _attackCooldown * _mulCooldown);
        #endregion

        #region 컴포넌트 캐싱
        protected Rigidbody2D _rb;
        protected Animator _animator;
        protected SpriteRenderer _spriteRenderer;
        protected InputHandler _inputHandler;
        // HP bar 연결 방법 두 가지(Unity에서 연결, 코드로 연결)
        [SerializeField] protected Image _hpBar;
        protected Image _hpBar2;

        // Facing 처리를 위한 Weapon Arm
        protected Transform _weaponArm;

        // 애니메이터 파라미터 해시(Hash)값 미리 추출
        protected static readonly int hashIsWalk = Animator.StringToHash("IsWalk");
        protected static readonly int hashAttack = Animator.StringToHash("Attack");
        protected static readonly int hashHit = Animator.StringToHash("Hit");
        
        #endregion

        // 마지막 공격 시간 기록
        protected float lastAttackTime = 0f;

        #region 유니티 생명주기
        // 오브젝트가 생길 때 한 번 불린다. 체력을 채우고 필요한 컴포넌트를 찾아 둔다.
        // 참고: RoguelikePlayer 는 이 Awake 를 base 로 부르지 않고 따로 다시 쓴다.
        //       로그라이크 씬에는 아래에서 찾는 "Canvas" 의 3번째 Image 나 "Arm" 오브젝트가 없기 때문이다.
        protected virtual void Awake()
        {
            Debug.Log($"Player::Awake()");
            // 초기 체력 설정
            _currHp = MaxHp;

            // 컴포넌트 캐싱 (this.gameObject.GetComponent<T>())
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _inputHandler = GetComponent<InputHandler>();

            // GetComponentsInChildren 함수는 깊이 우선 탐색(DFS)으로 처음 하위 오브젝트에서 더 하위 오브젝트로 내려가면서 배열에 할당함.
            /* Player 오브젝트와 하위 오브젝트에 모두 Image 컴포넌트가 있다고 하면, Player, Arm, Pivot, Sword, Head 순으로 배열에 할당됨
             * Player
                ├─ Arm
                │   └─ Pivot
                │       └─ Sword 
                └─ Head
            */
            // 하지만 여기서는 GameObject.Find로 "Canvas" 오브젝트를 찾았으니 "Canvas" 오브젝트부터 하위로 Image 컴포넌트 탐색함
            // _hpBar2 = GameObject.Find("Canvas").GetComponentsInChildren<Image>()[2];

            // Weapon Arm 설정
            _weaponArm = transform.Find("Arm");
            // Find() 함수는 Update(), FixedUpdate() 함수에서는 절대 사용하지 말 것 => 성능 저하
            // this.gameObject.Find => Root(Hierarchy)에서부터 찾음
            // this.gameObject.transform.Find => 해당 Transform의 위치에서부터 찾음
            // Transform는 GetComponent처럼 가져오는 방식이 아닌 직접 접근할 수 있는 shorthand를 유니티에서 지원함
        }

        // 켜질 때 입력 이벤트를 구독하고, 꺼질 때 해제한다.
        // 해제하지 않으면 오브젝트가 사라진 뒤에도 InputHandler 가 이 함수를 불러 에러가 날 수 있다.
        // virtual 이라 RoguelikePlayer 는 "공격 입력은 빼고 구독하는" 버전으로 바꿔 쓴다
        protected virtual void OnEnable()
        {
            _inputHandler.OnMoveAction += OnMove;
            _inputHandler.OnAttackAction += OnAttack;
            _inputHandler.OnInteractAction += OnInteract;
        }

        protected virtual void OnDisable()
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
        // 스프라이트를 좌우로 뒤집고 무기 팔(Arm)도 같이 돌린다.
        // RoguelikePlayer 에는 Arm 이 없어서 스프라이트만 뒤집는 버전으로 override 한다
        protected virtual void FlipDirection(bool facingRight)
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
        // 마지막 이동 입력. 이동속도가 바뀌면 이 값으로 속도를 다시 계산한다
        protected Vector2 _moveInput;

        // 이동속도 강화 직후에도 호출된다(AddMoveSpeed). 키를 누르고 있는 중에도 즉시 반영시키기 위함
        protected void ApplyMoveVelocity()
        {
            if (_rb == null) return;
            _rb.linearVelocity = _moveInput * MoveSpeed;
        }

        protected void OnMove(Vector2 ctx)
        {
            if(_isDead) return;
            Debug.Log($"이동: {ctx}, 벡터 크기: {ctx.normalized}");

            // 마지막 입력을 기억해 둔다.
            // InputHandler가 performed/canceled에만 바인딩되어 있어서 OnMove는 입력이 "바뀔 때"만 불린다.
            // 기억해 두지 않으면 이동속도를 강화해도 키를 뗐다 다시 누르기 전까지 반영되지 않는다.
            _moveInput = ctx;

            // 이동 처리
            ApplyMoveVelocity();

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

        protected void OnAttack()
        {
            if(_isDead) return;

            // 공격 쿨다운 체크
            // Time.time =  게임이 시작된 시점(또는 씬이 로드된 시점)부터 지금까지 흐른 시간을 초 단위로 알려주는 정적(static) float 값
            if(Time.time >= lastAttackTime + AttackCooldown)
            {
                lastAttackTime = Time.time;
                // _animator.SetTrigger(hashAttack);
                Attack();
            }
        }

        protected void OnInteract(bool ctx)
        {
            if(_isDead) return;
            Debug.Log($"상호작용: {ctx}");
        }
        #endregion
        
        #region 추상 메서드
        // 직업마다 공격 방식이 달라서 부모는 내용을 비워 두고 자식에게 맡긴다.
        // abstract 라서 자식 클래스는 이 함수를 반드시 override 해야 컴파일된다
        protected abstract void Attack();
        #endregion

        #region 가상 메서드
        // IDamagable 약속을 지키는 함수. 적이나 PlayerEditor 의 테스트 버튼이 부른다.
        // 체력을 깎고, HP바를 고치고, 피격 애니메이션을 틀고, 0 이하면 Die() 를 부른다.
        // 로그라이크 전사가 맞으면 이렇게 차례로 내려온다:
        //   RoguelikeWarrior.TakeDamage (방어력 빼기)
        //   -> RoguelikePlayer.TakeDamage (무적 검사, 소리, 흔들림, 히트스톱)
        //   -> 여기 Player.TakeDamage (실제로 체력 깎기)
        public virtual void TakeDamage(float damage)
        {
            if(_isDead) return;
            Debug.Log($"피격 당함! (dmg: {damage})");

            _currHp -= damage;

            // HP바 갱신
            RefreshHpBar();

            _animator.SetTrigger(hashHit);

            if(_currHp <= 0f)
            {
                Die();
            }
        }

        // 최대 체력이 바뀔 때도 불러야 해서 별도 메서드로 뺐다
        protected void RefreshHpBar()
        {
            if (_hpBar == null) return;
            _hpBar.fillAmount = MaxHp > 0f ? _currHp / MaxHp : 0f;
        }

        #region 스탯 강화 API (레벨업 카드가 호출)
        // percent 는 비율. 0.1f = +10%
        public void AddMaxHp(float flat, float percent = 0f, bool healDelta = true)
        {
            float before = MaxHp;

            _bonusMaxHp += flat;
            _mulMaxHp += percent;

            // 최대 체력이 늘어난 만큼은 회복시켜 준다. 안 그러면 체감이 없다
            if (healDelta) _currHp += Mathf.Max(0f, MaxHp - before);
            _currHp = Mathf.Min(_currHp, MaxHp);

            RefreshHpBar();
        }

        public void AddMoveSpeed(float flat, float percent = 0f)
        {
            _bonusMoveSpeed += flat;
            _mulMoveSpeed += percent;

            // 키를 누르고 있는 중에도 즉시 반영
            ApplyMoveVelocity();
        }

        // 기본 공격력을 올린다. 지금은 PlayerStat 에 공격력 항목이 없어서 이 함수를 부르는 카드가 없다.
        // 뱀서라이크의 무기 피해는 이것 대신 RoguelikePlayer 의 WeaponDamageMul 로 올린다
        public void AddAttackDamage(float flat, float percent = 0f)
        {
            _bonusAttackDamage += flat;
            _mulAttackDamage += percent;
        }

        // percent 만큼 쿨다운 감소. 0.1f = 10% 감소
        public void AddCooldownReduction(float percent)
        {
            _mulCooldown = Mathf.Max(0.2f, _mulCooldown - percent);   // 최대 80% 감소까지만
        }

        // 체력을 회복한다. 최대 체력을 넘지 않는다. RoguelikePlayer 의 초당 회복(TickRegen)이 부른다
        public void Heal(float amount)
        {
            if (_isDead) return;

            _currHp = Mathf.Min(_currHp + amount, MaxHp);
            RefreshHpBar();
        }
        #endregion

        // 체력이 0 이하가 되면 TakeDamage 가 부른다. 부모는 체력을 0 으로 맞추고 로그만 남긴다.
        // RoguelikePlayer 가 override 해서 시간 정지와 쓰러지는 연출을 덧붙인다
        protected virtual void Die()
        {
            _currHp = 0f;
            Debug.Log($"주인공이 사망했습니다.");
        }
        #endregion
    }
}
