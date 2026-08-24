using UnityEngine;
using DungeonMaster.Character.Enemy.FSM;
using UnityEngine.InputSystem;

namespace DungeonMaster.Character.Enemy
{
    public class Enemy : MonoBehaviour
    {
        //  상태 머신 변수 선언
        protected StateMachine _stateMachine;

        // 상태 전환 메서드
        public void ChangeState(IState newState)
        {
            _stateMachine.ChangeState(newState);
        }

        #region 유니티 생명주기
        protected void Awake()
        {
            Debug.Log($"Enemy::Awake()");

            // 상태 머신 초기화
            _stateMachine = new StateMachine(this);

            // 초기 상태 설정(IdleState)
            ChangeState(new IdelState());
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
                ChangeState(new IdelState());
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                ChangeState(new ChaseState());
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                ChangeState(new AttackState());
            }
        }
        #endregion
    }
}
