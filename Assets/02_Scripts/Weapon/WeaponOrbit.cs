using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 플레이어 자식 오브젝트(OrbitPivot)에 부착
    // 이 오브젝트를 회전시키면 자식으로 매달린 무기들이 함께 공전함
    // 무기를 하나씩 움직이지 않고 피벗 하나만 돌리는 이유:
    // 무기가 2개든 8개든 회전 코드가 한 줄로 끝나기 때문
    //
    // _damage 는 WeaponBase 로 올라갔다. 직렬화 키 이름이 같으므로 씬에 저장된 값은 그대로 유지된다.
    //
    // [하는 일] 플레이어 주위를 빙글빙글 도는 칼날(공전 칼날). 닿는 적에게 피해를 준다.
    // [붙이는 곳] 위 설명대로 Player > WeaponRoot > OrbitPivot 에 부착.
    //           _weaponPrefab 에는 OrbitWeapon 이 붙은 칼날 프리팹을 넣는다.
    // [연결] WeaponBase 를 물려받는다. 칼날 하나하나의 피격 판정은 칼날 프리팹의 OrbitWeapon 이 한다.
    //        WeaponUpgradeSO 는 AddStat() 을, 옛 카드(OrbitCountUpgradeSO 등)는 AddCount() 같은 전용 함수를 부른다.
    //        전역 배율 카드가 오면 WeaponManager.RebuildAll() 을 통해 Rebuild() 가 불린다.
    // [설계] 다른 무기와 달리 칼날을 미리 만들어 두는 무기라서, 스탯이 바뀔 때마다 Rebuild() 로
    //        칼날을 전부 지우고 다시 만든다. Rebuild 는 게임 시작(Start)과 카드를 고를 때만 불려서
    //        자주 일어나지 않으므로, 오브젝트 풀 없이 Instantiate/Destroy 로 새로 만든다.
    public class WeaponOrbit : WeaponBase
    {
        [Header("궤도 설정")]
        [SerializeField] private GameObject _weaponPrefab;
        // 0으로 시작한다. 시작 무기는 LevelUpUI의 Starting Upgrades가 넣어준다
        [Min(0)]
        [SerializeField] private int _count = 0;                // 무기 개수
        [Min(0f)]
        [SerializeField] private float _radius = 1.5f;          // 궤도 반경
        [SerializeField] private float _angularSpeed = -180f;    // 초당 회전 각도(양수: 반시계)

        [Header("스프라이트 방향 보정")]
        // 무기의 로컬 +X축이 바깥을 향하도록 배치하므로,
        // 스프라이트가 다른 방향으로 그려져 있으면 그만큼 보정해야 함
        //   칼날이 오른쪽(+X)을 향해 그려짐 ->   0
        //   칼날이 위쪽(+Y)을 향해 그려짐   -> -90
        //   칼날이 아래(-Y)를 향해 그려짐   ->  90
        [SerializeField] private float _angleOffset = 0f;

        // 프리팹 원본 크기. 궤도가 커질 때 이 값을 기준으로 비례해서 키운다
        private Vector3 _baseWeaponScale = Vector3.one;
        private bool _baseScaleCached;

        // 강화 누적분
        private int _addCount;
        private float _addRadius;
        private float _mulRadius = 1f;
        private float _mulSpeed = 1f;

        public override WeaponId Id => WeaponId.Orbit;

        // 공전 칼날은 "칼날이 1개라도 있으면 해금된 것"이다.
        // 별도의 해금 플래그를 두면 개수와 어긋날 수 있어서 개수로 판단한다.
        public override bool IsUnlocked => EffectiveCount > 0;

        // 기본값 + 강화분 + 전역 배율을 합친 "지금 실제로 쓰는 값".
        // 반경은 0.1 아래로 내려가지 않게 막았다. 0 이나 음수가 되면 칼날이 한 점에 뭉치거나 반대편으로 뒤집힌다.
        // 회전 속도에 GlobalRateMul 을 곱해서 "모든 무기 속도 UP" 카드가 칼날 회전에도 먹힌다.
        public int EffectiveCount => Mathf.Max(0, _count + _addCount);
        public float EffectiveRadius => Mathf.Max(0.1f, (_radius + _addRadius) * _mulRadius * GlobalAreaMul);
        public float EffectiveAngularSpeed => _angularSpeed * _mulSpeed * GlobalRateMul;

        #region 유니티 생명주기
        private void Start()
        {
            Rebuild();
        }

        private void Update()
        {
            // 피벗만 회전 -> 자식 무기들이 전부 따라 돎
            // 회전 속도가 곧 공격 속도가 됨(한 바퀴에 한 번씩 판정)
            transform.Rotate(0f, 0f, EffectiveAngularSpeed * Time.deltaTime);
        }
        #endregion

        #region 공개 메서드
        // 개수/반경/공격력이 바뀌었을 때 다시 배치(레벨업 등)
        [ContextMenu("Rebuild")]
        public override void Rebuild()
        {
            if (_weaponPrefab == null)
            {
                Debug.LogError($"WeaponOrbit::Rebuild() _weaponPrefab이 비어 있습니다.");
                return;
            }

            if (!_baseScaleCached)
            {
                _baseWeaponScale = _weaponPrefab.transform.localScale;
                _baseScaleCached = true;
            }

            ClearWeapons();

            // 개수가 0 이면(아직 해금 전) 옛 칼날만 지우고 끝낸다.
            // 지우기를 먼저 하는 이유: 개수가 줄어든 경우에도 남은 칼날이 없어야 하기 때문
            int count = EffectiveCount;
            if (count <= 0) return;

            // 반복문 안에서 매번 계산하지 않도록 한 번만 구해 둔다
            float radius = EffectiveRadius;
            float damage = Damage;

            // 360도를 개수로 나눠 균등 배치(2개면 0도/180도, 3개면 0도/120도/240도)
            float step = 360f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = step * i;
                Quaternion rot = Quaternion.Euler(0f, 0f, angle);

                GameObject weapon = Instantiate(_weaponPrefab, transform);

                // 원점에서 rot이 가리키는 방향으로 radius만큼 떨어진 지점
                weapon.transform.localPosition = rot * Vector3.up * radius;
                // 배치 각도 + 스프라이트 보정 -> 칼날이 바깥을 향하게 됨
                weapon.transform.localRotation = Quaternion.Euler(0f, 0f, angle + _angleOffset);

                // 궤도가 커지면 칼날도 같이 커진다.
                //
                // 이게 없으면 '궤도 확장'이나 '무기 범위 UP' 카드가 오히려 손해가 된다.
                // 칼날이 훑는 구간은 (반경 ± 칼날 길이의 절반) 인데, 반경만 커지면
                // 그 구간이 통째로 바깥으로 밀려나서 플레이어에게 붙어 있는 적을 아예 못 때린다.
                // 크기까지 같이 키우면 훑는 구간이 넓어지므로 안쪽도 계속 닿는다.
                // 인스펙터의 기본 반경(_radius)일 때 원래 크기(배율 1)가 되도록 기본 반경으로 나눈다.
                // Max(0.01) 은 기본 반경이 0 일 때 0 으로 나누는 사고를 막는다.
                weapon.transform.localScale = _baseWeaponScale * (radius / Mathf.Max(0.01f, _radius));

                weapon.GetComponent<OrbitWeapon>()?.SetDamage(damage);
            }
        }
        #endregion

        #region 내부 메서드
        private void ClearWeapons()
        {
            // 뒤에서부터 지워야 인덱스가 밀리지 않음
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;

                // Destroy는 프레임 끝에 처리됨
                // 그때까지 콜라이더가 살아 있으면 새 무기와 함께 두 번 때리므로 즉시 꺼 둠
                child.SetActive(false);
                Destroy(child);
            }
        }
        #endregion

        #region 강화
        // WeaponUpgradeSO 가 쓰는 통합 진입점
        // 공전 칼날 전용 스탯(개수, 반경, 회전 속도)을 여기서 쌓는다. Pierce 는 칼날에 의미가 없어 무시된다.
        // 개수는 정수라서 flat 을 반올림해서 더한다(카드에 1 을 넣으면 칼날 1개)
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
                    _mulSpeed += percent;
                    break;
            }

            // Damage 처리 + Rebuild 는 부모가 한다
            base.AddStat(stat, flat, percent);
        }

        // --- 아래 4개는 기존 업그레이드 SO(OrbitCountUpgradeSO 등)가 쓰는 시그니처.
        //     내부만 보정 필드에 쌓도록 바꾸고 이름/인자는 그대로 두어 기존 에셋이 안 깨지게 함 ---
        public void AddCount(int amount)
        {
            _addCount += amount;
            Rebuild();          // 개수가 바뀌었으니 재배치
        }

        public void AddDamage(float amount)
        {
            _addDamage += amount;
            Rebuild();          // 이미 생성된 무기들에 새 데미지를 내려줘야 함
        }

        public void AddRadius(float amount)
        {
            _addRadius += amount;
            Rebuild();
        }

        // 옛 카드용이라 배율에 "곱한다". 새 카드의 AddStat(Rate) 는 "더한다".
        // 두 종류를 섞어 먹으면 먹은 순서에 따라 최종 속도가 조금 달라진다.
        public void MultiplySpeed(float multiplier)
        {
            _mulSpeed *= multiplier;   // 회전만 빨라지면 되므로 Rebuild 불필요
        }
        #endregion
    }
}
