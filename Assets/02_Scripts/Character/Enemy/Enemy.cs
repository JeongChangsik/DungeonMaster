using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DungeonMaster.Character.Enemy.FSM;
using DungeonMaster.Character.Player;
using DungeonMaster.Core;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace DungeonMaster.Character.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class Enemy : MonoBehaviour
    {
        [Header("기본 스탯")]
        [SerializeField] protected EnemySO _enemySO;

        public EnemySO EnemySO => _enemySO;
        
        [Header("주인공 레이어 마스크")]
        [SerializeField] protected LayerMask _playerMask;
        
        [Header("주인공 검풀 빈도")]
        [SerializeField] protected float _detectInterval = 0.3f;
        private float _lastDetectTime = 0f;
        
        //  상태 머신 변수 선언
        protected StateMachine _stateMachine;
        
        // 상태를 저장할 딕셔너리 선언
        protected Dictionary<Type, IState> _states;
        
        // 딕셔너리 초기화 메서드
        protected abstract void InitState();
        
        // 컴포넌트 캐싱
        protected Rigidbody2D _rb;
        protected SpriteRenderer _spriteRenderer;
        protected Animator _animator;
        
        // 애니메이션 해시 추출
        protected static readonly int hashIsWalk = Animator.StringToHash("IsWalk");
        protected static readonly int hashHit = Animator.StringToHash("Hit");
        
        // 애니메이션 설정 메서드
        public void SetWalk(bool isWalk) => _animator.SetBool(hashIsWalk, isWalk);
        public void TriggerHit() => _animator.SetTrigger(hashHit);

        #region 초기화 메서드
        private void InitComponents()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();            
            _animator = GetComponent<Animator>();
        }
        #endregion

        #region 유니티 생명주기
        protected void Awake()
        {
            Debug.Log($"Enemy::Awake()");

            // // 초기 상태 설정(IdleState)
            // ChangeState(new IdleState());
            // => InitState로 변경

            InitState();
            InitComponents();
        }

        protected virtual void Start()
        {
            // 상태 머신 초기화
            _stateMachine = new StateMachine(this);
            ChangeState<IdleState>();
        }

        private void Update()
        {
            // Debug.Log($"Enemy::Update()");
            // 상태 머신 업데이트
            _stateMachine.Update();
            // TestFSM();
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.aquamarine;
            Gizmos.DrawWireSphere(transform.position, _enemySO.chaseDistance);  // 3d로 그림

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _enemySO.attackDistance);
        }
        #endregion
        
        #region 상태 관련 메서드
        // 상태 전환 메서드
        public void ChangeState<T>() where T : IState
        {
            // 딕셔너리에 저장된 상태(State)를 가져와서 전환
            if (_states.TryGetValue(typeof(T), out IState state))
            {
                _stateMachine?.ChangeState(state);
            }
        }
        #endregion
        
        #region 추적 관련 메서드
        // 반경
        public Transform target;

        public bool DetectPlayer()
        {
            // Physics2D : 2D 게임에서 사용하는 물리엔진
            // OverlapCircleAll(원점, 반지름, 레이어 마스크) : point를 중심으로, 반지름의 원을 생성하여, 그 안에 있는 Colliders 들을 검출할 수 있다.
            // 충돌조건에 맞지 않아도 충돌체를 검출할 수 있다. (검출하는 오브젝트와 검출할 오브젝트에 Rigedbody2D가 없어도 되고, Collision Layer Matrix에서 서로의 충돌을 무시해도 가능함)
            // Collider2D array를 반환한다. (반환때마다, 메모리가 할당되어 오버헤드 발생 가능성) 없으면 빈 배열을 반환한다.
            
            // Physics2D.OverlapCircleAll(transform.position, _enemySO.chaseDistance, 1 <<  LayerMask.NameToLayer("PLAYER"));   // "PLAYER" 직접 입력
            // Physics2D.OverlapCircleAll(transform.position, _enemySO.chaseDistance, 1 << 8); // Layer 번호(내가 추가한 PLAYER가 8번째라서 8 bit
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _enemySO.chaseDistance, _playerMask);
            
            // 가장 가까운 플레이어 검출
            // LINQ (링크, 링큐) : SQL Select, From, Where, OrderBy, Having, Join, ... 등을 c#에서 사용할 수 있음
            // LINQ는 SQL과 비슷한 문법을 지원하지만 사용할 때는 역순으로 입력해야함
            if (colliders.Length > 0)
            {
                // A(플레이어), B(슬라임) 거리 측정 방법
                // 1) Vector2.Distance(A,B) // 루트 연산(속도 느림)
                // 2) (A's Vector - B's Vector).magnitude     // 루트 연산(속도 느림)
                // 3) (A's Vector - B's Vector).sqrMagnitude  // 루트 연산 하지않음(속도 빠름, 위 방법보다 10배 이상)
                target = colliders
                    .OrderBy(c => (c.transform.position - transform.position).sqrMagnitude) // 정렬값  (c는 값에서 Linq 관례로 "colliders"의 첫 글자인 "c")
                    .First() // OrderBy한 첫 번쨰 값을 가져옴
                    .transform;
                
                // 조건절을 이용하여 3마리를 랜덤으로 추출
                // target = colliders
                //     .Where(c => (c.transform.position - transform.position).sqrMagnitude >= _enemySO.attackDistance)
                //     .OrderBy(c => Random.value)
                //     .Take(3)
                //     .FirstOrDefault()?.transform;
                
                return target != null;
            }
            
            target = null;
            return false;
        }
        
        // 주인공 검출 시간 확인
        public bool PlayerDetectable()
        {
            if (Time.time >= _lastDetectTime + _detectInterval)
            {
                _lastDetectTime = Time.time;
                return true;
            }
            return false;
        }

        public void MoveToPlayer()
        {
            if (target == null) return;
            
            // 이동 방향 계산(목표 방향 = (목표 위치 - 현재 위치).normalized)
            Vector2 direction = (target.position - transform.position).normalized;
            // Target의 위치에 따라서 스프라이트의 FlipX 속성 변경
            _spriteRenderer.flipX = direction.x < 0;

            // 목표 방향으로 이동
            _rb.linearVelocity = direction * _enemySO.moveSpeed;
            // transform.Translate(transform.position * Time.deltaTime * 1.0f);
        }
        
        // 추적 정지
        public void StopMoving()
        {
            _rb.linearVelocity = Vector2.zero;
            _animator.SetBool(hashIsWalk, false);
        }
        
        // 공격 쿨타임이 지났는지 여부를 확인하는 메서드
        public bool CanAttack(float lastAttackTime)
        {
            if (Time.time > lastAttackTime + _enemySO.attackCooldown)
            {
                return true;
            }
            return false;
        }
        
        // 공격 사정거리 이내에 플레이어 존재 여부 확인
        public bool PlayerAttackable()
        {
            float attackRange = (target.position - transform.position).sqrMagnitude;
            return (attackRange <= _enemySO.attackDistance * _enemySO.attackDistance);  // Mathf.Pow() => 사용 X, 속도 느림
        } 
        #endregion
        
        #region 충돌 감지 메서드

        // Collider가 충돌할 때 호출
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("PLAYER")) // or other.tag == "PLAYER"
            {
                var player = other.gameObject.GetComponent<IDamagable>();
                player?.TakeDamage(_enemySO.attackDamage);
            }
        }
        
        // 겹쳐진 상태일 때 계속 호출(웬만하면 사용 X -> 속도 문제)
        // private void OnTriggerStay2D(Collider2D other) { ... }
        
        // 충돌이 떨어질 때 호출
        // private void OnTriggerExit2D(Collider2D other) { ... }
        
        // 위 콜백말고도 더 많은 콜백이 있는데, 매번 보고 작성하기가 어려움
        // 그래서 하기 조건을 만족하는 것들에서 위 콜백들이 호출됨
        // Collider 충돌 조건
        // 1. 양쪽 다 Collider2D 컴포넌트가 존재해야 함
        // 2. 이동하는 객체에는 Rigidbody2D
        
        /* IsTrigger 체크
         * OnTriggerEnter / OnTriggerStay / OnTriggerExit
         *
         * IsTrigger 언체크
         * 
         */

        #endregion

        #region 테스트 코드
        private void TestFSM()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                ChangeState<IdleState>();
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                ChangeState<ChaseState>();
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                ChangeState<AttackState>();
            }
        }
        #endregion
    }
}
