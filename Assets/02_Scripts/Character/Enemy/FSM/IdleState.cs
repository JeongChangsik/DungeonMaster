using UnityEngine;

namespace DungeonMaster.Character.Enemy.FSM
{
    public class IdleState : IState
    {
        public void OnEnter(Enemy enemy)
        {
            Debug.Log($"IdleState::OnEnter()");
            // 애니메이션 Idle로 변경

        }

        public void OnUpdate(Enemy enemy)
        {
            // Debug.Log($"IdleState::OnUpdate()");
            // 플레이어와의 거리를 측정하고 추적 사정거리 이내이면 추적로 변경
            
        }

        public void OnExit(Enemy enemy)
        {
            Debug.Log($"IdleState::OnExit()");
        }

    }
}