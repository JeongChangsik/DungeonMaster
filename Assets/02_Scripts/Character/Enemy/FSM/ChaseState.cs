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
            // 넉백 중에는 거리 판정, 공격 시작 모두 스킵
            if (enemy.IsKnockbacking) return;
            
            // Debug.Log($"ChaseState::OnUpdate()");
            if (enemy.PlayerDetectable())
            {
                // 거리가 멀어지면 다시 IdleState로 전환
                if (enemy.DetectPlayer())
                {
                    // 공격 범위 내 플레이어 존재 여부 판단
                    if (enemy.PlayerAttackable())
                    {
                        // if (enemy is Swampy swampy && !swampy.CanAttack(swampy.LastAttackTime)) return;
                        enemy.ChangeState<AttackState>();
                    }
                    else
                    {
                        enemy.MoveToPlayer();
                    }
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