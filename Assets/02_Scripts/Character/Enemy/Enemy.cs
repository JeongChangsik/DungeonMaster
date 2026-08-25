using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonMaster.Character.Enemy.FSM;
using UnityEngine.InputSystem;

namespace DungeonMaster.Character.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class Enemy : MonoBehaviour
    {
        //  상태 머신 변수 선언
        protected StateMachine _stateMachine;

        // 상태 전환 메서드
        public void ChangeState<T>() where T : IState
        {
            if (CheckStateMachine())
            {
                // 딕셔너리에 저장된 상태(State)를 가져와서 전환
                if (_states.TryGetValue(typeof(T), out IState state))
                {
                    _stateMachine?.ChangeState(state);
                }
            }
        }

        private bool CheckStateMachine() => _stateMachine != null ? true : false;
        
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

        protected void Start()
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
