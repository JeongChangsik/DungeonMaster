using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 플레이어의 WeaponRoot 에 부착.
    // 무기들을 한 곳에서 찾고, 전역 배율이 바뀌었을 때 전부 갱신하는 역할만 한다.
    //
    // 이 컴포넌트가 없으면 "모든 무기 피해량 +10%" 같은 카드를 만들 수 없다.
    // 이미 배치된 칼날들에게 바뀐 피해량을 다시 내려보낼 주체가 없기 때문.
    public class WeaponManager : MonoBehaviour
    {
        // 캐싱하지 않는다.
        // 호출이 판당 수십 번뿐이라 성능상 의미가 없고, 캐싱하면 Awake 실행 순서에
        // 의존하게 되어 오히려 깨지기 쉽다.
        public T Get<T>() where T : WeaponBase
        {
            return GetComponentInChildren<T>(true);
        }

        public WeaponBase Get(WeaponId id)
        {
            foreach (WeaponBase w in GetComponentsInChildren<WeaponBase>(true))
            {
                if (w.Id == id) return w;
            }
            return null;
        }

        // 플레이어 전역 배율(무기 피해/범위/속도)이 바뀐 뒤에 호출한다.
        // 배율 값만 바꾸면 이미 생성된 칼날들은 옛 피해량을 들고 있다.
        public void RebuildAll()
        {
            foreach (WeaponBase w in GetComponentsInChildren<WeaponBase>(true))
            {
                w.Rebuild();
            }
        }
    }
}
