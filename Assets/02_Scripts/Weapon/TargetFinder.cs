using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 가장 가까운 적을 찾는 유틸.
    //
    // [하는 일] 한 점을 중심으로 원을 그리고, 그 안에 있는 적 중 제일 가까운 하나를 돌려준다.
    // [붙이는 곳] static 클래스라 오브젝트에 붙이지 않는다. 어디서든 TargetFinder.FindNearest(...) 로 부른다.
    // [연결] WeaponProjectile(투척 단검), WeaponBoomerang(부메랑)이 발사 방향을 정할 때 부른다.
    // [설계] 무기마다 같은 코드를 반복하지 않으려고 static 으로 뺐다.
    //        씬의 모든 적을 뒤지는 대신 Physics2D.OverlapCircle 을 쓴다.
    //        물리 엔진이 "이 원에 겹친 콜라이더"를 빠르게 골라 주므로, 적이 수백 마리여도 근처만 본다.
    public static class TargetFinder
    {
        // 매번 배열을 새로 만들면 GC 가 계속 발생한다. 재사용 버퍼를 쓴다.
        // (GC = 안 쓰는 메모리를 치우는 청소부. 자주 돌면 게임이 순간순간 끊긴다.)
        // 64 는 처음 크기일 뿐 상한이 아니다. 더 많이 잡히면 List 가 알아서 늘어난다.
        private static readonly List<Collider2D> _results = new List<Collider2D>(64);
        private static ContactFilter2D _filter;
        private static bool _filterReady;

        // origin 에서 radius(유닛) 안에 있는, mask 레이어의 콜라이더 중 가장 가까운 것의 Transform.
        // 하나도 없으면 null 을 돌려준다. 호출하는 쪽은 null 이면 "쏠 대상 없음"으로 처리한다.
        public static Transform FindNearest(Vector2 origin, float radius, LayerMask mask)
        {
            if (!_filterReady)
            {
                _filter = new ContactFilter2D();
                _filterReady = true;
            }

            // 필터 = "무엇을 잡을지" 조건표. 매 호출마다 다시 설정하는 이유는
            // 부르는 무기마다 mask(적 레이어)가 다를 수 있기 때문이다.
            // 적 콜라이더가 트리거라서 useTriggers 를 켜지 않으면 하나도 안 잡힌다
            _filter.useTriggers = true;
            _filter.useLayerMask = true;
            _filter.SetLayerMask(mask);
            _filter.useDepth = false;   // 2D 게임이라 Z 깊이는 따지지 않는다

            _results.Clear();
            Physics2D.OverlapCircle(origin, radius, _filter, _results);

            Transform best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < _results.Count; i++)
            {
                Collider2D c = _results[i];
                if (c == null) continue;

                // 거리 대신 "거리의 제곱"으로 비교한다.
                // 누가 더 가까운지만 알면 되므로 느린 제곱근 계산을 건너뛸 수 있다.
                float sqr = ((Vector2)c.transform.position - origin).sqrMagnitude;
                if (sqr >= bestSqr) continue;

                bestSqr = sqr;
                best = c.transform;
            }

            return best;
        }
    }
}
