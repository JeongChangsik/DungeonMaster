using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 나갔다가 던진 사람에게 돌아오는 투사체.
    //
    // [하는 일] 빙글빙글 돌며 곧게 날아가다가, 던진 곳에서 정해진 거리만큼 멀어지면
    //          방향을 바꿔 주인을 쫓아 돌아온다. 주인 가까이 오면 사라진다(풀로 반납).
    // [붙이는 곳] Axe 프리팹의 루트. 부모 Projectile 과 같은 조건이 필요하다
    //          (Rigidbody2D 는 Kinematic, Collider2D 는 Is Trigger 체크).
    // [연결] WeaponBoomerang 이 풀에서 꺼낸 뒤 Configure() -> Launch() 순서로 부른다.
    //          매 프레임 부모의 Update 가 Move() 를 부르고, 적과 닿으면 부모의 OnTriggerEnter2D 가 피해를 준다.
    // [설계] Projectile 을 상속해서 피격 판정 / 관통 / 풀 반납 / IPoolable 구현을
    //          전부 물려받는다. 여기서 재정의하는 건 "어떻게 움직이는가" 하나뿐이다.
    //          (상속 = 부모 클래스의 기능을 그대로 물려받고, override 로 필요한 부분만 바꿔 쓰는 것.
    //          부모가 virtual 로 "바꿔도 된다"고 열어둔 함수만 override 할 수 있다)
    //          주의: 부모의 수명(_lifetime) 규칙도 그대로 적용된다. 수명이 끝나면 돌아오는 도중이어도 사라지므로,
    //          사거리를 크게 늘리면 Axe 프리팹의 Lifetime 도 넉넉히 늘려야 한다.
    public class BoomerangProjectile : Projectile
    {
        // 아래 _outRange 는 인스펙터 값이 있지만, 실제 게임에서는 Configure() 가 무기의 사거리로 덮어쓴다
        [Header("부메랑")]
        [Tooltip("이 거리까지 나갔다가 돌아온다")]
        [SerializeField] private float _outRange = 5f;
        [Tooltip("초당 회전 각도. 빙글빙글 도는 연출")]
        [SerializeField] private float _spinSpeed = 720f;
        [Tooltip("돌아와서 이 거리 안에 들면 회수된다")]
        [SerializeField] private float _catchDistance = 0.5f;

        // 돌아갈 곳(던진 무기 오브젝트). 플레이어를 따라 움직인다
        private Transform _owner;
        // 던진 순간의 위치. "얼마나 멀리 나왔나"는 플레이어가 아니라 이 점에서 잰다
        private Vector2 _origin;
        // 돌아올 때 쓸 속도. 나갈 때와 같은 값이다
        private float _speed;
        // false = 나가는 중, true = 돌아오는 중
        private bool _returning;

        // WeaponBoomerang 이 발사 직전(Launch 보다 먼저) 호출해서 주인과 왕복 거리를 알려준다.
        // 부모의 Launch 는 모든 투사체가 같은 모양이라 부메랑 전용 값을 넣을 자리가 없어서 따로 뺐다
        public void Configure(Transform owner, float outRange)
        {
            _owner = owner;
            _outRange = outRange;
        }

        // 발사. 부모 Launch 로 피해량/관통/방향/속도를 먼저 세팅한 뒤,
        // 부메랑에만 필요한 출발점과 "나가는 중" 상태를 채운다.
        // 풀에서 재사용되므로 지난번 비행의 _returning 값이 남지 않게 여기서도 false 로 되돌린다
        public override void Launch(Vector2 direction, float damage, float speed, int pierce, float angleOffset)
        {
            base.Launch(direction, damage, speed, pierce, angleOffset);

            _origin = transform.position;
            _speed = speed;
            _returning = false;
        }

        // 부모 Projectile.Update 가 매 프레임 부른다. 부메랑의 움직임 전부가 여기 있다.
        // 나가는 동안은 Launch 에서 받은 속도로 그냥 직진하므로 거리만 확인하고,
        // 돌아오는 동안은 매 프레임 주인 쪽으로 방향을 새로 잡는다(주인이 계속 움직이기 때문)
        protected override void Move()
        {
            // 회전 연출. 스프라이트 방향 보정과 무관하게 계속 돈다
            transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);

            if (!_returning)
            {
                // 나가는 중: 최대 거리에 닿으면 방향을 튼다.
                // sqrMagnitude 는 "거리의 제곱"이다. 진짜 거리를 구하려면 제곱근 계산이 필요한데,
                // 양쪽을 다 제곱해서 비교해도 결과가 같으므로 더 싼 계산으로 비교한다
                if (((Vector2)transform.position - _origin).sqrMagnitude < _outRange * _outRange) return;

                _returning = true;

                // 돌아오는 길에 같은 적을 한 번 더 때릴 수 있게 기록을 비운다.
                // 이 기록은 부모 Projectile 이 "이미 때린 적" 을 모아두는 목록이다.
                // 단, 남은 관통 횟수(_pierceLeft)는 되돌리지 않으므로 갈 때 다 쓰면 돌아오는 첫 적에서 사라진다
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

        // 풀에서 꺼낸 직후 ObjectPool 이 부른다. 부모가 공통 초기화를 하게 한 뒤,
        // 지난번에 돌아오던 도중 회수된 도끼가 처음부터 "돌아오는 중"으로 나오지 않게 되돌린다
        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            _returning = false;
        }
    }
}
