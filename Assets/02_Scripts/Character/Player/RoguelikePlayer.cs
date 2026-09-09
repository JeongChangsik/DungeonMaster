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

        public int Level { get { return _level; } }

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
        [Tooltip("맞았을 때 화면이 흔들리는 세기. 0이면 흔들리지 않는다")]
        [SerializeField] private float _hitShakeForce = 0.6f;
        [Tooltip("맞는 순간 화면이 잠깐 멎는 시간(히트스톱). 0이면 멎지 않는다")]
        [SerializeField] private float _hitStopDuration = 0.05f;

        [Header("사망 연출")]
        [Tooltip("옆으로 쓰러지며 붉게 물드는 시간")]
        [SerializeField] private float _deathFallDuration = 0.6f;
        [Tooltip("쓰러진 뒤의 색")]
        [SerializeField] private Color _deathColor = new Color(0.7f, 0.15f, 0.15f, 1f);

        private float _lastHitTime = -999f;
        private Coroutine _flickerRoutine;
        private Coroutine _hitStopRoutine;

        // 죽는 처리가 두 번 돌지 않도록
        private bool _deathHandled;

        // 쓰러지는 연출이 끝나면 결과 화면(GameOverUI)에 알린다
        public event Action OnDied;

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
            ShakeOnHit(damage);
            HitStop();
            base.TakeDamage(damage);

            if (!_isDead) StartFlicker();
        }

        // 화면 흔들림은 "실제로 체력이 깎였을 때"만 일어나야 한다.
        // 적과 닿기만 해도 흔들면 적 수십 마리가 몰린 순간 화면이 계속 요동쳐서
        // 정작 진짜 피격을 구분할 수 없게 된다.
        private void ShakeOnHit(float damage)
        {
            if (_hitShakeForce <= 0f) return;

            // 큰 피해일수록 크게 흔든다. 최대 체력의 10%를 맞았을 때가 기준 세기
            float ratio = MaxHp > 0f ? damage / (MaxHp * 0.1f) : 1f;
            CameraShake.Instance.Shake(_hitShakeForce * Mathf.Clamp(ratio, 0.5f, 2f));
        }

        #region 사망
        // 죽는 순간 세상을 통째로 멈춘다.
        //
        // 죽음의 무게는 '정지'에서 나온다. 적도, 무기도, 시간도 그 자리에서 멎고
        // 주인공만 천천히 쓰러진다. 그래서 쓰러지는 연출은 전부 unscaled 로 움직인다
        // (timeScale 이 0이라 보통 시간으로는 아무것도 움직이지 않는다).
        protected override void Die()
        {
            if (_deathHandled) return;
            _deathHandled = true;

            base.Die();

            Time.timeScale = 0f;

            // 진행 중인 연출을 전부 멈춘다.
            // 특히 히트스톱을 안 멈추면 0.05초 뒤에 시간을 1로 되돌려 버린다
            if (_flickerRoutine != null) { StopCoroutine(_flickerRoutine); _flickerRoutine = null; }
            if (_hitStopRoutine != null) { StopCoroutine(_hitStopRoutine); _hitStopRoutine = null; }

            if (_spriteRenderer != null) _spriteRenderer.enabled = true;   // 깜빡이다 꺼진 채로 멈추지 않게
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            _moveInput = Vector2.zero;

            // 애니메이터를 꺼야 한다.
            // Player@hit 클립이 스프라이트 색을 직접 제어하기 때문에,
            // 켜둔 채로 색을 칠하면 다음 프레임에 애니메이터가 그대로 덮어쓴다.
            // 끄면 마지막 그림에서 멈추고, 그 위에 쓰러지는 연출을 얹을 수 있다
            if (_animator != null) _animator.enabled = false;

            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.playerDeathSFX : null, 0f);

            StartCoroutine(DeathFallCo());
        }

        private IEnumerator DeathFallCo()
        {
            Color from = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
            Quaternion fromRot = transform.rotation;
            Quaternion toRot = Quaternion.Euler(0f, 0f, 90f);   // 옆으로 쓰러진다

            float t = 0f;
            while (t < _deathFallDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / _deathFallDuration);

                if (_spriteRenderer != null) _spriteRenderer.color = Color.Lerp(from, _deathColor, k);
                transform.rotation = Quaternion.Slerp(fromRot, toRot, k);
                yield return null;
            }

            if (_spriteRenderer != null) _spriteRenderer.color = _deathColor;
            transform.rotation = toRot;

            if (OnDied != null) OnDied();
        }
        #endregion

        // 맞는 순간 화면 전체를 아주 잠깐 멈춘다. 한 대가 묵직하게 느껴진다.
        //
        // 적이 맞을 때마다 거는 것은 일부러 하지 않았다.
        // 화면에 적이 백 마리 넘게 있고 공전 칼날이 쉬지 않고 때리기 때문에,
        // 타격마다 멈추면 게임 전체가 슬로모션이 된다.
        // 플레이어가 맞을 때만 걸면 무적 시간(0.6초) 덕분에 최대 초당 한 번뿐이라 안전하다.
        private void HitStop()
        {
            if (_hitStopDuration <= 0f) return;

            // 레벨업 카드로 이미 시간이 멈춰 있으면 건드리지 않는다.
            // 여기서 끼어들면 카드를 고르는 도중에 시간이 다시 흘러버린다
            if (!Mathf.Approximately(Time.timeScale, 1f)) return;

            if (_hitStopRoutine != null) StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = StartCoroutine(HitStopCo());
        }

        private IEnumerator HitStopCo()
        {
            Time.timeScale = 0f;

            // 시간이 멈춘 동안에도 흐르는 실제 시간으로 기다려야 한다.
            // WaitForSeconds 를 쓰면 timeScale 이 0이라 영원히 안 끝난다
            yield return new WaitForSecondsRealtime(_hitStopDuration);

            // 기다리는 사이에 레벨업 카드가 떴다면 그쪽이 시간을 관리하게 둔다
            if (Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = 1f;
            _hitStopRoutine = null;
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

        // 방어력을 가진 직업이 재정의한다. 없는 직업은 이 카드를 먹어도 아무 일도 없다
        protected virtual void AddDefense(float amount) { }

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

                // 방어력은 직업마다 있을 수도, 없을 수도 있다.
                // 전사만 _defense 를 들고 있으므로 하위 클래스에 맡긴다
                case PlayerStat.Defense:
                    AddDefense(flat);
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
