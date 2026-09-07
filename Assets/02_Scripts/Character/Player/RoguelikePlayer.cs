using UnityEngine;
using DungeonMaster.InputSystem;
using DungeonMaster.Core;
using Unity.Cinemachine;
using UnityEngine.UI;
// ReSharper disable All

namespace DungeonMaster.Character.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(InputHandler))]
    public abstract class RoguelikePlayer : Player
    {
        [SerializeField] protected Image _expBar;

        #region 유니티 생명주기
        protected override void Awake()
        {
            Debug.Log($"RoguelikePlayer::Awake()");
            // 초기 체력 설정
            _currHp = _maxHp;

            // 컴포넌트 캐싱 (this.gameObject.GetComponent<T>())
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _inputHandler = GetComponent<InputHandler>();

            // GetComponentsInChildren 함수는 깊이 우선 탐색(DFS)으로 처음 하위 오브젝트에서 더 하위 오브젝트로 내려가면서 배열에 할당함.
            /* Player 오브젝트와 하위 오브젝트에 모두 Image 컴포넌트가 있다고 하면, Player, Arm, Pivot, Sword, Head 순으로 배열에 할당됨
             * Player
                ├─ Arm
                │   └─ Pivot
                │       └─ Sword 
                └─ Head
            */
            // 하지만 여기서는 GameObject.Find로 "Canvas" 오브젝트를 찾았으니 "Canvas" 오브젝트부터 하위로 Image 컴포넌트 탐색함
            // _hpBar2 = GameObject.Find("Canvas").GetComponentsInChildren<Image>()[2];

            // Weapon Arm 설정
            _weaponArm = transform.Find("Arm");
            // Find() 함수는 Update(), FixedUpdate() 함수에서는 절대 사용하지 말 것 => 성능 저하
            // this.gameObject.Find => Root(Hierarchy)에서부터 찾음
            // this.gameObject.transform.Find => 해당 Transform의 위치에서부터 찾음
            // Transform는 GetComponent처럼 가져오는 방식이 아닌 직접 접근할 수 있는 shorthand를 유니티에서 지원함
        }

        protected override void OnEnable()
        {
            _inputHandler.OnMoveAction += OnMove;
            // _inputHandler.OnAttackAction += OnAttack;
            _inputHandler.OnInteractAction += OnInteract;
        }

        protected override void OnDisable()
        {
            _inputHandler.OnMoveAction -= OnMove;
            // _inputHandler.OnAttackAction -= OnAttack;
            _inputHandler.OnInteractAction -= OnInteract;
        }

        private void Update()
        {
            if (_isDead) return;

            if (Time.time < lastAttackTime + _attackCooldown) return;

            lastAttackTime = Time.time;
            _animator.SetTrigger(hashAttack);
            Attack();
        }
        #endregion
    }
}
