using System;
using DungeonMaster.Core;
using UnityEngine;

namespace DungeonMaster.Character.Player
{
    public class Warrior : Player
    {
        [Header("적 검출 설정")]
        [SerializeField] private Vector2 _size = new Vector2(1f, 2f);
        [SerializeField] private float _offset = 1f;
        [SerializeField] private LayerMask _enemyLayer;
        private Vector2 _direction;
        private Vector2 _center;

        // [Header("오디오 설정")]
        // [SerializeField] private AudioClip _attackSFX;
        // private AudioSource _audioSource;
        
        // AudioSource
        // AudioSource.Play 오디오 클립 실행 중에 다시 실행되면 끊김
        // AudioSource.PlayOneShot 중첩되어도 실행될 수 있음
        // 
        
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
            
            // _audioSource = GetComponent<AudioSource>();
        }
        private void OnDrawGizmos()
        {
            if(_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            
            _direction = _spriteRenderer.flipX ? Vector2.left : Vector2.right;
            _center = (Vector2)transform.position + (_direction * _offset);
            
            // Gizmos.color = Color.chartreuse;
            Gizmos.color = new Color(1f, 0f, 0f, 0.8f); // a: alpha 투명도 (1f 이면 불투명)
            // Gizmos.DrawWireSphere(transform.position, );  // 3d로 그림
            // Gizmos.DrawWireCube(_center, _size);    
            Gizmos.DrawCube(_center, _size);    
        }
        #endregion

        #region 공격 및 피격 처리
        protected override void Attack()
        {
            Debug.Log("공격 실행");
            // _audioSource.PlayOneShot(_attackSFX, 0.8f);
            AudioManager.Instance.PlayerSFX(AudioManager.Instance.AudioDataSO.playerAttackSFX);
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
            // Debug.Log($"전사 공격 처리");
            // 실제 공격 처리 로직
            // 공격 범위 계산 (박스, 오프셋)
            Vector2 direction = _spriteRenderer.flipX ? Vector2.left : Vector2.right;
            Vector2 center = (Vector2)transform.position + (direction * _offset);

            // 추출 OverlapBoxAll
            Collider2D[] colliders = Physics2D.OverlapBoxAll(center, _size, 0, _enemyLayer);

            if(colliders.Length > 0) CameraShake.Instance.Shake();

            foreach (var collider in colliders)
            {
                collider.GetComponent<IDamagable>()?.TakeDamage(_warriorSO.attackDamage);
            }
        }
        #endregion

        private void OnTriggerEnter2D(Collider2D other)
        {
            if(other.CompareTag("Enemy"))
            {
                CameraShake.Instance.Shake();
            }
        }

    }
}