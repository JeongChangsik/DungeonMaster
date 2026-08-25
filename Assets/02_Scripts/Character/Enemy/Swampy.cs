using System;
using System.Collections.Generic;
using DungeonMaster.Character.Enemy.FSM;
using UnityEngine;

namespace DungeonMaster.Character.Enemy
{
    public class Swampy : Enemy
    {
        protected override void InitState()
        {
            // Indexer 방식 값 추가
            _states = new Dictionary<Type, IState>
            {
                [typeof(IdleState)] = new IdleState(),
                [typeof(ChaseState)] = new ChaseState(),
                [typeof(AttackState)] = new AttackState(),
                [typeof(IdleState)] = new IdleState(),
            };
            Debug.Log($"Swampy::InitState() 상태 초기화 완료");
            
        }
    }
}
