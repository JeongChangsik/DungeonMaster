using UnityEngine;
using DungeonMaster.InputSystem;


public class PlayerController : MonoBehaviour 
{
    #region 유니티 생명주기
    private void OnEnable()
    {
        // 구독처리
        InputHandler.OnMoveAction += OnPlayerMove;
        InputHandler.OnAttackAction += OnPlayerAttack;
    }

    private void OnDisable()
    {
        // 구독해지
        InputHandler.OnMoveAction -= OnPlayerMove;
        InputHandler.OnAttackAction -= OnPlayerAttack;
    }
    #endregion

    #region 콜백 메서드
    private void OnPlayerMove(Vector2 ctx)
    {
        Debug.Log($"플레이어 이동: {ctx}");
    }

    private void OnPlayerAttack()
    {
        Debug.Log($"플레이어 공격!");
    }
    #endregion

}