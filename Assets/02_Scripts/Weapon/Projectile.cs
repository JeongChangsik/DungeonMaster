using System.Collections.Generic;
using DungeonMaster.Core;
using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 직진하는 투사체. 오브젝트 풀에서 재사용된다.
    //
    // [하는 일] 한 번 던져지면 정한 방향으로 날아가다가 적에 닿으면 피해를 준다.
    //           관통 횟수가 남아 있으면 계속 날아가고, 다 쓰거나 수명(_lifetime)이 끝나면 풀로 돌아간다.
    // [붙이는 곳] 투척 단검 같은 투사체 프리팹의 루트(맨 위 오브젝트)에 부착.
    // [연결] WeaponProjectile(투척 단검)이 ObjectPool.Spawn 으로 꺼낸 뒤 Launch() 를 부른다.
    //        피해는 맞은 대상의 IDamagable.TakeDamage() 로 준다.
    //        BoomerangProjectile 이 이 클래스를 물려받아 Launch() / Move() / OnSpawnFromPool() 을 바꿔서
    //        "갔다가 돌아오는" 투사체를 만든다.
    // [설계] 피격/관통/반납 같은 공통 동작은 여기 두고, 무기마다 달라지는 부분(발사 준비, 움직임)만
    //        virtual 로 열어 두었다. 그래서 새 투사체를 만들 때 주로 "어떻게 움직이는가"만 새로 쓰면 된다.
    //        Destroy 하지 않고 풀에 돌려주므로, 다시 꺼내질 때 지난번 상태(맞힌 적 목록 등)를 반드시 지워야 한다.
    //
    // 필요한 컴포넌트
    //  - Rigidbody2D : Kinematic (물리에 밀리면 안 되고, 트리거 콜백은 받아야 함)
    //  - Collider2D  : Is Trigger 체크
    //  (Rigidbody2D 가 없으면 두 트리거가 겹쳐도 OnTriggerEnter2D 가 안 불릴 수 있다.
    //   RequireComponent 가 붙어 있어서 이 스크립트를 넣으면 Rigidbody2D 가 자동으로 따라 붙는다.)
    //
    // IPoolable 은 반드시 프리팹 루트에 있어야 한다.
    // ObjectPool.Spawn 이 GetComponent<IPoolable>() 로 찾기 때문(InChildren 아님).
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour, IPoolable
    {
        [Tooltip("이 시간이 지나면 아무것도 못 맞혀도 풀로 돌아간다")]
        [SerializeField] private float _lifetime = 3f;

        [Tooltip("맞힐 대상의 태그. 플레이어 무기는 Enemy, 적의 원거리 공격은 PLAYER")]
        [SerializeField] private string _targetTag = "Enemy";

        // protected = 자식 클래스(BoomerangProjectile)도 쓸 수 있게 열어 둔 필드
        protected Rigidbody2D _rb;
        protected float _damage;
        protected int _pierceLeft;      // 앞으로 더 뚫을 수 있는 적 수. 0 인 상태로 맞히면 사라진다
        protected float _spawnTime;     // Launch 된 시각. 수명 계산의 기준

        // 이미 반납했는지. 관통 투사체가 같은 프레임에 두 적을 맞히면
        // Release 이후에도 그 프레임의 나머지 OnTriggerEnter2D 가 계속 들어온다.
        protected bool _released;

        // 관통 중 같은 적을 여러 번 때리지 않도록 기록.
        // Unity 6 에서 GetInstanceID() 가 폐기되어 콜라이더 참조를 그대로 담는다.
        // 투사체 수명이 짧아서 참조를 들고 있어도 문제되지 않는다.
        // (HashSet = 같은 것이 두 번 들어가지 않는 주머니. "이미 있나?" 확인이 빠르다.)
        protected readonly HashSet<Collider2D> _hitTargets = new HashSet<Collider2D>();

        // 프리팹이 처음 만들어질 때 딱 한 번 불린다. 풀에서 다시 꺼내질 때는 안 불린다
        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        // 발사. 스포너가 위치를 잡아준 뒤 호출한다.
        // ObjectPool.Spawn 은 position 만 설정하고 rotation 은 건드리지 않으므로 여기서 직접 맞춘다.
        // 한 번 날릴 때마다 필요한 값을 전부 새로 넣는다. 풀에서 재사용되니 지난번 값이 남아 있으면 안 된다.
        // angleOffset: 그림이 오른쪽(+X)이 아닌 방향으로 그려져 있을 때 돌려서 맞추는 각도
        public virtual void Launch(Vector2 direction, float damage, float speed, int pierce, float angleOffset)
        {
            _damage = damage;
            _pierceLeft = pierce;
            _spawnTime = Time.time;
            _released = false;
            _hitTargets.Clear();

            // 방향이 (0,0) 이면 어디로 갈지 알 수 없으니 오른쪽으로 보낸다
            Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

            // 날아가는 방향을 각도로 바꿔서 그림이 그쪽을 바라보게 한다
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);

            // 속도를 한 번만 주면 물리 엔진이 매 프레임 알아서 옮겨 준다(Kinematic 이라 멈추거나 밀리지 않는다)
            _rb.linearVelocity = dir * speed;
        }

        // 매 프레임 불린다. 움직임(Move)을 처리하고, 수명이 다했으면 스스로 반납한다
        protected virtual void Update()
        {
            if (_released) return;

            Move();

            if (Time.time >= _spawnTime + _lifetime) ReleaseSelf();
        }

        // 기본 투사체는 Launch 에서 속도를 한 번 주고 그대로 직진하므로 할 일이 없다.
        // 부메랑처럼 매 프레임 경로를 바꾸는 무기가 이걸 재정의한다.
        protected virtual void Move() { }

        // 트리거 콜라이더끼리 처음 겹친 순간 유니티가 부른다.
        // 대상 태그인지, 이미 때린 적인지 확인한 뒤 피해를 주고 관통 횟수를 깎는다.
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (_released) return;
            if (!other.CompareTag(_targetTag)) return;

            if (_hitTargets.Contains(other)) return;
            _hitTargets.Add(other);

            other.GetComponent<IDamagable>()?.TakeDamage(_damage);

            // 관통이 0 이면 여기서 끝. 남아 있으면 하나 쓰고 계속 날아간다
            if (_pierceLeft <= 0) ReleaseSelf();
            else _pierceLeft--;
        }

        // 풀로 돌아간다. 풀이 없으면(테스트 씬 등) 그냥 파괴한다.
        // _released 로 두 번 반납되는 것을 막는다. 같은 오브젝트가 풀에 두 번 들어가면
        // 나중에 두 곳에서 동시에 꺼내 쓰는 심각한 버그가 된다.
        protected void ReleaseSelf()
        {
            if (_released) return;
            _released = true;

            _rb.linearVelocity = Vector2.zero;

            if (ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
            else Destroy(gameObject);
        }

        #region IPoolable
        // ObjectPool 이 풀에서 꺼낸 직후(SetActive(true) 뒤) 부른다.
        // Awake/Start 는 최초 1회뿐이라 재사용 시 초기화는 여기서 한다
        public virtual void OnSpawnFromPool()
        {
            _released = false;
            _hitTargets.Clear();
        }

        // ObjectPool 이 풀로 돌려놓기 직전(SetActive(false) 전) 부른다.
        // 속도를 0 으로 해 두어야 다음에 꺼냈을 때 Launch 전에 엉뚱한 방향으로 한 프레임 미끄러지지 않는다
        public virtual void OnReturnToPool()
        {
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }
        #endregion
    }
}
