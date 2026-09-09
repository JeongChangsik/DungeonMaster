using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 플레이어 자식 오브젝트(OrbitPivot)에 부착
    // 이 오브젝트를 회전시키면 자식으로 매달린 무기들이 함께 공전함
    // 무기를 하나씩 움직이지 않고 피벗 하나만 돌리는 이유:
    // 무기가 2개든 8개든 회전 코드가 한 줄로 끝나기 때문
    //
    // _damage 는 WeaponBase 로 올라갔다. 직렬화 키 이름이 같으므로 씬에 저장된 값은 그대로 유지된다.
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

            int count = EffectiveCount;
            if (count <= 0) return;

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

        public void MultiplySpeed(float multiplier)
        {
            _mulSpeed *= multiplier;   // 회전만 빨라지면 되므로 Rebuild 불필요
        }
        #endregion
    }
}
