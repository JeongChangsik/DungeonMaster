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

        public void Start()
        {
            
        }
        #endregion

    }
}
