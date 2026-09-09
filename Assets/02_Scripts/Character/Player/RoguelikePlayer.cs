using UnityEngine;
using DungeonMaster.InputSystem;
using DungeonMaster.Core;
using Unity.Cinemachine;
using UnityEngine.UI;
using System;
using System.Collections;
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

        // 무기 전체를 관리한다. WeaponRoot 에 붙어 있음
        public WeaponManager Weapons { get; private set; }

        // 기존 업그레이드 SO(OrbitCountUpgradeSO 등) 호환용.
        // 새 코드는 Weapons.Get<T>() 를 쓸 것
        public WeaponOrbit Orbit => Weapons != null ? Weapons.Get<WeaponOrbit>() : null;

        #region 뱀서 전용 스탯
        // 구 GamePlay 씬에는 없는 개념이라 Player.cs 를 오염시키지 않고 여기에만 둔다
        [Header("뱀서 전용 스탯")]
        [Tooltip("경험치 코인이 빨려오기 시작하는 거리")]
        [SerializeField] private float _pickupRadius = 3f;

        private float _bonusPickup;
        private float _mulPickup = 1f;
        private float _mulExpGain = 1f;

        // 모든 무기에 공통으로 곱해지는 배율. WeaponBase 가 읽어간다
        private float _mulWeaponDamage = 1f;
        private float _mulWeaponArea = 1f;
        private float _mulWeaponRate = 1f;

        public float PickupRadius => (_pickupRadius + _bonusPickup) * _mulPickup;
        public float ExpGainMul => _mulExpGain;
        public float WeaponDamageMul => _mulWeaponDamage;
        public float WeaponAreaMul => _mulWeaponArea;
        public float WeaponRateMul => _mulWeaponRate;

        // 초당 체력 회복. 회복 수단이 전혀 없으면 한 번 깎인 체력을 되돌릴 방법이 없다
        private float _regenPerSecond;
        private float _regenBuffer;      // 1 이상 쌓이면 정수 단위로 회복시킨다

        public float HealthRegen => _regenPerSecond;
        #endregion

        #region 무적 시간
        // 적이 겹치면 각자 자기 쿨타임으로 동시에 때려서 순식간에 죽는다.
        // 한 번 맞으면 짧게 무적이 되도록 한다.
        // Player.cs 가 아니라 여기에 두는 이유: 구 GamePlay 씬의 난이도를 바꾸지 않기 위함.
        [Header("무적 시간")]
        [SerializeField] private float _invincibleDuration = 0.6f;
        [Tooltip("무적 동안 깜빡이는 간격")]
        [SerializeField] private float _flickerInterval = 0.08f;

        private float _lastHitTime = -999f;
        private Coroutine _flickerRoutine;

        public bool IsInvincible => Time.time < _lastHitTime + _invincibleDuration;
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

            // 계층이 Player > WeaponRoot > (OrbitPivot / ProjectileMuzzle / AuraArea)
            Weapons = GetComponentInChildren<WeaponManager>();
            if (Weapons == null) Debug.LogError("RoguelikePlayer::Awake() WeaponManager 를 찾지 못했습니다. WeaponRoot 에 붙어 있는지 확인하세요.");

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

            TickRegen();

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

        public override void TakeDamage(float damage)
        {
            if (_isDead) return;
            if (IsInvincible) return;      // 겹친 적들에게 동시에 맞아 즉사하는 것을 막는다

            _lastHitTime = Time.time;
            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.playerHurtSFX : null);
            base.TakeDamage(damage);

            if (!_isDead) StartFlicker();
        }

        // 무적인 동안 스프라이트를 깜빡여 상태를 눈에 보이게 한다
        private void StartFlicker()
        {
            if (_flickerRoutine != null) StopCoroutine(_flickerRoutine);
            _flickerRoutine = StartCoroutine(FlickerCo());
        }

        private IEnumerator FlickerCo()
        {
            while (IsInvincible)
            {
                _spriteRenderer.enabled = !_spriteRenderer.enabled;
                yield return new WaitForSeconds(_flickerInterval);
            }

            // 깜빡임이 꺼진 채로 끝나지 않도록 반드시 되돌린다
            _spriteRenderer.enabled = true;
            _flickerRoutine = null;
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

        // 매 프레임 소수점 단위로 회복시키면 HP바가 계속 떨리므로
        // 1 이상 쌓였을 때만 실제로 회복시킨다
        private void TickRegen()
        {
            if (_regenPerSecond <= 0f) return;
            if (_currHp >= MaxHp) { _regenBuffer = 0f; return; }

            _regenBuffer += _regenPerSecond * Time.deltaTime;
            if (_regenBuffer < 1f) return;

            float amount = Mathf.Floor(_regenBuffer);
            _regenBuffer -= amount;
            Heal(amount);
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

                case PlayerStat.HealthRegen:
                    _regenPerSecond += flat;
                    break;

                // 전역 무기 배율은 값만 바꿔선 부족하다.
                // 이미 배치된 칼날들이 옛 피해량을 들고 있으므로 반드시 다시 내려보내야 한다
                case PlayerStat.WeaponDamage:
                    _mulWeaponDamage += percent;
                    if (Weapons != null) Weapons.RebuildAll();
                    break;

                case PlayerStat.WeaponArea:
                    _mulWeaponArea += percent;
                    if (Weapons != null) Weapons.RebuildAll();
                    break;

                case PlayerStat.WeaponRate:
                    _mulWeaponRate += percent;
                    if (Weapons != null) Weapons.RebuildAll();
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
