using UnityEngine;
using DungeonMaster.InputSystem;
using DungeonMaster.Core;
using Unity.Cinemachine;
using UnityEngine.UI;
using System;
using MoreMountains.Tools;
using DungeonMaster.Weapon;

// ReSharper disable All

namespace DungeonMaster.Character.Player
{
    public abstract class RoguelikePlayer : Player
    {
        // [SerializeField] protected Image _expBar;
        [SerializeField] protected MMProgressBar _expBar;
        [SerializeField] private float _nextExp = 1.5f;

        public event Action<int> OnLevelUpAction;
        
        private int _maxExp = 25;
        private int _currExp = 0;
        private int _level = 1;

        // 공전 무기. 업그레이드가 이걸 통해 강화한다
        public WeaponOrbit Orbit { get; private set; }

        #region 뱀서 전용 스탯
        // 구 GamePlay 씬에는 없는 개념이라 Player.cs 를 오염시키지 않고 여기에만 둔다
        [Header("뱀서 전용 스탯")]
        [Tooltip("경험치 코인이 빨려오기 시작하는 거리")]
        [SerializeField] private float _pickupRadius = 3f;

        private float _bonusPickup;
        private float _mulPickup = 1f;
        private float _mulExpGain = 1f;

        public float PickupRadius => (_pickupRadius + _bonusPickup) * _mulPickup;
        public float ExpGainMul => _mulExpGain;
        #endregion

        #region 유니티 생명주기
        protected override void Awake()
        {
            Debug.Log($"RoguelikePlayer::Awake()");
            // 초기 체력 설정
            _currHp = MaxHp;

            // 컴포넌트 캐싱 (this.gameObject.GetComponent<T>())
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _inputHandler = GetComponent<InputHandler>();

            // 계층이 Player > WeaponRoot > OrbitPivot 이라 손자까지 훑어야 함
            Orbit = GetComponentInChildren<WeaponOrbit>();

            // _expBar.fillAmount = 0f;
        }

        protected override void OnEnable()
        {
            _inputHandler.OnMoveAction += OnMove;
            // _inputHandler.OnAttackAction += OnAttack;
            _inputHandler.OnInteractAction += OnInteract;

            _expBar.OnBarMovementIncreasingStop.AddListener(OnExpBarFilled);
        }

        protected override void OnDisable()
        {
            _inputHandler.OnMoveAction -= OnMove;
            // _inputHandler.OnAttackAction -= OnAttack;
            _inputHandler.OnInteractAction -= OnInteract;

            _expBar.OnBarMovementIncreasingStop.RemoveListener(OnExpBarFilled);
        }

        private void Update()
        {
            if (_isDead) return;

            if (Time.time < lastAttackTime + AttackCooldown) return;

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
            _currExp += Mathf.Max(1, Mathf.RoundToInt(exp * ExpGainMul));

            // 여기서는 레벨업하지 않는다. 바가 다 찬 뒤에 OnExpBarFilled가 처리함
            RefreshExpBar();
        }

        private void LevelUp()
        {
            _currExp -= _maxExp;                    // _maxExp를 올리기 전에 빼야 함
            _maxExp = (int)(_maxExp * _nextExp);
            _level++;

            Debug.Log($"레벨업! 현재 레벨:{_level} (다음까지 {_currExp}/{_maxExp})");

            // 내부 상태를 전부 갱신한 뒤에 알린다.
            // 먼저 던지면 구독자가 아직 오르기 전 레벨을 보게 됨
            OnLevelUpAction?.Invoke(_level);
        }

        // MMProgressBar가 목표치까지 다 차오르면 호출됨
        private void OnExpBarFilled()
        {
            // 가득 차지 않은 채로 멈춘 경우(경험치만 조금 오른 경우)
            if (_currExp < _maxExp) return;

            LevelUp();

            // 바는 여기서 건드리지 않는다.
            // 카드 화면이 떠 있는 동안 가득 찬 상태로 멈춰 있어야 함
        }

        private void RefreshExpBar()
        {
            // UpdateBar01은 내부에서 Clamp01 하므로 1을 넘겨도 안전
            _expBar.UpdateBar01((float)_currExp / _maxExp);
        }

        // 레벨업 카드(PlayerStatUpgradeSO)가 호출하는 단일 진입점
        public void ApplyStatUpgrade(PlayerStat stat, float flat, float percent)
        {
            switch (stat)
            {
                case PlayerStat.MaxHp:
                    AddMaxHp(flat, percent);
                    break;

                case PlayerStat.MoveSpeed:
                    AddMoveSpeed(flat, percent);
                    break;

                case PlayerStat.CooldownReduction:
                    AddCooldownReduction(percent);
                    break;

                case PlayerStat.PickupRadius:
                    _bonusPickup += flat;
                    _mulPickup += percent;
                    break;

                case PlayerStat.ExpGain:
                    _mulExpGain += percent;
                    break;
            }
        }

        // 카드 선택이 끝나면 LevelUpUI가 호출
        public void ResumeExpGain()
        {
            _expBar.SetBar01(0f);   // 애니메이션 없이 즉시 비움
            RefreshExpBar();        // 잔여 경험치만큼 차오름
        }
    }
}
