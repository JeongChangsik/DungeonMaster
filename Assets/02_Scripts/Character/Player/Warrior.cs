using System;
using UnityEngine;

namespace DungeonMaster.Character.Player
{
    public class Warrior : Player
    {
        [Header("전사 전용 스탯")]
        [SerializeField] private WarriorSO _warriorSO;
        private float _defense;

        #region 유니티 생명주기
        protected override void Awake()
        {
            Debug.Log($"Warrior::Awake()");

            // 전사의 기본 스탯 설정
            _maxHp = _warriorSO.maxHp;
            _moveSpeed = _warriorSO.moveSpeed;
            _attackDamage = _warriorSO.attackDamage;
            _attackCooldown = _warriorSO.attackCooldown;
            _defense = _warriorSO.defense;
            base.Awake();
        }
        #endregion

        #region 공격 및 피격 처리
        protected override void Attack()
        {
            Debug.Log("공격 실행");
        }

        public override void TakeDamage(float damage)
        {
            // 방어력 적용
            float actualDamage = Mathf.Max(1f, damage - _defense);  // 최소 1 데미지
            base.TakeDamage(actualDamage);
            Debug.Log($"Warrior가 {actualDamage}의 피해를 입었습니다 (HP: {_currHp} / {_maxHp})");
        }

        // 애니메이션 이벤트에서 호출할 메서드
        public void OnAttackAnimEvent()
        {
            // TODO: 실제 공격 처리 로직
            Debug.Log($"전사 공격 처리");


        }
        
        #endregion

    }
}