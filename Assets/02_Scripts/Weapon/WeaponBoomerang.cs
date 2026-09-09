using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 부메랑 도끼. WeaponRoot 아래 아무 빈 오브젝트에 부착.
    // 가장 가까운 적 방향으로 도끼를 던지면 일정 거리까지 나갔다가 돌아온다.
    // 왕복하면서 각각 한 번씩 때리므로 한 발당 최대 2번 타격한다.
    public class WeaponBoomerang : WeaponBase
    {
        [Header("발사 설정")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private float _interval = 2.0f;
        [Min(1)] [SerializeField] private int _count = 1;
        [Tooltip("여러 발일 때 벌어지는 각도")]
        [SerializeField] private float _spreadAngle = 40f;
        [SerializeField] private float _projectileSpeed = 7f;
        [Tooltip("이 거리까지 나갔다가 돌아온다. 적 탐색 반경도 겸한다")]
        [SerializeField] private float _range = 5f;
        [Tooltip("나가는 길에 몇 명을 뚫는가")]
        [Min(0)] [SerializeField] private int _pierce = 2;
        [SerializeField] private LayerMask _enemyLayer;
        [SerializeField] private float _angleOffset = 0f;

        // 강화 누적분
        private int _addCount;
        private float _addRange;
        private float _mulRange = 1f;
        private float _mulRate = 1f;

        private float _lastFire;

        public override WeaponId Id { get { return WeaponId.Boomerang; } }

        public float Interval { get { return Mathf.Max(0.1f, _interval / (_mulRate * GlobalRateMul)); } }
        public int Count { get { return Mathf.Max(1, _count + _addCount); } }
        public float Range { get { return (_range + _addRange) * _mulRange * GlobalAreaMul; } }

        private void Update()
        {
            if (!IsUnlocked) return;
            if (Time.time < _lastFire + Interval) return;

            // 사거리보다 조금 넓게 찾는다. 던지고 나서 적이 다가오는 경우가 많기 때문
            Transform target = TargetFinder.FindNearest(transform.position, Range * 1.5f, _enemyLayer);
            if (target == null) return;

            _lastFire = Time.time;
            Fire(((Vector2)(target.position - transform.position)).normalized);
        }

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

            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.weaponThrowSFX : null, 0.15f);

            float start = -(count - 1) * 0.5f * _spreadAngle;

            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Rotate(baseDirection, start + _spreadAngle * i);

                GameObject go = ObjectPool.Instance != null
                    ? ObjectPool.Instance.Spawn(_projectilePrefab, transform.position)
                    : Instantiate(_projectilePrefab, transform.position, Quaternion.identity);

                if (go == null) continue;

                BoomerangProjectile b = go.GetComponent<BoomerangProjectile>();
                if (b == null) continue;

                // 왕복 거리와 돌아올 대상을 먼저 알려주고 발사해야 한다
                b.Configure(transform, range);
                b.Launch(dir, damage, _projectileSpeed, _pierce, _angleOffset);
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
            }

            base.AddStat(stat, flat, percent);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? Range : _range);
        }
    }
}
