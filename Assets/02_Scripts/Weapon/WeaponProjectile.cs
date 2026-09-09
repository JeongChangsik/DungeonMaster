using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 투척 단검. 플레이어의 WeaponRoot/ProjectileMuzzle 에 부착.
    // 일정 주기로 가장 가까운 적을 향해 단검을 던진다.
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
        [SerializeField] private float _projectileSpeed = 9f;
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
        private float _mulRate = 1f;

        private float _lastFire;

        public override WeaponId Id { get { return WeaponId.Projectile; } }

        public float Interval { get { return Mathf.Max(0.05f, _interval / (_mulRate * GlobalRateMul)); } }
        public int Count { get { return Mathf.Max(1, _count + _addCount); } }
        public float Range { get { return (_range + _addRange) * _mulRange * GlobalAreaMul; } }
        public int Pierce { get { return Mathf.Max(0, _pierce + _addPierce); } }

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

        private void Fire(Vector2 baseDirection)
        {
            if (_projectilePrefab == null)
            {
                Debug.LogError($"WeaponProjectile::Fire() _projectilePrefab이 비어 있습니다.");
                return;
            }

            int count = Count;
            float damage = Damage;
            int pierce = Pierce;

            // 여러 발이면 부채꼴로 벌린다. 3발이면 -spread, 0, +spread
            float start = -(count - 1) * 0.5f * _spreadAngle;

            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Rotate(baseDirection, start + _spreadAngle * i);

                GameObject go = ObjectPool.Instance != null
                    ? ObjectPool.Instance.Spawn(_projectilePrefab, transform.position)
                    : Instantiate(_projectilePrefab, transform.position, Quaternion.identity);

                if (go == null) continue;

                Projectile p = go.GetComponent<Projectile>();
                if (p != null) p.Launch(dir, damage, _projectileSpeed, pierce, _angleOffset);
            }
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? Range : _range);
        }
    }
}
