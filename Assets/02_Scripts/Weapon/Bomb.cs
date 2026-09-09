using System.Collections;
using System.Collections.Generic;
using DungeonMaster.Core;
using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 바닥에 놓이는 폭탄. 심지가 타는 동안 기다렸다가 범위 피해로 터진다.
    //
    // 다른 무기와 역할이 다르다.
    //  - 공전 칼날 / 화염 오라 : 플레이어 몸 주변만
    //  - 투척 단검 / 부메랑     : 한 방향으로 날아감
    //  - 폭탄                   : 그 자리에 남아서 "지나갈 곳"을 미리 막는다
    //
    // 심지 시간이 있어서 즉발이 아니지만, 그만큼 한 방이 세다.
    public class Bomb : MonoBehaviour, IPoolable
    {
        [Header("심지")]
        [Tooltip("놓인 뒤 터질 때까지 걸리는 시간")]
        [SerializeField] private float _fuse = 1.2f;
        [Tooltip("심지가 타는 동안 순서대로 보여줄 그림. 마지막 칸이 터지기 직전 모습")]
        [SerializeField] private Sprite[] _fuseFrames;

        [Header("폭발")]
        [SerializeField] private float _radius = 2.5f;
        [SerializeField] private LayerMask _enemyLayer;
        [Tooltip("폭발 원이 커졌다 사라지는 시간")]
        [SerializeField] private float _flashDuration = 0.25f;
        [Tooltip("폭발 원 그림. 비워두면 소리와 피해만 들어간다")]
        [SerializeField] private SpriteRenderer _flash;

        private SpriteRenderer _spriteRenderer;
        private float _damage;
        private bool _exploded;
        private bool _fromPool;

        // 매번 새로 만들면 GC 가 계속 쌓인다
        private static readonly List<Collider2D> _hits = new List<Collider2D>(64);
        private ContactFilter2D _filter;
        private bool _filterReady;

        public float Radius { get { return _radius; } }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // WeaponBomb 이 놓는 즉시 호출한다
        public void Arm(float damage, float radius, float fuse)
        {
            _damage = damage;
            _radius = radius;
            _fuse = fuse;

            StopAllCoroutines();
            StartCoroutine(FuseCo());
        }

        private IEnumerator FuseCo()
        {
            _exploded = false;

            if (_flash != null)
            {
                _flash.enabled = false;
                _flash.transform.localScale = Vector3.zero;
            }
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;

            float elapsed = 0f;
            while (elapsed < _fuse)
            {
                elapsed += Time.deltaTime;

                // 심지가 타들어가는 것을 그림으로 보여준다.
                // 이게 없으면 언제 터지는지 알 수 없어서 그냥 운이 된다
                if (_fuseFrames != null && _fuseFrames.Length > 0 && _spriteRenderer != null)
                {
                    int idx = Mathf.Clamp(
                        Mathf.FloorToInt(elapsed / _fuse * _fuseFrames.Length),
                        0, _fuseFrames.Length - 1);
                    _spriteRenderer.sprite = _fuseFrames[idx];
                }

                yield return null;
            }

            Explode();
        }

        private void Explode()
        {
            if (_exploded) return;
            _exploded = true;

            if (!_filterReady)
            {
                _filter = new ContactFilter2D();
                _filterReady = true;
            }
            // 적 콜라이더가 전부 트리거라 이걸 켜지 않으면 아무도 안 잡힌다
            _filter.useTriggers = true;
            _filter.useLayerMask = true;
            _filter.SetLayerMask(_enemyLayer);
            _filter.useDepth = false;

            _hits.Clear();
            Physics2D.OverlapCircle(transform.position, _radius, _filter, _hits);

            for (int i = 0; i < _hits.Count; i++)
            {
                if (_hits[i] == null) continue;
                IDamagable target = _hits[i].GetComponent<IDamagable>();
                if (target != null) target.TakeDamage(_damage);
            }

            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.bombExplodeSFX : null, 0.15f);

            StartCoroutine(FlashCo());
        }

        // 폭탄 그림은 감추고, 폭발 원이 커지면서 옅어진다
        private IEnumerator FlashCo()
        {
            if (_spriteRenderer != null) _spriteRenderer.enabled = false;

            if (_flash == null)
            {
                Release();
                yield break;
            }

            _flash.enabled = true;

            // 원형 스프라이트의 지름이 폭발 반경의 2배가 되도록 맞춘다
            float spriteDiameter = _flash.sprite != null ? _flash.sprite.bounds.size.x : 1f;
            if (spriteDiameter <= 0.0001f) spriteDiameter = 1f;
            float targetScale = (_radius * 2f) / spriteDiameter;

            Color baseColor = _flash.color;
            float elapsed = 0f;
            while (elapsed < _flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _flashDuration);

                // 순식간에 커졌다가 서서히 옅어진다
                _flash.transform.localScale = Vector3.one * (targetScale * Mathf.Sqrt(t));

                Color c = baseColor;
                c.a = baseColor.a * (1f - t);
                _flash.color = c;

                yield return null;
            }

            _flash.color = baseColor;
            _flash.enabled = false;
            _flash.transform.localScale = Vector3.zero;

            Release();
        }

        // 풀 출신이면 반납, 아니면 파괴.
        // 씬에 직접 놓인 것을 Release 하면 풀이 모르는 오브젝트라 화면에 그대로 남는다
        private void Release()
        {
            if (_fromPool && ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
            else Destroy(gameObject);
        }

        #region IPoolable
        public void OnSpawnFromPool()
        {
            _fromPool = true;
            _exploded = false;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = true;
                if (_fuseFrames != null && _fuseFrames.Length > 0) _spriteRenderer.sprite = _fuseFrames[0];
            }
            if (_flash != null)
            {
                _flash.enabled = false;
                _flash.transform.localScale = Vector3.zero;
            }
        }

        public void OnReturnToPool()
        {
            StopAllCoroutines();

            // 터지는 도중에 반납되면 다음에 나올 때 투명하거나 커진 채로 나온다
            if (_flash != null)
            {
                _flash.enabled = false;
                _flash.transform.localScale = Vector3.zero;
            }
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
