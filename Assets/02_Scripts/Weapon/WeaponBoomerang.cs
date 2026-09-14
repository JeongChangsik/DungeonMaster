using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 부메랑 도끼. WeaponRoot 아래 아무 빈 오브젝트에 부착.
    // 가장 가까운 적 방향으로 도끼를 던지면 일정 거리까지 나갔다가 돌아온다.
    // 왕복하면서 각각 한 번씩 때리므로 한 발당 최대 2번 타격한다.
    //
    // [하는 일] 몇 초마다 가까운 적을 찾아 그쪽으로 도끼를 던진다. 여러 발이면 부채꼴로 벌려 던진다.
    //          이 스크립트는 "언제, 어느 방향으로, 몇 개" 던질지만 정한다.
    //          날아가고 돌아오는 움직임은 도끼 프리팹(Axe)에 붙은 BoomerangProjectile.cs 가 한다.
    // [붙이는 곳] RL_Warrior 프리팹의 WeaponRoot 아래 BoomerangMuzzle 오브젝트.
    // [연결] WeaponUpgradeSO(부메랑 카드)가 Unlock() 과 AddStat() 을 부른다.
    //          TargetFinder 로 가장 가까운 적을 찾고, ObjectPool 에서 도끼를 꺼내
    //          Configure() -> Launch() 순서로 넘긴다.
    // [설계] "적 한 명 = 한 번"이 아니라 "갈 때 한 번, 올 때 한 번"이라,
    //          적이 몰려오는 쪽으로 던지면 같은 무리를 두 번 쓸어버린다. 투척 단검과의 차이점이다.
    //          도끼가 주인(이 오브젝트의 transform)을 따라 돌아오므로, 플레이어가 움직여도 손으로 돌아온다.
    public class WeaponBoomerang : WeaponBase
    {
        [Header("발사 설정")]
        // 도끼 프리팹. 루트에 BoomerangProjectile 이 있어야 한다
        [SerializeField] private GameObject _projectilePrefab;
        // 던지는 간격(초)
        [SerializeField] private float _interval = 2.0f;
        [Min(1)] [SerializeField] private int _count = 1;
        [Tooltip("여러 발일 때 벌어지는 각도")]
        [SerializeField] private float _spreadAngle = 40f;
        // 도끼가 날아가는 속도(초당 유니티 단위). 나갈 때와 돌아올 때 같은 속도다
        [SerializeField] private float _projectileSpeed = 7f;
        [Tooltip("이 거리까지 나갔다가 돌아온다. 적 탐색 반경도 겸한다")]
        [SerializeField] private float _range = 5f;
        [Tooltip("나가는 길에 몇 명을 뚫는가")]
        [Min(0)] [SerializeField] private int _pierce = 2;
        // 적 탐색에 쓸 레이어. 적이 있는 레이어를 골라야 한다
        [SerializeField] private LayerMask _enemyLayer;
        // 도끼 그림이 오른쪽이 아닌 다른 쪽을 보고 그려졌을 때 맞추는 각도(도).
        // 부메랑은 날아가며 계속 돌기 때문에 사실상 첫 순간에만 의미가 있다
        [SerializeField] private float _angleOffset = 0f;

        // 강화 누적분.
        // 인스펙터 값(기본값)은 그대로 두고 카드로 오른 만큼만 따로 쌓는다.
        // add 는 더하는 양(0 = 강화 없음), mul 은 곱하는 배율(1 = 그대로)
        private int _addCount;
        private float _addRange;
        private float _mulRange = 1f;
        private float _mulRate = 1f;

        // 마지막으로 던진 시각(초, Time.time 기준)
        private float _lastFire;

        // 카드 SO 가 "부메랑 무기"를 찾을 때 이 값으로 알아본다
        public override WeaponId Id { get { return WeaponId.Boomerang; } }

        // 아래 세 값은 "기본값 + 강화분 + 전역 배율"을 합친 최종값이다.
        // 던지는 간격(초). 속도 배율이 커질수록 짧아지고, 0.1초보다 짧아지지는 않는다
        public float Interval { get { return Mathf.Max(0.1f, _interval / (_mulRate * GlobalRateMul)); } }
        public int Count { get { return Mathf.Max(1, _count + _addCount); } }
        // 도끼가 나가는 최대 거리(유니티 단위). "모든 무기 범위" 카드도 여기에 곱해진다
        public float Range { get { return (_range + _addRange) * _mulRange * GlobalAreaMul; } }

        // 매 프레임 "던질 시간이 됐고, 던질 적이 있나"를 확인한다.
        // Time.time 은 timeScale 을 따르므로 레벨업/일시정지로 시간이 멈추면 던지기도 멈춘다
        private void Update()
        {
            // 아직 해금 카드를 안 먹었으면 아무것도 하지 않는다
            if (!IsUnlocked) return;
            if (Time.time < _lastFire + Interval) return;

            // 사거리보다 조금 넓게 찾는다. 던지고 나서 적이 다가오는 경우가 많기 때문
            Transform target = TargetFinder.FindNearest(transform.position, Range * 1.5f, _enemyLayer);
            // 적이 없으면 허공에 던지지 않는다. _lastFire 를 갱신하지 않았으니
            // 적이 나타나는 즉시(쿨다운을 다시 기다리지 않고) 바로 던진다
            if (target == null) return;

            _lastFire = Time.time;
            Fire(((Vector2)(target.position - transform.position)).normalized);
        }

        // 도끼를 Count 개 꺼내서 baseDirection(적 쪽) 을 가운데로 부채꼴로 벌려 던진다
        private void Fire(Vector2 baseDirection)
        {
            if (_projectilePrefab == null)
            {
                Debug.LogError($"WeaponBoomerang::Fire() _projectilePrefab이 비어 있습니다.");
                return;
            }

            int count = Count;
            float damage = Damage;
            float range = Range;

            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.weaponThrowSFX : null, 0.15f, 0.12f);

            // 부채꼴이 적 방향을 기준으로 좌우 대칭이 되게 시작 각도를 정한다.
            // 예: 3발, 40도면 -40, 0, +40 도. 1발이면 0 도 하나라 정확히 적 쪽으로 간다
            float start = -(count - 1) * 0.5f * _spreadAngle;

            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Rotate(baseDirection, start + _spreadAngle * i);

                // 풀에서 도끼를 꺼낸다(매번 새로 만들고 부수면 버벅이므로). 풀이 없을 때만 새로 만든다
                GameObject go = ObjectPool.Instance != null
                    ? ObjectPool.Instance.Spawn(_projectilePrefab, transform.position)
                    : Instantiate(_projectilePrefab, transform.position, Quaternion.identity);

                if (go == null) continue;

                BoomerangProjectile b = go.GetComponent<BoomerangProjectile>();
                if (b == null) continue;

                // 왕복 거리와 돌아올 대상을 먼저 알려주고 발사해야 한다.
                // 돌아올 대상은 플레이어 자식인 이 오브젝트라서, 플레이어가 움직여도 도끼가 따라온다
                b.Configure(transform, range);
                b.Launch(dir, damage, _projectileSpeed, _pierce, _angleOffset);
            }
        }

        // 방향 v 를 degrees 도만큼 돌린 방향을 돌려준다(양수 = 반시계 방향).
        // 수학 시간의 회전 공식을 그대로 쓴 것이다
        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        // 강화 카드가 부르는 입구. 개수/사거리/속도는 여기서 받고,
        // 피해량(Damage)은 모든 무기가 같으므로 맨 아래 base.AddStat 에서 부모가 처리한다.
        // Pierce(관통)는 여기서 받지 않으므로, 부메랑 관통 카드를 만들어도 지금은 효과가 없다
        public override void AddStat(WeaponStat stat, float flat, float percent)
        {
            switch (stat)
            {
                case WeaponStat.Count:
                    _addCount += Mathf.RoundToInt(flat);
                    break;

                case WeaponStat.Range:
                    _addRange += flat;
                    _mulRange += percent;
                    break;

                case WeaponStat.Rate:
                    _mulRate += percent;
                    break;
            }

            base.AddStat(stat, flat, percent);
        }

        // 에디터에서 선택하면 씬 뷰에 사거리를 원으로 그려준다(게임 화면에는 안 보인다).
        // 플레이 중에는 강화가 반영된 값, 편집 중에는 인스펙터 기본값을 보여준다
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? Range : _range);
        }
    }
}
