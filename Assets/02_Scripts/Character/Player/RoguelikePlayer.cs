using UnityEngine;
using DungeonMaster.InputSystem;
using DungeonMaster.Core;
using Unity.Cinemachine;
using UnityEngine.UI;
using System;
// ReSharper disable All

namespace DungeonMaster.Character.Player
{
    public abstract class RoguelikePlayer : Player
    {
        [SerializeField] protected Image _expBar;
        private event Action OnLevelUpAction;

        private int _maxExp = 25;
        private int _currExp = 0;
        private int _level = 1;

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

            _expBar.fillAmount = 0f;
        }

        protected override void OnEnable()
        {
            _inputHandler.OnMoveAction += OnMove;
            // _inputHandler.OnAttackAction += OnAttack;
            _inputHandler.OnInteractAction += OnInteract;
            OnLevelUpAction += LevelUp;
        }

        protected override void OnDisable()
        {
            _inputHandler.OnMoveAction -= OnMove;
            // _inputHandler.OnAttackAction -= OnAttack;
            _inputHandler.OnInteractAction -= OnInteract;
            OnLevelUpAction -= LevelUp;
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

        protected override void FlipDirection(bool facingRight)
        {
            if (facingRight)
            {
                // 오른쪽
                _spriteRenderer.flipX = false;
            }
            else
            {
                // 왼쪽
                _spriteRenderer.flipX = true;
            }
        }

        public void AddExp(int exp)
        {
            _currExp += exp;
            _expBar.fillAmount = Mathf.Min(1.0f, (float)_currExp / _maxExp);
            if(_currExp >= _maxExp) OnLevelUpAction?.Invoke();
        }

        private void LevelUp()
        {
            // 잔여 경험치를 다음 레벨에 이관
            if(_currExp > _maxExp) _currExp -= _maxExp;
            else _currExp = 0;

            _maxExp = (int)(_maxExp * 1.5f);
            _level++;

            Debug.Log($"레벨업! 현재 레벨:{_level}");
            Debug.Log($"다음 레벨업까지 {_currExp}/{_maxExp}");
            
            // TODO: 레벨업 텍스트 출력

            // TODO: 카드 선택 지 UI 오픈

        }
    }
}
