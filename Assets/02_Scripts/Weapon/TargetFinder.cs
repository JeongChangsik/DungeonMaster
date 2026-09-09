using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 가장 가까운 적을 찾는 유틸.
    // 무기마다 같은 코드를 반복하지 않으려고 static 으로 뺐다.
    public static class TargetFinder
    {
        // 매번 배열을 새로 만들면 GC 가 계속 발생한다. 재사용 버퍼를 쓴다.
        private static readonly List<Collider2D> _results = new List<Collider2D>(64);
        private static ContactFilter2D _filter;
        private static bool _filterReady;

        public static Transform FindNearest(Vector2 origin, float radius, LayerMask mask)
        {
            if (!_filterReady)
            {
                _filter = new ContactFilter2D();
                _filterReady = true;
            }

            // 적 콜라이더가 트리거라서 useTriggers 를 켜지 않으면 하나도 안 잡힌다
            _filter.useTriggers = true;
            _filter.useLayerMask = true;
            _filter.SetLayerMask(mask);
            _filter.useDepth = false;

            _results.Clear();
            Physics2D.OverlapCircle(origin, radius, _filter, _results);

            Transform best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < _results.Count; i++)
            {
                Collider2D c = _results[i];
                if (c == null) continue;

                float sqr = ((Vector2)c.transform.position - origin).sqrMagnitude;
                if (sqr >= bestSqr) continue;

                bestSqr = sqr;
                best = c.transform;
            }

            return best;
        }
    }
}
