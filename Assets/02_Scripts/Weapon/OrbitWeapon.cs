using DungeonMaster.Core;
using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 공전하는 무기 본체(칼 프리팹에 부착)
    // 필요한 컴포넌트:
    //  - Collider2D : Is Trigger 체크
    //  - Rigidbody2D : Body Type을 Kinematic으로
    //    (Transform으로만 움직이면 유니티가 정적 콜라이더로 취급해서
    //     매 프레임 물리 트리를 다시 만듦 -> 느리고 트리거 콜백도 불안정)
    [RequireComponent(typeof(Rigidbody2D))]
    public class OrbitWeapon : MonoBehaviour
    {
        [SerializeField] private float _damage = 10f;

        // WeaponOrbit이 생성 직후 자기 스탯을 내려줌
        public void SetDamage(float damage) => _damage = damage;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Enemy")) return;

            // 무기가 계속 움직이므로 적을 지나칠 때마다 Enter/Exit가 반복됨
            // -> 한 바퀴에 한 번씩만 데미지가 들어가므로 별도 쿨다운이 필요 없음
            other.GetComponent<IDamagable>()?.TakeDamage(_damage);
        }
    }
}
