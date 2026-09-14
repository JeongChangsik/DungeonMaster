using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 무기들의 관리자(무기 서랍장).
    //
    // [하는 일] 무기들을 한 곳에서 찾고, 전역 배율이 바뀌었을 때 전부 갱신하는 역할만 한다.
    //           직접 공격하거나 스탯을 들고 있지는 않는다.
    // [붙이는 곳] 플레이어의 WeaponRoot 에 부착.
    //           계층: Player > WeaponRoot(여기) > OrbitPivot / ProjectileMuzzle / AuraArea ...
    // [연결] RoguelikePlayer.Awake 가 GetComponentInChildren 으로 이걸 찾아 Weapons 프로퍼티에 담는다.
    //        WeaponUpgradeSO 가 Get(WeaponId) 로 강화할 무기를 찾는다.
    //        RoguelikePlayer.ApplyStatUpgrade 가 무기 전역 배율 카드를 적용한 뒤 RebuildAll() 을 부른다.
    // [설계] 이 컴포넌트가 없으면 "모든 무기 피해량 +10%" 같은 카드를 만들 수 없다.
    //        이미 배치된 칼날들에게 바뀐 피해량을 다시 내려보낼 주체가 없기 때문.
    //        무기를 새로 추가해도 WeaponRoot 아래에 붙이기만 하면 자동으로 찾아지므로, 여기에 목록을 등록할 필요가 없다.
    public class WeaponManager : MonoBehaviour
    {
        // 타입으로 무기 하나를 찾는다. 예: Get<WeaponOrbit>()
        // 인자 true = 꺼져 있는(비활성) 자식 오브젝트까지 뒤진다. 잠긴 무기가 꺼져 있어도 찾을 수 있게.
        //
        // 캐싱하지 않는다.
        // 호출이 판당 수십 번뿐이라 성능상 의미가 없고, 캐싱하면 Awake 실행 순서에
        // 의존하게 되어 오히려 깨지기 쉽다.
        // (Awake 는 오브젝트마다 불리는 순서가 정해져 있지 않다. 이 매니저가 무기들보다 먼저 깨어나면
        //  캐시가 비어 버릴 수 있다.)
        public T Get<T>() where T : WeaponBase
        {
            return GetComponentInChildren<T>(true);
        }

        // 카드 SO 에 적힌 WeaponId 로 무기를 찾는다. 없으면 null.
        // 같은 Id 가 둘이면 먼저 찾은 쪽을 돌려준다.
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
