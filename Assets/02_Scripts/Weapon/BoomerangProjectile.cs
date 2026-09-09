using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 나갔다가 던진 사람에게 돌아오는 투사체.
    //
    // Projectile 을 상속해서 피격 판정 / 관통 / 풀 반납 / IPoolable 구현을
    // 전부 물려받는다. 여기서 재정의하는 건 "어떻게 움직이는가" 하나뿐이다.
    public class BoomerangProjectile : Projectile
    {
        [Header("부메랑")]
        [Tooltip("이 거리까지 나갔다가 돌아온다")]
        [SerializeField] private float _outRange = 5f;
        [Tooltip("초당 회전 각도. 빙글빙글 도는 연출")]
        [SerializeField] private float _spinSpeed = 720f;
        [Tooltip("돌아와서 이 거리 안에 들면 회수된다")]
        [SerializeField] private float _catchDistance = 0.5f;

        private Transform _owner;
        private Vector2 _origin;
        private float _speed;
        private bool _returning;

        // WeaponBoomerang 이 발사 직후 호출해서 주인과 왕복 거리를 알려준다
        public void Configure(Transform owner, float outRange)
        {
            _owner = owner;
            _outRange = outRange;
        }

        public override void Launch(Vector2 direction, float damage, float speed, int pierce, float angleOffset)
        {
            base.Launch(direction, damage, speed, pierce, angleOffset);

            _origin = transform.position;
            _speed = speed;
            _returning = false;
        }

        protected override void Move()
        {
            // 회전 연출. 스프라이트 방향 보정과 무관하게 계속 돈다
            transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);

            if (!_returning)
            {
                // 나가는 중: 최대 거리에 닿으면 방향을 튼다
                if (((Vector2)transform.position - _origin).sqrMagnitude < _outRange * _outRange) return;

                _returning = true;

                // 돌아오는 길에 같은 적을 한 번 더 때릴 수 있게 기록을 비운다
                _hitTargets.Clear();
                return;
            }

            // 돌아오는 중: 주인을 쫓아간다. 주인이 사라졌으면 그냥 회수
            if (_owner == null) { ReleaseSelf(); return; }

            Vector2 toOwner = (Vector2)_owner.position - (Vector2)transform.position;
            if (toOwner.sqrMagnitude <= _catchDistance * _catchDistance)
            {
                ReleaseSelf();
                return;
            }

            _rb.linearVelocity = toOwner.normalized * _speed;
        }

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            _returning = false;
        }
    }
}
