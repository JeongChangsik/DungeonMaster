using System.Collections.Generic;
using DungeonMaster.Core;
using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 화염 오라.
    //
    // [하는 일] 플레이어 주변 원 안의 적 전부에게 주기적으로 피해를 준다.
    //           범위 안에 오래 머무는 적일수록 여러 번 맞는다. 원형 불꽃 그림이 반경에 맞춰 커진다.
    // [붙이는 곳] 플레이어의 WeaponRoot/AuraArea 에 부착.
    //           _visual 에는 AuraArea 아래의 원형 SpriteRenderer 를 연결한다.
    // [연결] WeaponUpgradeSO 가 Unlock() / AddStat() 을 불러 열고 강화한다.
    //        피해는 적의 IDamagable.TakeDamage() 로 준다.
    // [설계] 콜라이더를 쓰지 않는다.
    //        OnTriggerStay2D + 적별 쿨다운 딕셔너리로 만들면 적이 죽거나 풀에 반납될 때
    //        딕셔너리를 정리해야 해서 복잡해진다. 틱마다 OverlapCircle 한 번이 훨씬 단순하다.
    //        (틱 = "딱" 하고 한 번 때리는 순간. 기본값이면 0.6초마다 한 번씩 원 안을 훑는다.)
    public class WeaponAura : WeaponBase
    {
        [Header("오라 설정")]
        [SerializeField] private float _radius = 2.2f;      // 기본 반경(유닛)
        [Tooltip("몇 초마다 범위 안의 적을 때리는가")]
        [SerializeField] private float _tickInterval = 0.6f;
        [SerializeField] private LayerMask _enemyLayer;     // 적 레이어만 체크할 것. 비워 두면 아무도 안 맞는다

        [Header("연출")]
        [Tooltip("반경에 맞춰 자동으로 크기가 조절되는 원형 스프라이트. 없어도 동작에는 지장 없음")]
        [SerializeField] private SpriteRenderer _visual;

        // 강화 누적분
        private float _addRadius;
        private float _mulRadius = 1f;
        private float _mulRate = 1f;    // 클수록 틱 간격이 짧아진다(더 자주 때린다)

        // 마지막으로 때린 시각(Time.time 기준, 초)
        private float _lastTick;

        // 매 틱 배열을 새로 만들면 GC 가 계속 발생한다
        private static readonly List<Collider2D> _hits = new List<Collider2D>(128);
        private ContactFilter2D _filter;
        private bool _filterReady;

        public override WeaponId Id { get { return WeaponId.Aura; } }

        public float Radius { get { return (_radius + _addRadius) * _mulRadius * GlobalAreaMul; } }

        // 속도 배율로 "나눈다". 배율 2 면 간격이 절반 = 두 배 자주 때린다.
        // 0.05초 아래로는 내려가지 않게 막아서 강화를 많이 먹어도 매 프레임 때리지는 않게 했다.
        public float TickInterval { get { return Mathf.Max(0.05f, _tickInterval / (_mulRate * GlobalRateMul)); } }

        // 매 프레임 불린다. 잠겨 있으면 그림만 끄고 끝, 열려 있으면 그림 크기를 맞추고 때릴 때가 됐는지 본다.
        // Time.time 은 timeScale 의 영향을 받으므로, 레벨업 카드나 일시정지로 시간이 멈추면 오라도 멈춘다.
        private void Update()
        {
            if (!IsUnlocked)
            {
                if (_visual != null && _visual.enabled) _visual.enabled = false;
                return;
            }

            // 반경은 카드뿐 아니라 전역 배율로도 바뀌므로 Rebuild 를 기다리지 않고 매 프레임 맞춘다
            UpdateVisual();

            if (Time.time < _lastTick + TickInterval) return;
            _lastTick = Time.time;

            Tick();
        }

        // 한 번 때리기. 원 안에 겹친 적 콜라이더를 전부 모아서 한 명씩 피해를 준다.
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

                // 피해를 받을 수 있는 대상(IDamagable 을 구현한 것)만 때린다.
                // 콜라이더와 같은 오브젝트에 IDamagable 이 있어야 잡힌다(자식/부모는 찾지 않는다)
                IDamagable target = c.GetComponent<IDamagable>();
                if (target != null) target.TakeDamage(damage);
            }
        }

        // 원형 그림의 크기를 현재 반경에 맞춘다. 판정(OverlapCircle)과 눈에 보이는 원이 어긋나지 않게 하는 용도
        private void UpdateVisual()
        {
            if (_visual == null) return;

            if (!_visual.enabled) _visual.enabled = true;

            // 원형 스프라이트의 지름이 반경의 2배가 되도록.
            // 스프라이트 1유닛 = PPU 기준이라 sprite.bounds 로 실제 크기를 구해 맞춘다
            // (PPU = Pixels Per Unit. 그림 몇 픽셀이 게임 속 1칸인지. 그림마다 달라서 직접 재야 한다.)
            float spriteDiameter = _visual.sprite != null ? _visual.sprite.bounds.size.x : 1f;
            if (spriteDiameter <= 0.0001f) spriteDiameter = 1f;   // 0 으로 나누는 사고 방지

            float scale = (Radius * 2f) / spriteDiameter;
            _visual.transform.localScale = new Vector3(scale, scale, 1f);
        }

        // 오라 전용 스탯(범위, 속도)을 쌓고, 피해량 처리는 부모에게 넘긴다.
        // Count, Pierce 는 오라에 의미가 없어서 받아도 무시한다
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

        // 해금되는 순간 바로 그림을 켠다. 다음 Update 까지 한 프레임 비어 보이지 않게
        protected override void OnUnlock()
        {
            if (_visual != null) _visual.enabled = true;
        }

        // 에디터 씬 뷰에서 이 오브젝트를 선택했을 때만 반경을 주황 원으로 그려 준다. 게임 화면에는 안 나온다.
        // 플레이 중이 아니면 강화가 없으니 기본 반경(_radius)을 보여준다
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? Radius : _radius);
        }
    }
}
