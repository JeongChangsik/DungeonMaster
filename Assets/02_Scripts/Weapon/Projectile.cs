using System.Collections.Generic;
using DungeonMaster.Core;
using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 직진하는 투사체. 오브젝트 풀에서 재사용된다.
    //
    // 필요한 컴포넌트
    //  - Rigidbody2D : Kinematic (물리에 밀리면 안 되고, 트리거 콜백은 받아야 함)
    //  - Collider2D  : Is Trigger 체크
    //
    // IPoolable 은 반드시 프리팹 루트에 있어야 한다.
    // ObjectPool.Spawn 이 GetComponent<IPoolable>() 로 찾기 때문(InChildren 아님).
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour, IPoolable
    {
        [Tooltip("이 시간이 지나면 아무것도 못 맞혀도 풀로 돌아간다")]
        [SerializeField] private float _lifetime = 3f;

        protected Rigidbody2D _rb;
        protected float _damage;
        protected int _pierceLeft;
        protected float _spawnTime;

        // 이미 반납했는지. 관통 투사체가 같은 프레임에 두 적을 맞히면
        // Release 이후에도 그 프레임의 나머지 OnTriggerEnter2D 가 계속 들어온다.
        protected bool _released;

        // 관통 중 같은 적을 여러 번 때리지 않도록 기록.
        // Unity 6 에서 GetInstanceID() 가 폐기되어 콜라이더 참조를 그대로 담는다.
        // 투사체 수명이 짧아서 참조를 들고 있어도 문제되지 않는다.
        protected readonly HashSet<Collider2D> _hitTargets = new HashSet<Collider2D>();

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        // 발사. 스포너가 위치를 잡아준 뒤 호출한다.
        // ObjectPool.Spawn 은 position 만 설정하고 rotation 은 건드리지 않으므로 여기서 직접 맞춘다.
        public virtual void Launch(Vector2 direction, float damage, float speed, int pierce, float angleOffset)
        {
            _damage = damage;
            _pierceLeft = pierce;
            _spawnTime = Time.time;
            _released = false;
            _hitTargets.Clear();

            Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);

            _rb.linearVelocity = dir * speed;
        }

        protected virtual void Update()
        {
            if (_released) return;

            Move();

            if (Time.time >= _spawnTime + _lifetime) ReleaseSelf();
        }

        // 기본 투사체는 Launch 에서 속도를 한 번 주고 그대로 직진하므로 할 일이 없다.
        // 부메랑처럼 매 프레임 경로를 바꾸는 무기가 이걸 재정의한다.
        protected virtual void Move() { }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (_released) return;
            if (!other.CompareTag("Enemy")) return;

            if (_hitTargets.Contains(other)) return;
            _hitTargets.Add(other);

            other.GetComponent<IDamagable>()?.TakeDamage(_damage);

            if (_pierceLeft <= 0) ReleaseSelf();
            else _pierceLeft--;
        }

        protected void ReleaseSelf()
        {
            if (_released) return;
            _released = true;

            _rb.linearVelocity = Vector2.zero;

            if (ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
            else Destroy(gameObject);
        }

        #region IPoolable
        // Awake/Start 는 최초 1회뿐이라 재사용 시 초기화는 여기서 한다
        public virtual void OnSpawnFromPool()
        {
            _released = false;
            _hitTargets.Clear();
        }

        public virtual void OnReturnToPool()
        {
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }
        #endregion
    }
}
