using UnityEngine;

namespace DungeonMaster.Character.Enemy.FSM
{
    public class AttackState : IState
    {
        public void OnEnter(Enemy enemy)
        {
            Debug.Log($"AttackState::OnEnter()");

        }

        public void OnUpdate(Enemy enemy)
        {
            // Debug.Log($"AttackState::OnUpdate()");
            // 플레이어와의 거리가 공격 사정거리 이내이면 공격
            
        }

        public void OnExit(Enemy enemy)
        {
            Debug.Log($"AttackState::OnExit()");
        }

    }
}