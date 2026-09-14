using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 투척 단검.
    //
    // [하는 일] 일정 주기로 가장 가까운 적을 향해 단검을 던진다.
    //           강화하면 한 번에 여러 자루를 부채꼴로 던지고, 적을 뚫고 지나가기도 한다.
    // [붙이는 곳] 플레이어의 WeaponRoot/ProjectileMuzzle 에 부착.
    //           단검은 이 오브젝트 위치에서 튀어나간다(Muzzle = 총구).
    //           _projectilePrefab 에는 Projectile 컴포넌트가 붙은 단검 프리팹을 넣는다.
    // [연결] TargetFinder.FindNearest 로 대상을 찾고, ObjectPool 에서 단검을 꺼내 Projectile.Launch 로 날린다.
    //        실제로 적에게 맞는 판정은 Projectile 이 한다. 이 스크립트는 "언제, 어느 쪽으로, 몇 개"만 정한다.
    //        WeaponUpgradeSO 가 Unlock() / AddStat() 을 불러 열고 강화한다.
    // [설계] 단검은 Instantiate/Destroy 대신 오브젝트 풀에서 빌려 쓰고 돌려준다.
    //        초당 수십 개씩 만들고 부수면 GC 때문에 게임이 끊기기 때문이다.
    //        (오브젝트 풀 = 미리 만들어 둔 물건 창고. 꺼내 쓰고 끝나면 꺼서 다시 넣어 둔다.)
    public class WeaponProjectile : WeaponBase
    {
        [Header("발사 설정")]
        [SerializeField] private GameObject _projectilePrefab;
        [Tooltip("발사 주기(초). 짧을수록 빠르다")]
        [SerializeField] private float _interval = 1.2f;
        [Tooltip("한 번에 몇 발")]
        [Min(1)] [SerializeField] private int _count = 1;
        [Tooltip("여러 발일 때 부채꼴로 벌어지는 각도")]
        [SerializeField] private float _spreadAngle = 12f;
        [SerializeField] private float _projectileSpeed = 9f;   // 초당 이동 거리(유닛)
        [Tooltip("적을 찾는 반경 = 사거리")]
        [SerializeField] private float _range = 9f;
        [Tooltip("적을 몇 명 더 뚫고 지나가는가. 0이면 하나 맞히고 사라짐")]
        [Min(0)] [SerializeField] private int _pierce = 0;
        [SerializeField] private LayerMask _enemyLayer;

        [Header("스프라이트 방향 보정")]
        [Tooltip("단검이 오른쪽(+X)을 향해 그려졌으면 0, 위(+Y)를 향했으면 -90")]
        [SerializeField] private float _angleOffset = -90f;

        // 강화 누적분
        private int _addCount;
        private int _addPierce;
        private float _addRange;
        private float _mulRange = 1f;
        private float _mulRate = 1f;    // 클수록 발사 간격이 짧아진다

        // 마지막으로 던진 시각(Time.time 기준, 초)
        private float _lastFire;

        public override WeaponId Id { get { return WeaponId.Projectile; } }

        // 속도 배율로 나눠서 간격을 줄인다. 0.05초 아래로는 내려가지 않게 막았다
        public float Interval { get { return Mathf.Max(0.05f, _interval / (_mulRate * GlobalRateMul)); } }
        public int Count { get { return Mathf.Max(1, _count + _addCount); } }
        public float Range { get { return (_range + _addRange) * _mulRange * GlobalAreaMul; } }
        public int Pierce { get { return Mathf.Max(0, _pierce + _addPierce); } }

        // 매 프레임 불린다. 쿨다운이 끝났고 사거리 안에 적이 있으면 던진다.
        // Time.time 은 timeScale 이 0이면 멈추므로, 카드 선택이나 일시정지 중에는 던지지 않는다.
        //
        // Rebuild 를 override 하지 않는 이유: Damage, Count 같은 값을 던지는 순간마다 새로 읽기 때문에
        // 강화나 전역 배율이 바뀌어도 다음 발사부터 자동으로 반영된다.
        private void Update()
        {
            // 해금 전에는 아무것도 하지 않는다
            if (!IsUnlocked) return;
            if (Time.time < _lastFire + Interval) return;

            Transform target = TargetFinder.FindNearest(transform.position, Range, _enemyLayer);

            // 사거리 안에 적이 없으면 쿨다운을 소모하지 않는다.
            // 이렇게 해야 적이 나타나는 순간 바로 던진다.
            if (target == null) return;

            _lastFire = Time.time;
            Fire(((Vector2)(target.position - transform.position)).normalized);
        }

        // baseDirection(가장 가까운 적 쪽) 을 가운데로 두고 Count 발을 부채꼴로 던진다.
        private void Fire(Vector2 baseDirection)
        {
            if (_projectilePrefab == null)
            {
                Debug.LogError($"WeaponProjectile::Fire() _projectilePrefab이 비어 있습니다.");
                return;
            }

            // 이번 발사에 쓸 값을 미리 한 번만 계산해 둔다. 반복문 안에서 매번 계산할 필요가 없다
            int count = Count;
            float damage = Damage;
            int pierce = Pierce;

            // 연사 강화를 먹으면 초당 수십 발이라 간격을 준다
            // 첫 번째 0.12 = 음 높이를 살짝 흔드는 정도(매번 똑같은 소리로 들리지 않게),
            // 두 번째 0.12 = 최소 간격(초). 0.12초 안에 또 부르면 소리를 건너뛴다. 발사는 그대로 하고 소리만 줄인다
            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.weaponThrowSFX : null, 0.12f, 0.12f);

            // 여러 발이면 부채꼴로 벌린다. 3발이면 -spread, 0, +spread
            // 짝수 발이면 가운데를 비워서 적의 양옆으로 벌어진다(2발이면 -spread/2, +spread/2)
            float start = -(count - 1) * 0.5f * _spreadAngle;

            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Rotate(baseDirection, start + _spreadAngle * i);

                // 풀이 씬에 없으면(테스트 씬 등) 그냥 새로 만든다. 게임이 멈추지 않게 하는 대비책
                GameObject go = ObjectPool.Instance != null
                    ? ObjectPool.Instance.Spawn(_projectilePrefab, transform.position)
                    : Instantiate(_projectilePrefab, transform.position, Quaternion.identity);

                if (go == null) continue;

                // 풀에서 꺼낸 단검은 지난번 상태가 남아 있을 수 있어서, Launch 가 피해량/속도/방향을 전부 새로 넣어 준다
                Projectile p = go.GetComponent<Projectile>();
                if (p != null) p.Launch(dir, damage, _projectileSpeed, pierce, _angleOffset);
            }
        }

        // 2D 벡터를 degrees 도만큼 반시계로 돌린다(회전 공식). 부채꼴 방향을 만들 때 쓴다
        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        // 투척 단검 전용 스탯(개수, 사거리, 연사, 관통)을 쌓는다.
        // 개수/관통은 정수라서 flat 을 반올림해서 더한다(카드에 1 을 넣으면 1 개)
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

                case WeaponStat.Pierce:
                    _addPierce += Mathf.RoundToInt(flat);
                    break;
            }

            // Damage 처리는 부모가 한다
            base.AddStat(stat, flat, percent);
        }

        // 에디터 씬 뷰에서 선택했을 때 사거리를 하늘색 원으로 보여준다. 게임 화면에는 안 나온다
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? Range : _range);
        }
    }
}
