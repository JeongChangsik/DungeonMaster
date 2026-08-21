using System;
using UnityEngine;

namespace DungeonMaster.Character.Player
{
    public class Warrior : Player
    {
        [Header("전사 전용 스탯")]
        [SerializeField] private float _defense = 10f;

        protected override void Attack()
        {
            Debug.Log("공격 실행");
        }

        public override void TakeDamage(float damage)
        {
            // 방어력 적용
            float actualDamage = Mathf.Max(1f, damage - _defense);  // 최소 5 데미지
            base.TakeDamage(actualDamage);
            Debug.Log($"Warrior가 {actualDamage}의 피해를 입었습니다 (HP: {_currHp} / {_maxHp})");
        }

        #region 유니티 생명주기
        protected override void Awake()
        {
            Debug.Log($"Warrior::Awake()");
            // 전사의 기본 스탯 설정
            _maxHp = 150f;
            _moveSpeed = 4f;
            _attackDamage = 25f;
            _attackCooldown = 0.7f;
            base.Awake();
        }
        #endregion

    }
}