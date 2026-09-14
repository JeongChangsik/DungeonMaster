using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 폭탄 투하. WeaponRoot 아래 빈 오브젝트에 부착.
    // 일정 주기로 플레이어가 서 있는 자리에 폭탄을 놓는다.
    //
    // [하는 일] 몇 초마다 발밑에 폭탄을 떨어뜨린다. 폭탄은 잠시 뒤 터져서 주변 적을 한꺼번에 때린다.
    //          이 스크립트는 "언제, 몇 개, 어디에 놓을지"만 정하는 투하 담당이다.
    //          실제로 기다렸다가 터지는 일은 폭탄 프리팹에 붙은 Bomb.cs 가 한다.
    // [붙이는 곳] RL_Warrior 프리팹의 WeaponRoot 아래 BombDropper 오브젝트.
    //          플레이어 자식이라 플레이어를 따라다니고, transform.position 이 곧 플레이어 발밑이다.
    // [연결] WeaponUpgradeSO(폭탄 카드)가 Unlock() 과 AddStat() 을 부른다.
    //          투하할 때 ObjectPool 에서 Bomb 프리팹을 꺼내고 Bomb.Arm() 으로 피해량/반경/심지 시간을 넘긴다.
    //          전역 배율(모든 무기 범위/속도)은 부모 WeaponBase 를 통해 플레이어에게서 읽는다.
    // [설계] 조준하지 않는 것이 이 무기의 성격이다.
    //          다른 무기가 "적을 찾아 때리는" 것과 달리, 폭탄은 "내가 지나온 길"에 남는다.
    //          그래서 적을 몰고 다니다가 방향을 트는 플레이가 강해진다.
    //          피해량/반경은 폭탄 프리팹에 박아두지 않고, 놓는 순간 이 무기가 계산해서 넘겨준다.
    //          그래야 강화 카드를 먹었을 때 프리팹을 고치지 않아도 새로 놓는 폭탄부터 바로 세진다.
    public class WeaponBomb : WeaponBase
    {
        [Header("투하 설정")]
        // 폭탄 프리팹. 루트에 Bomb 컴포넌트가 있어야 한다
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

        // 강화 누적분.
        // 인스펙터 값(기본값)은 건드리지 않고, 카드로 오른 만큼만 여기에 따로 쌓는다.
        // add 는 더하는 양(0 = 강화 없음), mul 은 곱하는 배율(1 = 그대로, 1.1 = 10% 증가)
        private int _addCount;
        private float _addRadius;
        private float _mulRadius = 1f;
        private float _mulRate = 1f;

        // 마지막으로 폭탄을 놓은 시각(초, Time.time 기준)
        private float _lastDrop;

        // 카드 SO 가 "폭탄 무기"를 찾을 때 이 값으로 알아본다
        public override WeaponId Id { get { return WeaponId.Bomb; } }

        // 아래 세 값은 "기본값 + 강화분 + 전역 배율"을 합친 최종값이다. 필드 대신 항상 이것을 쓴다.
        // 투하 간격(초). 속도 배율이 커질수록 짧아진다. 너무 짧아져 화면이 폭탄으로 덮이지 않게 0.2초가 바닥이다
        public float Interval { get { return Mathf.Max(0.2f, _interval / (_mulRate * GlobalRateMul)); } }
        public int Count { get { return Mathf.Max(1, _count + _addCount); } }
        // 폭발 반경(유니티 단위, 1 = 타일 한 칸 정도)
        public float Radius { get { return (_radius + _addRadius) * _mulRadius * GlobalAreaMul; } }

        // 매 프레임 "놓을 시간이 됐나"만 확인한다.
        // Time.time 은 Time.timeScale 의 영향을 받는 시계라서, 레벨업 카드나 일시정지로 시간이 멈추면
        // 이 시계도 같이 멈춘다. 그래서 멈춘 동안에는 폭탄이 쌓이지 않는다
        private void Update()
        {
            // 아직 해금 카드를 안 먹었으면 오브젝트는 있어도 아무것도 하지 않는다
            if (!IsUnlocked) return;
            if (Time.time < _lastDrop + Interval) return;

            _lastDrop = Time.time;
            Drop();
        }

        // 폭탄을 Count 개 놓고 각각 불을 붙인다. Update 에서 간격마다 한 번 불린다
        private void Drop()
        {
            if (_bombPrefab == null)
            {
                Debug.LogError($"WeaponBomb::Drop() _bombPrefab이 비어 있습니다.");
                return;
            }

            // 최종값은 계산이 들어가므로 반복문 밖에서 한 번만 구해 둔다
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

                // 오브젝트 풀: 폭탄을 매번 새로 만들고 부수면 게임이 버벅이므로,
                // 창고에 넣어둔 폭탄을 꺼내 쓰고 다 쓰면 돌려놓는다.
                // 풀이 씬에 없을 때(테스트 씬 등)만 Instantiate 로 새로 만든다
                GameObject go = ObjectPool.Instance != null
                    ? ObjectPool.Instance.Spawn(_bombPrefab, spot)
                    : Instantiate(_bombPrefab, spot, Quaternion.identity);

                if (go == null) continue;

                // 꺼낸 폭탄은 지난번에 쓰던 값이 남아 있을 수 있으니, 이번 값으로 새로 채워서 불을 붙인다
                Bomb bomb = go.GetComponent<Bomb>();
                if (bomb != null) bomb.Arm(damage, radius, _fuse);
            }
        }

        // 강화 카드가 부르는 입구. 폭탄에 의미 있는 스탯만 여기서 받고,
        // 피해량(Damage)은 모든 무기가 같으므로 맨 아래 base.AddStat 에서 부모가 처리한다.
        // Pierce(관통)는 폭탄과 상관없어서 받지 않는다(카드를 만들어도 효과가 없다)
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

        // 에디터에서 이 오브젝트를 선택하면 씬 뷰에 폭발 반경을 원으로 그려준다(게임 화면에는 안 보인다).
        // 플레이 중에는 강화가 반영된 최종 반경, 편집 중에는 인스펙터 기본값을 보여준다
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? Radius : _radius);
        }
    }
}
