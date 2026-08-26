using UnityEngine;

namespace DungeonMaster.Character.Enemy.FSM
{
    public class ChaseState : IState
    {
        public void OnEnter(Enemy enemy)
        {
            Debug.Log($"ChaseState::OnEnter()");
            // 애니메이션 Walk로 변경
            enemy.SetWalk(true);
        }

        public void OnUpdate(Enemy enemy)
        {
            // Debug.Log($"ChaseState::OnUpdate()");
            if (enemy.PlayerDetectable())
            {
                // 거리가 멀어지면 다시 IdleState로 전환
                if (enemy.DetectPlayer())
                {
                    enemy.MoveToPlayer();
                }
                else
                {
                    enemy.ChangeState<IdleState>();
                }
                
            }
            
            // TODO: 플레이어와의 거리가 공격 사정거리 이내이면 공격
            
        }

        public void OnExit(Enemy enemy)
        {
            Debug.Log($"ChaseState::OnExit()");
        }

    }
}