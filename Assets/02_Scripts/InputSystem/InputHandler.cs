using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonMaster.InputSystem
{
    public class InputHandler : MonoBehaviour
    {
        // InputSystem_Action의 인스턴스를 저장하기 위한 변수
        private InputSystem_Actions _inputActions;

        // 액션을 참조할 변수
        private InputAction _moveAction;
        private InputAction _attackAction;
        private InputAction _interactAction;

        #region 유니티 생명주기
        private void Awake()
        {
            _inputActions = new InputSystem_Actions();

            // 액션을 찾아와 바인딩
            _moveAction = _inputActions.Player.Move;
            _attackAction = _inputActions.Player.Attack;
            _interactAction = _inputActions.Player.Interact;
        }

        private void OnEnable()
        {
            // OnEnable 에서는 가장 먼저 액션 시스템을 활성화
            _inputActions.Enable();

            // delegate chain 연결
            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;
            _attackAction.performed += OnAttack;
            _interactAction.performed += OnInteract;
            _interactAction.canceled += OnInteract;
        }

        private void OnDisable()
        {
            // OnDisable 에서는 가장 먼저 액션 시스템을 비활성화 => 안하면 메모리 누수 발생
            _inputActions.Disable();

            // delegate chain 해제
            _moveAction.performed -= OnMove;
            _moveAction.canceled -= OnMove;
            _attackAction.performed -= OnAttack;
            _interactAction.performed -= OnInteract;
            _interactAction.canceled -= OnInteract;
        }
        #endregion

        /* Vector2 : 2차원 좌표(x,y)를 저장하는 데이터 타입, 구조체(Structure)
         * Vector3 : 3차원 좌표(x,y,z)를 저장하는 데이터 타입, 구조체(Structure)
         * 구조체(Structure) : 클래스와 비슷해 보이지만 상속이 불가능함. 값 타입(Value type), 메모리 공간: 스택(Stack) 
         * 클래스(Class) : 상속 가능. 참조 타입(Reference type), 메모리 공간: 힙(Heap)
         */
        #region 콜백 메서드
        private void OnMove(InputAction.CallbackContext ctx)
        {
            Debug.Log($"Move: {ctx.ReadValue<Vector2>()}");
        }

        private void OnAttack(InputAction.CallbackContext ctx)
        {
            Debug.Log($"Attack: 공격!");
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            if(ctx.phase == InputActionPhase.Performed)
            {
                Debug.Log($"Interact: 상호작용 시작");
            }
            else if(ctx.phase == InputActionPhase.Canceled)
            {
                Debug.Log($"Interact: 상호작용 종료");
            }
        }
        #endregion

    }
}
