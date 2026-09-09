using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 폭탄 투하. WeaponRoot 아래 빈 오브젝트에 부착.
    // 일정 주기로 플레이어가 서 있는 자리에 폭탄을 놓는다.
    //
    // 조준하지 않는 것이 이 무기의 성격이다.
    // 다른 무기가 "적을 찾아 때리는" 것과 달리, 폭탄은 "내가 지나온 길"에 남는다.
    // 그래서 적을 몰고 다니다가 방향을 트는 플레이가 강해진다.
    public class WeaponBomb : WeaponBase
    {
        [Header("투하 설정")]
        [SerializeField] private GameObject _bombPrefab;
        [Tooltip("몇 초마다 놓는가")]
        [SerializeField] private float _interval = 3f;
        [Tooltip("한 번에 몇 개")]
        [Min(1)] [SerializeField] private int _count = 1;
        [Tooltip("여러 개일 때 흩어놓는 반경")]
        [SerializeField] private float _scatter = 1.2f;
        [Tooltip("놓인 뒤 터질 때까지")]
        [SerializeField] private float _fuse = 1.2f;
        [Tooltip("폭발 반경")]
        [SerializeField] private float _radius = 2.5f;

        // 강화 누적분
        private int _addCount;
        private float _addRadius;
        private float _mulRadius = 1f;
        private float _mulRate = 1f;

        private float _lastDrop;

        public override WeaponId Id { get { return WeaponId.Bomb; } }

        public float Interval { get { return Mathf.Max(0.2f, _interval / (_mulRate * GlobalRateMul)); } }
        public int Count { get { return Mathf.Max(1, _count + _addCount); } }
        public float Radius { get { return (_radius + _addRadius) * _mulRadius * GlobalAreaMul; } }

        private void Update()
        {
            if (!IsUnlocked) return;
            if (Time.time < _lastDrop + Interval) return;

            _lastDrop = Time.time;
            Drop();
        }

        private void Drop()
        {
            if (_bombPrefab == null)
            {
                Debug.LogError($"WeaponBomb::Drop() _bombPrefab이 비어 있습니다.");
                return;
            }

            int count = Count;
            float damage = Damage;
            float radius = Radius;

            for (int i = 0; i < count; i++)
            {
                // 한 개일 때는 발밑에 정확히, 여러 개일 때만 흩어놓는다
                Vector3 offset = count > 1
                    ? (Vector3)(Random.insideUnitCircle * _scatter)
                    : Vector3.zero;
                Vector3 spot = transform.position + offset;

                GameObject go = ObjectPool.Instance != null
                    ? ObjectPool.Instance.Spawn(_bombPrefab, spot)
                    : Instantiate(_bombPrefab, spot, Quaternion.identity);

                if (go == null) continue;

                Bomb bomb = go.GetComponent<Bomb>();
                if (bomb != null) bomb.Arm(damage, radius, _fuse);
            }
        }

        public override void AddStat(WeaponStat stat, float flat, float percent)
        {
            switch (stat)
            {
                case WeaponStat.Count:
                    _addCount += Mathf.RoundToInt(flat);
                    break;

                case WeaponStat.Range:
                    _addRadius += flat;
                    _mulRadius += percent;
                    break;

                case WeaponStat.Rate:
                    _mulRate += percent;
                    break;
            }

            base.AddStat(stat, flat, percent);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? Radius : _radius);
        }
    }
}
