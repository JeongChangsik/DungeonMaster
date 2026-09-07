using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 플레이어 자식 오브젝트(OrbitPivot)에 부착
    // 이 오브젝트를 회전시키면 자식으로 매달린 무기들이 함께 공전함
    // 무기를 하나씩 움직이지 않고 피벗 하나만 돌리는 이유:
    // 무기가 2개든 8개든 회전 코드가 한 줄로 끝나기 때문
    public class WeaponOrbit : MonoBehaviour
    {
        [Header("궤도 설정")]
        [SerializeField] private GameObject _weaponPrefab;
        [Min(1)]
        [SerializeField] private int _count = 1;                // 무기 개수
        [Min(0f)]
        [SerializeField] private float _radius = 1.5f;          // 궤도 반경
        [SerializeField] private float _angularSpeed = -180f;    // 초당 회전 각도(양수: 반시계)

        [Header("무기 스탯")]
        [SerializeField] private float _damage = 10f;

        [Header("스프라이트 방향 보정")]
        // 무기의 로컬 +X축이 바깥을 향하도록 배치하므로,
        // 스프라이트가 다른 방향으로 그려져 있으면 그만큼 보정해야 함
        //   칼날이 오른쪽(+X)을 향해 그려짐 ->   0
        //   칼날이 위쪽(+Y)을 향해 그려짐   -> -90
        //   칼날이 아래(-Y)를 향해 그려짐   ->  90
        [SerializeField] private float _angleOffset = 0f;

        #region 유니티 생명주기
        private void Start()
        {
            Rebuild();
        }

        private void Update()
        {
            // 피벗만 회전 -> 자식 무기들이 전부 따라 돎
            // 회전 속도가 곧 공격 속도가 됨(한 바퀴에 한 번씩 판정)
            transform.Rotate(0f, 0f, _angularSpeed * Time.deltaTime);
        }
        #endregion

        #region 공개 메서드
        // 개수/반경/공격력이 바뀌었을 때 다시 배치(레벨업 등)
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (_weaponPrefab == null)
            {
                Debug.LogError($"WeaponOrbit::Rebuild() _weaponPrefab이 비어 있습니다.");
                return;
            }

            ClearWeapons();

            // 360도를 개수로 나눠 균등 배치(2개면 0도/180도, 3개면 0도/120도/240도)
            float step = 360f / _count;
            for (int i = 0; i < _count; i++)
            {
                float angle = step * i;
                Quaternion rot = Quaternion.Euler(0f, 0f, angle);

                GameObject weapon = Instantiate(_weaponPrefab, transform);

                // 원점에서 rot이 가리키는 방향으로 _radius만큼 떨어진 지점
                weapon.transform.localPosition = rot * Vector3.up * _radius;
                // 배치 각도 + 스프라이트 보정 -> 칼날이 바깥을 향하게 됨
                weapon.transform.localRotation = Quaternion.Euler(0f, 0f, angle + _angleOffset);

                weapon.GetComponent<OrbitWeapon>()?.SetDamage(_damage);
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
    }
}
