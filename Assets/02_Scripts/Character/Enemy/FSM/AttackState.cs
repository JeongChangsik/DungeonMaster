using UnityEngine;

namespace DungeonMaster.Character.Enemy.FSM
{
    public class AttackState : IState
    {
        public void OnEnter(Enemy enemy)
        {
            Debug.Log($"AttackState::OnEnter()");
            enemy.StopMoving();
        }

        public void OnUpdate(Enemy enemy)
        {
            // 넉백 중에는 거리 판정, 공격 시작 모두 스킵
            if (enemy.IsKnockbacking) return;
            
            // Debug.Log($"AttackState::OnUpdate()");
            // 플레이어와의 거리가 공격 사정거리 이내이면 공격
            
            // 공격 범위 내에 없다면 공격 불가 -> 다시 추적
            if(!enemy.PlayerAttackable()) enemy.ChangeState<ChaseState>(); 
            
            // 슬라임 전용 대시 공격
            if (enemy is Swampy swampy && enemy.CanAttack(swampy.LastAttackTime))
            {
                enemy.StartCoroutine(swampy.DashAttack());
            }
        }

        public void OnExit(Enemy enemy)
        {
            Debug.Log($"AttackState::OnExit()");
        }
    }
}