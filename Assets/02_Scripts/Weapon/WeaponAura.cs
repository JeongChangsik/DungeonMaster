using System.Collections.Generic;
using DungeonMaster.Core;
using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 화염 오라. 플레이어의 WeaponRoot/AuraArea 에 부착.
    // 플레이어 주변 원 안의 적 전부에게 주기적으로 피해를 준다.
    //
    // 콜라이더를 쓰지 않는다.
    // OnTriggerStay2D + 적별 쿨다운 딕셔너리로 만들면 적이 죽거나 풀에 반납될 때
    // 딕셔너리를 정리해야 해서 복잡해진다. 틱마다 OverlapCircle 한 번이 훨씬 단순하다.
    public class WeaponAura : WeaponBase
    {
        [Header("오라 설정")]
        [SerializeField] private float _radius = 2.2f;
        [Tooltip("몇 초마다 범위 안의 적을 때리는가")]
        [SerializeField] private float _tickInterval = 0.6f;
        [SerializeField] private LayerMask _enemyLayer;

        [Header("연출")]
        [Tooltip("반경에 맞춰 자동으로 크기가 조절되는 원형 스프라이트. 없어도 동작에는 지장 없음")]
        [SerializeField] private SpriteRenderer _visual;

        // 강화 누적분
        private float _addRadius;
        private float _mulRadius = 1f;
        private float _mulRate = 1f;

        private float _lastTick;

        // 매 틱 배열을 새로 만들면 GC 가 계속 발생한다
        private static readonly List<Collider2D> _hits = new List<Collider2D>(128);
        private ContactFilter2D _filter;
        private bool _filterReady;

        public override WeaponId Id { get { return WeaponId.Aura; } }

        public float Radius { get { return (_radius + _addRadius) * _mulRadius * GlobalAreaMul; } }
        public float TickInterval { get { return Mathf.Max(0.05f, _tickInterval / (_mulRate * GlobalRateMul)); } }

        private void Update()
        {
            if (!IsUnlocked)
            {
                if (_visual != null && _visual.enabled) _visual.enabled = false;
                return;
            }

            UpdateVisual();

            if (Time.time < _lastTick + TickInterval) return;
            _lastTick = Time.time;

            Tick();
        }

        private void Tick()
        {
            if (!_filterReady)
            {
                _filter = new ContactFilter2D();
                _filterReady = true;
            }

            // 적 콜라이더가 트리거라서 useTriggers 를 켜야 잡힌다
            _filter.useTriggers = true;
            _filter.useLayerMask = true;
            _filter.SetLayerMask(_enemyLayer);
            _filter.useDepth = false;

            _hits.Clear();
            Physics2D.OverlapCircle(transform.position, Radius, _filter, _hits);

            float damage = Damage;
            for (int i = 0; i < _hits.Count; i++)
            {
                Collider2D c = _hits[i];
                if (c == null) continue;

                IDamagable target = c.GetComponent<IDamagable>();
                if (target != null) target.TakeDamage(damage);
            }
        }

        private void UpdateVisual()
        {
            if (_visual == null) return;

            if (!_visual.enabled) _visual.enabled = true;

            // 원형 스프라이트의 지름이 반경의 2배가 되도록.
            // 스프라이트 1유닛 = PPU 기준이라 sprite.bounds 로 실제 크기를 구해 맞춘다
            float spriteDiameter = _visual.sprite != null ? _visual.sprite.bounds.size.x : 1f;
            if (spriteDiameter <= 0.0001f) spriteDiameter = 1f;

            float scale = (Radius * 2f) / spriteDiameter;
            _visual.transform.localScale = new Vector3(scale, scale, 1f);
        }

        public override void AddStat(WeaponStat stat, float flat, float percent)
        {
            switch (stat)
            {
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

        protected override void OnUnlock()
        {
            if (_visual != null) _visual.enabled = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? Radius : _radius);
        }
    }
}
