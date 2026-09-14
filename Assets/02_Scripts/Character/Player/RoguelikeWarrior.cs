using System;
using DungeonMaster.Core;
using UnityEngine;

namespace DungeonMaster.Character.Player
{
    // [하는 일] 로그라이크(뱀서라이크) 씬에서 쓰는 전사 캐릭터. 전사만의 스탯(WarriorSO)과 방어력을 담당한다.
    // [붙이는 곳] RogueLike 씬의 플레이어 오브젝트.
    // [연결] 부모 RoguelikePlayer 가 경험치, 무적 시간, 사망 연출, 스탯 강화를 맡는다.
    //        '방어력 증가' 카드는 RoguelikePlayer.ApplyStatUpgrade -> AddDefense(여기서 override) 로 들어온다.
    // [설계] 구 GamePlay 씬의 Warrior.cs 를 복사해 만든 파일이다. Warrior.cs 는 건드리지 않는 규칙이라
    //        뱀서용 차이(무기가 공격, 방어력 상한 등)는 전부 이쪽에만 넣었다.
    public class RoguelikeWarrior : RoguelikePlayer
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
        [Tooltip("방어력 상한. 이걸 안 두면 카드를 몰아서 먹었을 때 모든 피해가 1이 된다")]
        [SerializeField] private float _maxDefense = 30f;
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
        #endregion

        #region 공격 및 피격 처리
        // 로그라이크 씬의 공격은 전부 WeaponRoot 아래 무기들이 알아서 한다.
        // 이 메서드는 부모의 자동 공격 타이머가 부르는 자리라 비워 둔다
        // (부모 Player 에서 abstract 로 선언돼 있어서, 비어 있더라도 override 는 반드시 적어야 한다)
        protected override void Attack()
        {
        }

        public override void TakeDamage(float damage)
        {
            // 방어력 적용
            float actualDamage = Mathf.Max(1f, damage - _defense);  // 최소 1 데미지
            base.TakeDamage(actualDamage);
        }

        // '방어력 증가' 카드가 여기로 들어온다.
        // 방어력은 피해에서 그냥 빼는 방식이라 무한정 올리면 모든 피해가 1이 되어버린다.
        // 상한을 둬서 최소한의 위협은 남긴다
        protected override void AddDefense(float amount)
        {
            _defense = Mathf.Min(_defense + amount, _maxDefense);
        }

        public float Defense { get { return _defense; } }

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

            // 구 Warrior 는 _warriorSO.attackDamage 를 그대로 쓰지만,
            // 여기서는 강화(보너스, 배율)가 반영된 AttackDamage 프로퍼티를 쓴다
            foreach (var collider in colliders)
            {
                collider.GetComponent<IDamagable>()?.TakeDamage(AttackDamage);
            }
        }
        #endregion

        // OnTriggerEnter2D 로 "적과 닿을 때" 화면을 흔들던 코드를 없앴다.
        // 뱀서라이크는 적 수십 마리가 항상 몸에 붙어 있어서 화면이 쉬지 않고 흔들렸고,
        // 무적 시간이라 피해를 안 입은 순간에도, 심지어 죽은 뒤에도 흔들렸다.
        // 지금은 RoguelikePlayer.TakeDamage 에서 실제로 체력이 깎일 때만 흔든다.

    }
}