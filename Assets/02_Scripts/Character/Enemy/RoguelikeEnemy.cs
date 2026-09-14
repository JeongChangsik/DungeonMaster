using System.Collections;
using DungeonMaster.Core;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using DungeonMaster.Weapon;
using UnityEngine;

namespace DungeonMaster.Character.Enemy
{
    // 뱀서라이크용 잡몹
    // 스폰 직후부터 플레이어를 향해 계속 직진하고, 겹쳐 있는 동안 주기적으로 데미지를 준다.
    //
    // 기존 Enemy/Swampy를 상속하지 않는 이유:
    //  - Enemy는 FSM(Idle/Chase/Attack) 전제로 만들어져 있어서
    //    탐색 반경 / 공격 사거리 / 넉백 스턴이 전부 "추적을 끊는" 방향으로 동작함
    //  - 여기서 필요한 건 상태 1개뿐이라 FSM, 탐색(OverlapCircle+LINQ), 공격이 전부 불필요
    //  - GamePlay 씬이 쓰는 Enemy.cs / Swampy.cs를 한 줄도 건드리지 않기 위함
    // 스탯 데이터(EnemySO)만 재사용한다.
    //
    // [하는 일] 뱀서라이크 적 한 마리. 플레이어를 쫓아가고, 맞으면 번쩍이며 밀려나고, 죽으면 코인을 떨군다.
    //          EnemySO.behavior 값에 따라 추격형 / 돌진형 / 원거리형 / 자폭형 중 하나로 움직인다.
    // [붙이는 곳] RL_Goblin, RL_Swampy, RL_OrcShaman 같은 뱀서라이크 적 프리팹의 루트(맨 위 오브젝트).
    // [연결] EnemySpawner 가 ObjectPool.Spawn 으로 꺼낸 뒤 ApplyDifficultyScale 로 "시간이 흐른 만큼 강해진 배율"을 넣어 준다.
    //        무기들은 IDamagable.TakeDamage 로 이 적을 때린다.
    //        죽으면 Coin / HealthPickup 을 떨구고, 처치 수(TotalKills)는 SurvivalHUD / GameOverUI 가 읽는다.
    // [설계] 행동 유형마다 클래스를 따로 만들지 않고, 한 클래스 안에서 behavior 값으로 switch 한다.
    //        새 적은 코드 없이 EnemySO 파일과 프리팹만 만들면 된다.
    // [설계] 오브젝트 풀을 쓴다. 죽어도 파괴하지 않고 창고(ObjectPool)에 넣었다가 다음에 다시 꺼내 쓴다.
    //        Awake/Start 는 처음 한 번만 불리므로 "새로 나온 것처럼" 되돌리는 일은 OnSpawnFromPool 이 맡는다.
    //
    // 파일 안내 (위에서 아래 순서)
    //  1) 인스펙터 설정값과 내부 상태 변수
    //  2) 유니티 생명주기: Awake, Start, FixedUpdate(물리 틱마다 행동 결정)
    //  3) 적끼리 밀어내기
    //  4) 행동 유형: 추격 / 돌진 / 원거리 / 자폭
    //  5) 접촉 데미지(OnTriggerStay2D)
    //  6) 피격(TakeDamage), 멀어진 적 회수, 난이도 배율 적용
    //  7) 타격 연출: 데미지 숫자, 번쩍임, 넉백, 체력바
    //  8) 사망: Die, 사망 연출, Remove
    //  9) IPoolable: 풀에서 꺼낼 때 / 넣을 때
    // 10) 드롭: 회복 아이템, 코인
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class RoguelikeEnemy : MonoBehaviour, IDamagable, IPoolable
    {
        [Header("기본 스탯")]
        // maxHp / moveSpeed / attackDamage / attackCooldown 만 사용
        // chaseDistance, attackDistance는 FSM 전용이라 이 클래스에선 쓰지 않음
        [SerializeField] private EnemySO _enemySO;
        [SerializeField] GameObject _dropCoin;
        [Tooltip("가끔 떨구는 회복 아이템. 확률은 EnemySO.healthDropChance 로 조절")]
        [SerializeField] private GameObject _dropHealth;

        // 아래 설정값 단위: 시간은 초, 거리는 유니티 단위(바닥 타일 한 칸 = 1)
        [Header("타격 연출")]
        [Tooltip("피격 시 번쩍이는 시간")]
        [SerializeField] private float _flashDuration = 0.08f;
        // SpriteRenderer.color 는 곱연산이라 흰색(1,1,1)은 항등원이다.
        // 스프라이트 기본 색이 흰색이므로 "흰색 플래시"는 화면상 아무 변화가 없다.
        // 진짜 흰색 발광을 하려면 전용 셰이더가 필요하므로, 여기서는 붉게 물들이는 방식을 쓴다.
        [SerializeField] private Color _flashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [Tooltip("피격 시 데미지 숫자를 띄운다. 씬에 MMFloatingTextSpawner 가 있어야 보인다")]
        [SerializeField] private bool _showDamageNumber = true;
        [Tooltip("데미지 숫자를 띄우는 최소 간격. 그 사이에 맞은 피해는 합쳐서 한 번에 보여준다")]
        [SerializeField] private float _damageNumberInterval = 0.5f;
        [Tooltip("맞았을 때 뒤로 밀려나는 거리. 적별 저항은 EnemySO.knockbackResist 로 조절")]
        [SerializeField] private float _knockbackDistance = 0.35f;
        [Tooltip("밀려나는 동안 스스로 움직이지 못하는 시간")]
        [SerializeField] private float _knockbackDuration = 0.09f;

        [Header("적끼리 밀어내기")]
        // 적 콜라이더가 전부 트리거라 물리적으로 서로를 밀어내지 못한다.
        // 그대로 두면 수십 마리가 플레이어 좌표 한 점에 완전히 겹쳐 쌓인다.
        [Tooltip("이 거리 안의 다른 적을 밀어낸다. 적 반지름의 약 1.2배가 적당")]
        [SerializeField] private float _separationRadius = 1.1f;
        [Tooltip("밀어내는 세기. 이동속도에 대한 비율. 0이면 밀어내지 않는다")]
        [SerializeField] private float _separationWeight = 3f;
        [Tooltip("몇 초마다 주변을 다시 살피는가. 짧을수록 정확하지만 무겁다")]
        [SerializeField] private float _separationInterval = 0.12f;
        [Tooltip("한 번에 고려할 이웃 수 상한. 수백 마리가 몰려도 비용이 일정하게 유지된다")]
        [SerializeField] private int _separationMaxNeighbors = 8;
        // 밀어낼 상대를 찾을 레이어. 보통 적 레이어 하나만 켠다.
        // Nothing 으로 비워 두면 아무도 안 잡혀서 밀어내기가 꺼진 것과 같다
        [SerializeField] private LayerMask _separationLayer;

        [Header("사망 연출")]
        [Tooltip("죽을 때 부풀었다 쪼그라들며 사라지는 시간. 0이면 즉시 사라진다")]
        [SerializeField] private float _deathPopDuration = 0.14f;
        [Tooltip("코인이 정확히 같은 자리에 겹치지 않도록 흩뿌리는 반경")]
        [SerializeField] private float _coinScatterRadius = 0.25f;

        // 컴포넌트 캐싱
        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        // 추적 대상. 스폰될 때 한 번만 찾고 이후로는 다시 탐색하지 않음
        private Transform _target;

        private float _currHp;
        private bool _isDead;

        // 경과 시간에 따른 난이도 배율. 스포너가 생성 직후에 내려준다
        // 1이면 EnemySO 에 적힌 값 그대로, 2면 두 배다
        private float _hpScale = 1f;
        private float _damageScale = 1f;

        // 풀에서 나온 개체인지. 씬에 직접 배치된 적은 풀이 모르므로 Release 하면 안 된다.
        // (ObjectPool.Release 는 모르는 오브젝트를 경고만 내고 무시해서, 죽어도 안 사라지는
        //  좀비 오브젝트가 된다)
        private bool _fromPool;

        // 행동 유형별 상태
        // "시각"은 Time.time(게임 시작 후 흐른 초) 기준이다. 알람 시계처럼
        // "지금 + 기다릴 시간"을 적어 두고, Time.time 이 그 값을 넘으면 때가 됐다고 본다
        private float _nextChargeTime;      // 다음 돌진 가능 시각
        private float _chargeStateEnd;      // 현재 단계(준비/돌진)가 끝나는 시각
        private bool _isWindingUp;          // 돌진 준비 중(멈춰서 기 모으는 중)
        private bool _isCharging;           // 돌진 중
        // 돌진 방향. 돌진하는 동안에는 바꾸지 않아서 옆으로 피하면 비껴간다
        private Vector2 _chargeDir;
        // 원거리형의 다음 발사 가능 시각
        private float _nextShootTime;
        private bool _isFusing;             // 자폭 준비 중(부풀어오르는 중)
        // 자폭형이 터지는 시각
        private float _fuseEnd;

        // 넉백. 이 시각까지는 스스로 움직이지 않고 밀려나는 속도를 유지한다
        private float _knockbackEnd;

        // 밀어내기. 매 프레임 주변을 검색하면 수백 마리에서 감당이 안 되므로
        // 간격을 두고 갱신하고, 그 사이에는 마지막 결과를 재사용한다
        private Vector2 _separation;
        private float _nextSeparationTime;

        // 검색 결과를 담을 리스트. static 으로 공유해서 매번 새로 만들지 않는다
        // 적 수백 마리가 리스트 하나를 돌려 써도 괜찮다. 유니티 스크립트는 한 번에 한 마리씩
        // 차례로 실행되므로, 한 마리가 쓰는 도중에 다른 적이 끼어들지 않는다
        private static readonly System.Collections.Generic.List<Collider2D> _sepHits =
            new System.Collections.Generic.List<Collider2D>(32);
        // 검색 조건(트리거도 포함할지, 어느 레이어를 볼지). 처음 쓸 때 한 번 만든다
        private ContactFilter2D _sepFilter;
        private bool _sepFilterReady;

        // 이번 판에서 죽인 적의 수. SurvivalHUD 가 판을 시작할 때 0으로 되돌린다.
        // 죽는 곳이 Die() 한 군데뿐이라 여기서 세는 것이 가장 단순하다
        // static 이라서 적마다 따로 있는 게 아니라 RoguelikeEnemy 전체에 딱 하나 있는 숫자다.
        // 그래서 다른 스크립트가 적 하나를 찾지 않고도 RoguelikeEnemy.TotalKills 로 바로 읽는다
        public static int TotalKills;

        // 난이도 배율까지 곱한 실제 값. EnemySO 원본 숫자는 건드리지 않는다
        public float MaxHp { get { return _enemySO.maxHp * _hpScale; } }
        public float ContactDamage { get { return _enemySO.attackDamage * _damageScale; } }

        // 겹쳐 있는 동안 데미지가 매 프레임 들어가지 않도록 하는 쿨타임
        private float _lastContactTime;

        // 데미지 숫자를 합쳐서 보여주기 위한 누적분
        private float _pendingDamage;
        private float _nextDamageNumberTime;

        // 타격 연출용
        private MMHealthBar _healthBar;     // 없으면 체력바를 안 그릴 뿐, 동작에는 지장 없음
        // 프리팹 원래 색과 크기. 번쩍임, 부풀기, 사망 연출이 끝나면 이 값으로 되돌린다
        private Color _baseColor;
        private Vector3 _baseScale;
        // 돌고 있는 번쩍임 코루틴. 또 맞았을 때 이전 것을 멈추려고 기억해 둔다
        private Coroutine _flashRoutine;

        // 애니메이션 해시(RLSwampyAnim: IsWalk(bool), Hit(trigger))
        private static readonly int hashIsWalk = Animator.StringToHash("IsWalk");
        private static readonly int hashHit = Animator.StringToHash("Hit");

        #region 유니티 생명주기
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();

            // 연출 뒤에 되돌아갈 "원래 모습"을 가장 처음에 한 번만 기억해 둔다
            _healthBar = GetComponent<MMHealthBar>();
            _baseColor = _spriteRenderer.color;
            _baseScale = transform.localScale;

            _currHp = MaxHp;
        }

        private void Start()
        {
            AcquireTarget();
        }

        // Start 는 최초 1회뿐이라 풀에서 재사용될 때는 안 불린다.
        // 씬을 다시 시작하면 파괴된 옛 플레이어를 물고 있게 되므로 매번 다시 잡는다.
        private void AcquireTarget()
        {
            GameObject player = GameObject.FindWithTag("PLAYER");
            if (player == null)
            {
                Debug.LogError($"RoguelikeEnemy::AcquireTarget() PLAYER 태그를 가진 오브젝트가 없습니다.");
                return;
            }
            _target = player.transform;

            // 멈추는 상태가 없으므로 계속 걷는 애니메이션
            _animator.SetBool(hashIsWalk, true);
        }

        // linearVelocity를 다루므로 Update가 아니라 FixedUpdate
        private void FixedUpdate()
        {
            // 플레이어가 죽어서 파괴되면 target이 null이 됨
            if (_isDead || _target == null) return;

            // 밀려나는 중에는 아무 행동도 하지 않는다.
            // 여기서 return 하지 않으면 아래 분기가 매 프레임 속도를 덮어써서
            // 넉백이 화면상 전혀 보이지 않는다
            if (Time.time < _knockbackEnd) return;

            // 행동 유형에 맞는 Tick 함수가 이번 틱의 속도(_rb.linearVelocity, 1초에 움직일 양)를 정한다.
            // 속도만 정해 두면 실제로 위치를 옮기는 일은 물리 엔진이 알아서 한다
            switch (_enemySO.behavior)
            {
                case EnemyBehavior.Charger: TickCharger(); break;
                case EnemyBehavior.Ranged:  TickRanged();  break;
                case EnemyBehavior.Bomber:  TickBomber();  break;
                default:                    TickChase();   break;
            }

            // 돌진 중이거나 자폭 준비 중에는 건드리지 않는다.
            // 여기서 밀면 "멎어서 부풀어오른다"는 신호가 흐트러져서 피할 타이밍을 못 읽는다
            if (!_isCharging && !_isFusing) ApplySeparation();
        }

        #region 적끼리 밀어내기
        // 적 콜라이더는 전부 트리거라 물리 엔진이 서로 밀어내 주지 않는다.
        // 그래서 전부 플레이어 좌표 한 점으로 수렴해 실측 거리가 0.00~0.05 였고,
        // 그 결과 반경 1.5 를 도는 공전 칼날이 단 한 마리도 때리지 못했다.
        // (겉보기에도 수십 마리가 한 마리처럼 보인다)
        //
        // FixedUpdate 에서 행동이 정해진 뒤에 불린다.
        // 이미 정한 속도에 "옆에 붙은 적에게서 멀어지는 힘"을 더해 준다
        private void ApplySeparation()
        {
            if (_separationWeight <= 0f || _rb == null) return;

            if (Time.time >= _nextSeparationTime)
            {
                // 같은 프레임에 스폰된 적들이 동시에 검색하지 않도록 간격을 흩뿌린다
                _nextSeparationTime = Time.time + _separationInterval * Random.Range(0.85f, 1.15f);
                _separation = ComputeSeparation();
            }

            if (_separation.sqrMagnitude < 0.0001f) return;

            _rb.linearVelocity += _separation * (_enemySO.moveSpeed * _separationWeight);

            // 밀어내기가 더해져도 원래 속도의 1.5배를 넘지 않게 한다
            float max = _enemySO.moveSpeed * 1.5f;
            if (_rb.linearVelocity.sqrMagnitude > max * max)
                _rb.linearVelocity = _rb.linearVelocity.normalized * max;
        }

        // 반경 안의 이웃들을 찾아서, 각각에게서 멀어지는 방향을 평균 내 돌려준다.
        // 결과 길이는 0~1 사이다. 이웃이 가까울수록, 한쪽으로 몰려 있을수록 1에 가깝다
        private Vector2 ComputeSeparation()
        {
            if (!_sepFilterReady)
            {
                _sepFilter = new ContactFilter2D();
                _sepFilterReady = true;
            }
            // 적 콜라이더가 트리거라서 useTriggers 를 켜지 않으면 아무것도 잡히지 않는다
            _sepFilter.useTriggers = true;
            _sepFilter.useLayerMask = true;
            _sepFilter.SetLayerMask(_separationLayer);
            _sepFilter.useDepth = false;

            _sepHits.Clear();
            Physics2D.OverlapCircle(_rb.position, _separationRadius, _sepFilter, _sepHits);

            Vector2 sum = Vector2.zero;
            int counted = 0;

            for (int i = 0; i < _sepHits.Count && counted < _separationMaxNeighbors; i++)
            {
                Collider2D c = _sepHits[i];
                if (c == null) continue;
                if (c.transform == transform) continue;      // 자기 자신

                Vector2 away = _rb.position - (Vector2)c.transform.position;
                float dist = away.magnitude;

                // 완전히 겹쳐 있으면 밀어낼 방향이 정해지지 않는다.
                // 아무 방향이나 잡아줘야 서로 붙은 채로 굳지 않는다
                if (dist < 0.001f) away = Random.insideUnitCircle.normalized;
                else away /= dist;

                // 가까울수록 세게 민다
                sum += away * (1f - Mathf.Clamp01(dist / _separationRadius));
                counted++;
            }

            if (counted == 0) return Vector2.zero;
            return sum / counted;
        }
        #endregion

        #region 행동 유형
        // 나에게서 플레이어 쪽을 가리키는 길이 1짜리 방향
        private Vector2 DirectionToTarget
        {
            get { return ((Vector2)_target.position - _rb.position).normalized; }
        }

        // 가는 방향에 맞춰 그림을 좌우로 뒤집는다.
        // 바로 위나 아래로 갈 때(x 가 거의 0)는 그대로 둬야 좌우로 깜빡거리지 않는다
        private void FaceTarget(Vector2 dir)
        {
            if (Mathf.Abs(dir.x) > 0.01f) _spriteRenderer.flipX = dir.x < 0f;
        }

        // 기본: 플레이어를 향해 계속 직진
        private void TickChase()
        {
            Vector2 dir = DirectionToTarget;
            FaceTarget(dir);
            _rb.linearVelocity = dir * _enemySO.moveSpeed;
        }

        // 돌진형: 가까워지면 잠깐 멈췄다가(피할 여유) 빠르게 돌진
        // 걷기 -> 준비(_isWindingUp) -> 돌진(_isCharging) -> 다시 걷기를 차례로 돈다.
        // 단계가 셋뿐이라 FSM 없이 bool 두 개와 "끝나는 시각" 하나로 충분하다.
        // 아래 코드는 지금 단계가 뒤쪽인 것부터 검사한다(돌진 중 -> 준비 중 -> 평소)
        private void TickCharger()
        {
            // 1) 돌진 중
            if (_isCharging)
            {
                if (Time.time >= _chargeStateEnd)
                {
                    _isCharging = false;
                    _nextChargeTime = Time.time + _enemySO.chargeCooldown;
                }
                else
                {
                    _rb.linearVelocity = _chargeDir * (_enemySO.moveSpeed * _enemySO.chargeSpeedMul);
                    return;
                }
            }

            // 2) 돌진 준비 중 - 멈춰 서서 방향만 본다
            if (_isWindingUp)
            {
                _rb.linearVelocity = Vector2.zero;
                FaceTarget(DirectionToTarget);

                if (Time.time < _chargeStateEnd) return;

                _isWindingUp = false;
                _isCharging = true;
                _chargeDir = DirectionToTarget;      // 준비가 끝난 시점의 방향으로 고정
                _chargeStateEnd = Time.time + _enemySO.chargeDuration;
                return;
            }

            // 3) 평소에는 천천히 접근하다가 사거리에 들면 준비 시작
            Vector2 dir = DirectionToTarget;
            FaceTarget(dir);
            _rb.linearVelocity = dir * _enemySO.moveSpeed;

            if (Time.time < _nextChargeTime) return;

            float sqr = ((Vector2)_target.position - _rb.position).sqrMagnitude;
            if (sqr > _enemySO.chargeTriggerDistance * _enemySO.chargeTriggerDistance) return;

            _isWindingUp = true;
            _chargeStateEnd = Time.time + _enemySO.chargeWindup;

            // 돌진 준비를 소리로도 알린다.
            // 준비 동작이 짧아서 화면 구석에서 일어나면 눈으로만은 놓치기 쉽다.
            // 이 소리가 "지금 피해라"는 신호가 된다.
            //
            // 간격을 길게 잡은 이유: 후반에는 수십 마리가 동시에 준비 동작에 들어간다.
            // 초당 스무 번씩 울리면 경고가 아니라 소음이 되어 오히려 신호가 죽는다
            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.enemyChargeSFX : null, 0.2f, 0.25f);
        }

        // 원거리형: 선호 거리를 유지하며 주기적으로 발사
        private void TickRanged()
        {
            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            float dist = toTarget.magnitude;
            Vector2 dir = dist > 0.001f ? toTarget / dist : Vector2.right;
            FaceTarget(dir);

            float preferred = _enemySO.preferredDistance;

            // 너무 가까우면 물러나고, 너무 멀면 다가가고, 적당하면 멈춘다
            // 0.8~1.2배의 여유 구간을 두는 이유: 기준이 딱 한 값이면
            // 그 선을 넘었다 말았다 하면서 한 발 앞, 한 발 뒤로 덜덜 떨게 된다
            if (dist < preferred * 0.8f)      _rb.linearVelocity = -dir * _enemySO.moveSpeed;
            else if (dist > preferred * 1.2f) _rb.linearVelocity = dir * _enemySO.moveSpeed;
            else                              _rb.linearVelocity = Vector2.zero;

            if (Time.time < _nextShootTime) return;
            _nextShootTime = Time.time + _enemySO.shootInterval;

            Shoot(dir);
        }

        // 자폭형: 달라붙으면 그 자리에 멎어서 부풀어올랐다가 터진다.
        //
        // 핵심은 "터지기 전에 멎는다"는 것이다. 그 순간이 도망칠 기회이자,
        // 그 전에 죽이면 폭발을 아예 막을 수 있다는 뜻이기도 하다.
        // (죽어도 터지지 않는다. 죽였는데 피해를 입으면 억울하기만 하다)
        private void TickBomber()
        {
            if (_isFusing)
            {
                _rb.linearVelocity = Vector2.zero;

                // 부풀어오르는 연출. 멎어 있는 것만으로는 눈에 안 띈다
                // t 는 준비를 시작한 순간 0, 터지는 순간 1이다. 그만큼 커지고 주황색에 가까워진다
                float t = Mathf.InverseLerp(_fuseEnd - _enemySO.bombFuse, _fuseEnd, Time.time);
                transform.localScale = _baseScale * (1f + 0.35f * t);
                if (_spriteRenderer != null)
                    _spriteRenderer.color = Color.Lerp(_baseColor, new Color(1f, 0.5f, 0.2f, 1f), t);

                if (Time.time < _fuseEnd) return;

                Detonate();
                return;
            }

            Vector2 dir = DirectionToTarget;
            FaceTarget(dir);
            _rb.linearVelocity = dir * _enemySO.moveSpeed;

            float sqr = ((Vector2)_target.position - _rb.position).sqrMagnitude;
            if (sqr > _enemySO.bombTriggerDistance * _enemySO.bombTriggerDistance) return;

            _isFusing = true;
            _fuseEnd = Time.time + _enemySO.bombFuse;
            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.enemyChargeSFX : null, 0.25f, 0.25f);
        }

        // 자폭형의 준비 시간이 다 지나면 TickBomber 가 부른다
        private void Detonate()
        {
            _isFusing = false;

            // 폭발 반경 안에 있을 때만 맞는다. 도망쳤으면 아무 일도 없다
            if (_target != null)
            {
                float dist = Vector2.Distance(_rb.position, (Vector2)_target.position);
                if (dist <= _enemySO.bombRadius)
                {
                    IDamagable player = _target.GetComponent<IDamagable>();
                    if (player != null) player.TakeDamage(_enemySO.bombDamage * _damageScale);
                }
            }

            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.bombExplodeSFX : null, 0.2f, 0.2f);

            // 자기 자신도 사라진다. 죽은 것으로 처리하므로 코인은 떨군다
            _currHp = 0f;
            Die();
        }

        // 원거리형이 발사 주기마다 부른다. 투사체는 풀이 있으면 꺼내 쓰고, 없으면 새로 만든다.
        // Launch 인자: 관통 0 = 처음 닿은 대상에서 사라짐,
        // -90 = 그림 방향을 날아가는 방향에 맞추는 회전 각도(투사체 그림이 위쪽을 보고 그려져 있다고 보고 돌린다)
        private void Shoot(Vector2 dir)
        {
            if (_enemySO.projectilePrefab == null) return;

            GameObject go = ObjectPool.Instance != null
                ? ObjectPool.Instance.Spawn(_enemySO.projectilePrefab, transform.position)
                : Instantiate(_enemySO.projectilePrefab, transform.position, Quaternion.identity);
            if (go == null) return;

            Projectile p = go.GetComponent<Projectile>();
            if (p != null) p.Launch(dir, ContactDamage, _enemySO.projectileSpeed, 0, -90f);
        }
        #endregion
        #endregion

        #region 접촉 데미지
        // Enter만 쓰면 한 번 겹친 뒤 계속 붙어 있어도 더 이상 데미지가 들어가지 않음
        // 몸으로 미는 것이 유일한 공격 수단이므로 Stay + 쿨타임으로 처리
        private void OnTriggerStay2D(Collider2D other)
        {
            if (_isDead) return;
            if (!other.CompareTag("PLAYER")) return;
            if (Time.time < _lastContactTime + _enemySO.attackCooldown) return;

            _lastContactTime = Time.time;
            other.GetComponent<IDamagable>()?.TakeDamage(ContactDamage);
        }
        #endregion

        #region 인터페이스 구현
        public void TakeDamage(float damage)
        {
            // 공전 무기 여러 개가 같은 프레임에 때리면 Die가 두 번 불릴 수 있음
            if (_isDead) return;

            _currHp -= damage;

            // 맞을 때마다: 숫자 모으기 -> 번쩍 -> 뒤로 밀림 -> 체력바 갱신 -> 효과음
            AccumulateDamageNumber(damage);
            Flash();
            Knockback();
            UpdateHealthBar();
            // 초당 수십 번 일어나는 사건이라 간격을 넉넉히 준다. 안 그러면 소리의 벽이 된다
            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.enemyHitSFX : null, 0.08f, 0.15f);

            if (_currHp > 0f)
            {
                _animator.SetTrigger(hashHit);
                return;
            }

            Die();
        }
        #endregion

        // 스포너가 "너무 멀리 뒤처졌다"고 판단했을 때 호출한다.
        // 죽은 것이 아니므로 코인도 안 떨구고 처치 수도 안 센다. 조용히 창고로 돌아간다.
        // 씬에 직접 배치된 적은 창고가 모르는 개체라 손대지 않는다
        public void DespawnFarAway()
        {
            if (_isDead || !_fromPool) return;
            if (ObjectPool.Instance == null) return;

            ObjectPool.Instance.Release(gameObject);
        }

        // 스포너가 Instantiate 직후에 호출한다.
        // 이 시점엔 Awake 가 이미 끝나 _currHp 가 세팅되어 있으므로 다시 계산해 준다.
        // 풀에서 꺼낸 경우도 같다. OnSpawnFromPool 이 먼저 체력을 채우고, 여기서 배율을 곱해 다시 채운다.
        // 0.1배 아래로는 못 내려가게 막아서, 실수로 0이 들어와도 "체력 0인 적"이 생기지 않는다
        public void ApplyDifficultyScale(float hpScale, float damageScale)
        {
            _hpScale = Mathf.Max(0.1f, hpScale);
            _damageScale = Mathf.Max(0.1f, damageScale);

            _currHp = MaxHp;
        }

        #region 타격 연출
        // 타격마다 숫자를 띄우면 화면이 숫자로 뒤덮인다.
        //
        // 실측: 최대 난이도에서 화면에 숫자가 186개까지 떴다.
        // 그 정도면 서로 겹쳐서 아무것도 못 읽고, 숫자 오브젝트도 준비해둔 양을 넘어
        // 게임 도중에 계속 새로 만들어진다(60개 준비 -> 261개까지 증식).
        //
        // 그래서 짧은 간격 동안 맞은 피해를 합쳐서 한 번에 보여준다.
        // 숫자가 줄어들 뿐 아니라, 합계라서 한 번에 얼마나 들어갔는지가 오히려 잘 보인다.
        private void AccumulateDamageNumber(float damage)
        {
            if (!_showDamageNumber) return;

            _pendingDamage += damage;

            if (Time.time < _nextDamageNumberTime) return;
            _nextDamageNumberTime = Time.time + _damageNumberInterval;

            FlushDamageNumber();
        }

        // 쌓아둔 피해를 지금 바로 띄운다. 죽는 순간에도 불러서 마지막 타격을 놓치지 않게 한다
        private void FlushDamageNumber()
        {
            if (_pendingDamage <= 0f) return;

            float amount = _pendingDamage;
            _pendingDamage = 0f;
            ShowDamageNumber(amount);
        }

        // 적 머리 위에 숫자를 띄워 달라고 "이벤트"를 보낸다.
        // 이벤트는 방송과 같다. 적은 누가 듣는지 모르고 외치기만 하고, 듣고 있던 쪽이 숫자를 만든다
        private void ShowDamageNumber(float damage)
        {
            if (!_showDamageNumber) return;

            // 씬에 MMFloatingTextSpawner 가 없으면 아무도 이 이벤트를 듣지 않아서 조용히 무시된다
            MMFloatingTextSpawnEvent.Trigger(
                new MMChannelData(MMChannelModes.Int, 0, null),
                transform.position,
                Mathf.Max(1, Mathf.RoundToInt(damage)).ToString(),
                Vector3.up,
                1f);
        }

        // 연속으로 맞으면 코루틴이 겹쳐서 원래 색으로 못 돌아오므로 항상 이전 것을 멈춘다
        private void Flash()
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashCo());
        }

        // 붉게 물들임 -> _flashDuration 초 기다림 -> 원래 색으로
        private IEnumerator FlashCo()
        {
            _spriteRenderer.color = _flashColor;
            yield return new WaitForSeconds(_flashDuration);
            _spriteRenderer.color = _baseColor;
            _flashRoutine = null;
        }

        // 데미지를 준 쪽의 위치를 인자로 받지 않는다.
        // IDamagable.TakeDamage(float) 는 구 GamePlay 씬의 Enemy 도 구현하는 인터페이스라
        // 시그니처를 바꿀 수 없기 때문이다.
        // 대신 "플레이어 반대 방향"으로 민다. 피해가 사실상 전부 플레이어에게서 오므로
        // (공전 칼날/오라는 플레이어 몸에서, 단검/도끼도 플레이어가 던진다) 결과가 거의 같다.
        private void Knockback()
        {
            if (_knockbackDistance <= 0f || _rb == null || _target == null) return;

            float power = _knockbackDistance * (1f - Mathf.Clamp01(_enemySO.knockbackResist));
            if (power <= 0.001f) return;

            Vector2 away = _rb.position - (Vector2)_target.position;
            if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle.normalized;

            // 거리 / 시간 = 속도. 이 속도를 넉백 시간 동안 유지하면 딱 그 거리만큼 밀린다
            _rb.linearVelocity = away.normalized * (power / Mathf.Max(0.01f, _knockbackDuration));
            _knockbackEnd = Time.time + _knockbackDuration;
        }

        // 머리 위 체력바(MMHealthBar)에 지금 체력 / 최소 0 / 최대 체력을 넘겨 다시 그리게 한다
        private void UpdateHealthBar()
        {
            if (_healthBar == null) return;
            _healthBar.UpdateBar(Mathf.Max(0f, _currHp), 0f, MaxHp, true);
        }
        #endregion

        // 체력이 0 이하가 되면 TakeDamage 가, 자폭하면 Detonate 가 부른다.
        // 처치 수 +1 -> 마지막 숫자 -> 소리 -> 코인/회복 드롭 -> 사라지는 연출 순서로 진행한다
        private void Die()
        {
            _isDead = true;
            _currHp = 0f;
            // 모든 적이 함께 쓰는 static 카운터를 1 올린다
            TotalKills++;

            // 마지막 타격은 반드시 보여준다. 죽인 한 방이 안 보이면 허전하다
            FlushDamageNumber();

            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.enemyDeathSFX : null, 0.08f, 0.18f);
            DropCoin();
            DropHealth();

            // _isDead 가 true 라 이 시점부터는 움직이지도, 접촉 피해를 주지도 않는다.
            // 그래서 연출 때문에 잠깐 남아 있어도 플레이어에게 불리하지 않다
            if (_rb != null) _rb.linearVelocity = Vector2.zero;

            // 사망 애니메이션이 없어서 그냥 사라지면 "때린 건지 사라진 건지" 알기 어렵다.
            // 살짝 부풀었다가 쪼그라들며 투명해지는 짧은 연출을 넣는다
            if (_deathPopDuration > 0f && gameObject.activeInHierarchy) StartCoroutine(DeathPopCo());
            else Remove();
        }

        // Die 에서 시작하는 코루틴. 매 프레임 크기와 투명도를 조금씩 바꾸고, 다 끝나면 Remove 로 치운다.
        // "yield return null" 은 "여기서 멈췄다가 다음 프레임에 이어서 하기"라는 뜻이다
        private IEnumerator DeathPopCo()
        {
            // 체력바가 0인 채로 같이 쪼그라들면 지저분하므로 먼저 끈다
            if (_healthBar != null) _healthBar.enabled = false;

            float elapsed = 0f;
            while (elapsed < _deathPopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _deathPopDuration);

                // 앞 30% 구간에서 1.25배까지 부풀고, 나머지 구간에서 0까지 줄어든다
                float scale = t < 0.3f
                    ? Mathf.Lerp(1f, 1.25f, t / 0.3f)
                    : Mathf.Lerp(1.25f, 0f, (t - 0.3f) / 0.7f);

                transform.localScale = _baseScale * scale;

                if (_spriteRenderer != null)
                {
                    Color c = _spriteRenderer.color;
                    c.a = 1f - t;
                    _spriteRenderer.color = c;
                }

                yield return null;
            }

            Remove();
        }

        // 풀 출신이면 반납, 아니면 파괴.
        // 씬에 직접 배치된 적을 Release 하면 풀이 모르는 오브젝트라 경고만 내고 무시해서
        // 죽었는데 화면에 그대로 남는다
        // 치우기 전에 크기, 색, 체력바를 원래대로 돌린다. 창고에 들어간 모습 그대로 다음에 다시 나오기 때문이다
        private void Remove()
        {
            transform.localScale = _baseScale;
            if (_spriteRenderer != null) _spriteRenderer.color = _baseColor;
            if (_healthBar != null) _healthBar.enabled = true;

            if (_fromPool && ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
            else Destroy(gameObject);
        }

        #region IPoolable
        // Awake/Start 는 최초 1회뿐이므로 재사용 시 초기화는 전부 여기서 한다.
        // 하나라도 빠뜨리면 "죽은 채로 스폰되는 적", "체력이 깎인 채 나오는 적",
        // "스폰하자마자 피격 애니메이션을 재생하는 적" 같은 버그가 된다.
        // ObjectPool.Spawn 이 오브젝트를 켠(SetActive(true)) 직후에 부른다. 처음 만들어진 개체도 똑같이 불린다.
        // 난이도 배율(_hpScale, _damageScale)은 여기서 되돌리지 않는다. 바로 뒤에 스포너가 ApplyDifficultyScale 로 새로 넣는다
        public void OnSpawnFromPool()
        {
            _fromPool = true;
            _isDead = false;
            _currHp = MaxHp;
            _lastContactTime = 0f;

            // 행동 상태도 반드시 되돌린다.
            // 안 그러면 돌진 도중에 죽은 적이 다음 스폰 때 돌진 상태로 튀어나온다
            _isWindingUp = false;
            _isCharging = false;
            _nextChargeTime = 0f;
            _chargeStateEnd = 0f;
            _nextShootTime = 0f;
            _knockbackEnd = 0f;
            _pendingDamage = 0f;
            _nextDamageNumberTime = 0f;
            _isFusing = false;
            _fuseEnd = 0f;
            _separation = Vector2.zero;
            _nextSeparationTime = 0f;

            // 사망 연출로 크기가 0까지 줄고 투명해진 채로 반납됐을 수 있다.
            // 이걸 되돌리지 않으면 다음에 "보이지 않는 적"이 스폰된다
            transform.localScale = _baseScale;
            if (_healthBar != null) _healthBar.enabled = true;

            if (_rb != null) _rb.linearVelocity = Vector2.zero;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;   // 붉게 물든 채로 반납됐을 수 있다
                _spriteRenderer.enabled = true;
            }

            if (_animator != null)
            {
                // 래치된 트리거가 남아 있으면 스폰 즉시 피격 모션이 재생된다
                _animator.ResetTrigger(hashHit);
                _animator.SetBool(hashIsWalk, true);
            }

            AcquireTarget();
        }

        // ObjectPool.Release 가 오브젝트를 끄기 직전에 부른다.
        // 꺼지면 코루틴은 저절로 멈추지만 색은 붉은 채로 남으므로, 여기서 멈추고 모습을 돌려 둔다
        public void OnReturnToPool()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            if (_spriteRenderer != null) _spriteRenderer.color = _baseColor;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            transform.localScale = _baseScale;
        }
        #endregion

        // 가끔만 나온다. 항상 나오면 체력이 자원이 아니게 되고,
        // 아예 안 나오면 한 번 깎인 체력을 되돌릴 방법이 사실상 없다
        // Die 에서 부른다. Random.value 는 0~1 사이 무작위 수라서, 확률이 0.1 이면 열 번에 한 번꼴로 떨군다
        private void DropHealth()
        {
            if (_dropHealth == null) return;
            if (_enemySO.healthDropChance <= 0f) return;
            if (Random.value > _enemySO.healthDropChance) return;

            if (ObjectPool.Instance != null) ObjectPool.Instance.Spawn(_dropHealth, transform.position);
            else Instantiate(_dropHealth, transform.position, Quaternion.identity);
        }

        private void DropCoin()
        {
            if (_dropCoin == null) return;

            // 부모를 지정하지 않는 것이 중요함
            // Instantiate(_dropCoin, transform)처럼 자신을 부모로 주면
            // 바로 아래 Destroy(gameObject)에서 코인까지 같이 사라짐
            // 강한 적일수록 여러 개를 떨군다. 강적을 노릴 이유를 만든다
            int count = Mathf.Max(1, _enemySO.coinDrop);

            for (int i = 0; i < count; i++)
            {
                // 적이 뭉쳐서 죽으면 코인이 완전히 겹쳐 한 개처럼 보인다. 조금씩 흩뿌린다.
                // 여러 개를 떨굴 때는 반경을 조금 넓혀야 뭉쳐 보이지 않는다
                float radius = _coinScatterRadius * (count > 1 ? 2f : 1f);
                Vector3 offset = radius > 0f
                    ? (Vector3)(Random.insideUnitCircle * radius)
                    : Vector3.zero;
                Vector3 spot = transform.position + offset;

                if (ObjectPool.Instance != null) ObjectPool.Instance.Spawn(_dropCoin, spot);
                else Instantiate(_dropCoin, spot, Quaternion.identity);
            }
        }
    }
}
