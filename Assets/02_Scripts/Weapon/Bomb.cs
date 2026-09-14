using System.Collections;
using System.Collections.Generic;
using DungeonMaster.Core;
using UnityEngine;

namespace DungeonMaster.Weapon
{
    // 바닥에 놓이는 폭탄. 심지가 타는 동안 기다렸다가 범위 피해로 터진다.
    //
    // [하는 일] 놓이면 심지 그림을 한 칸씩 바꾸며 기다리다가, 시간이 되면 둥근 범위 안의 적을 모두 때린다.
    //          그 뒤 폭발 원이 커지며 옅어지는 연출을 보여주고 창고(풀)로 돌아간다.
    // [붙이는 곳] Bomb 프리팹의 루트. 루트에 SpriteRenderer(폭탄 그림)가 있어야 하고,
    //          자식 Flash 오브젝트의 SpriteRenderer 를 _flash 칸에 넣는다.
    // [연결] WeaponBomb 이 풀에서 꺼낸 직후 Arm() 을 불러 불을 붙인다.
    //          ObjectPool 이 꺼낼 때 OnSpawnFromPool(), 돌려받을 때 OnReturnToPool() 을 부른다(IPoolable).
    //          적은 IDamagable.TakeDamage() 로 때리고, 터지는 소리는 AudioManager 로 낸다.
    // [설계] 다른 무기와 역할이 다르다.
    //  - 공전 칼날 / 화염 오라 : 플레이어 몸 주변만
    //  - 투척 단검 / 부메랑     : 한 방향으로 날아감
    //  - 폭탄                   : 그 자리에 남아서 "지나갈 곳"을 미리 막는다
    //
    // 심지 시간이 있어서 즉발이 아니지만, 그만큼 한 방이 세다.
    //
    // 날아가는 무기(Projectile)는 부딪히는 순간(OnTriggerEnter2D)을 기다리지만,
    // 폭탄은 부딪힐 일이 없다. 그래서 터지는 순간에 "지금 이 원 안에 누가 있나"를 한 번 물어보는
    // Physics2D.OverlapCircle 방식을 쓴다. 콜라이더를 달고 기다리는 것보다 간단하고, 딱 한 번만 계산한다.
    //
    // 기다리는 일은 코루틴(IEnumerator + yield)으로 한다.
    // 코루틴은 "한 프레임 쉬고 이어서 하기"를 할 수 있는 함수라서, 타이머 변수를 Update 에 흩어놓지 않아도 된다.
    public class Bomb : MonoBehaviour, IPoolable
    {
        // 아래 _fuse 와 _radius 는 인스펙터 값이 있지만, 실제 게임에서는 Arm() 이 WeaponBomb 의 값으로 덮어쓴다.
        // 여기 적힌 값은 프리팹만 따로 시험해 볼 때와 기즈모 표시용이라고 보면 된다
        [Header("심지")]
        [Tooltip("놓인 뒤 터질 때까지 걸리는 시간")]
        [SerializeField] private float _fuse = 1.2f;
        [Tooltip("심지가 타는 동안 순서대로 보여줄 그림. 마지막 칸이 터지기 직전 모습")]
        [SerializeField] private Sprite[] _fuseFrames;

        [Header("폭발")]
        [SerializeField] private float _radius = 2.5f;
        // 적이 있는 레이어만 골라 검사한다. 비워두면(Nothing) 아무도 안 맞는다
        [SerializeField] private LayerMask _enemyLayer;
        [Tooltip("폭발 원이 커졌다 사라지는 시간")]
        [SerializeField] private float _flashDuration = 0.25f;
        [Tooltip("폭발 원 그림. 비워두면 소리와 피해만 들어간다")]
        [SerializeField] private SpriteRenderer _flash;

        // 폭탄 본체 그림. 심지 프레임을 바꿔 끼우고, 터질 때 숨긴다
        private SpriteRenderer _spriteRenderer;
        private float _damage;
        // 이미 터졌는가. 한 폭탄이 두 번 피해를 주지 않게 막는 안전장치
        private bool _exploded;
        // 풀에서 꺼낸 폭탄인가. 끝났을 때 "반납"할지 "파괴"할지 고르는 데 쓴다
        private bool _fromPool;

        // 매번 새로 만들면 GC 가 계속 쌓인다.
        // (GC = 안 쓰는 메모리를 치우는 청소부. 쓰레기가 많으면 청소하느라 게임이 순간 멈춘다)
        // static 이라 모든 폭탄이 이 리스트 하나를 같이 쓴다. 폭발 검사는 한 번에 하나씩 끝나므로 겹치지 않는다
        private static readonly List<Collider2D> _hits = new List<Collider2D>(64);
        // 겹침 검사 조건(어느 레이어, 트리거 포함 여부). 한 번 만들어 두고 재사용한다
        private ContactFilter2D _filter;
        private bool _filterReady;

        public float Radius { get { return _radius; } }

        // Awake 는 오브젝트가 처음 만들어질 때 딱 한 번만 불린다.
        // 풀에서 다시 꺼낼 때는 불리지 않으므로, 여기서는 "한 번만 해도 되는 일"(컴포넌트 찾기)만 한다
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // WeaponBomb 이 놓는 즉시 호출한다.
        // 불을 붙이는 함수. 강화가 반영된 피해량/반경/심지 시간을 받아 저장하고 심지 타이머를 시작한다
        public void Arm(float damage, float radius, float fuse)
        {
            _damage = damage;
            _radius = radius;
            _fuse = fuse;

            // 혹시 이전 사용 때 돌던 타이머가 남아 있으면 끄고 새로 시작한다(타이머가 둘이면 두 번 터진다)
            StopAllCoroutines();
            StartCoroutine(FuseCo());
        }

        // 심지 타이머. 매 프레임 조금씩 시간을 재며 심지 그림을 바꾸고, 다 타면 Explode() 를 부른다.
        // Time.deltaTime 은 시간 정지(timeScale 0) 중에는 0 이라서, 레벨업 카드를 고르는 동안 심지도 멈춘다
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
                    // 흐른 비율(0~1)을 그림 개수만큼 나눠서 몇 번째 그림인지 고른다.
                    // 마지막 프레임에 비율이 1을 살짝 넘어도 배열 밖으로 나가지 않게 Clamp 로 묶는다
                    int idx = Mathf.Clamp(
                        Mathf.FloorToInt(elapsed / _fuse * _fuseFrames.Length),
                        0, _fuseFrames.Length - 1);
                    _spriteRenderer.sprite = _fuseFrames[idx];
                }

                // "여기서 한 프레임 쉬고, 다음 프레임에 이 줄 다음부터 이어서 한다"는 뜻
                yield return null;
            }

            Explode();
        }

        // 폭발. 반경 안의 적을 한 번에 찾아 모두 피해를 주고, 소리를 내고, 폭발 연출을 시작한다
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

            // 원 안에 걸친 콜라이더를 전부 _hits 에 담아 달라고 물리 엔진에 부탁한다.
            // 결과를 새 배열로 받지 않고 미리 만들어 둔 리스트에 채우게 해서 쓰레기를 만들지 않는다
            _hits.Clear();
            Physics2D.OverlapCircle(transform.position, _radius, _filter, _hits);

            for (int i = 0; i < _hits.Count; i++)
            {
                if (_hits[i] == null) continue;
                // 적 종류가 달라도 IDamagable(맞을 수 있는 것) 약속만 지키면 똑같이 때릴 수 있다
                IDamagable target = _hits[i].GetComponent<IDamagable>();
                if (target != null) target.TakeDamage(_damage);
            }

            // 뒤의 두 숫자는 소리 크기와 음 높이를 매번 조금씩 흔드는 양이다.
            // 폭탄 여러 개가 연달아 터져도 똑같은 소리가 기계적으로 반복되지 않게 한다
            AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.bombExplodeSFX : null, 0.15f, 0.2f);

            StartCoroutine(FlashCo());
        }

        // 폭탄 그림은 감추고, 폭발 원이 커지면서 옅어진다.
        // 피해는 Explode() 에서 이미 들어갔고, 이건 눈으로 보여주기만 하는 연출이다. 끝나면 Release() 로 정리한다
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

                // 순식간에 커졌다가 서서히 옅어진다.
                // 크기는 Sqrt(t) 라서 처음에 확 커지고 끝으로 갈수록 느려진다(펑 하는 느낌).
                // 투명도는 t 에 그대로 비례해서 일정한 속도로 사라진다
                _flash.transform.localScale = Vector3.one * (targetScale * Mathf.Sqrt(t));

                Color c = baseColor;
                c.a = baseColor.a * (1f - t);
                _flash.color = c;

                yield return null;
            }

            // 색을 원래대로 돌려놓는다. 안 그러면 다음에 재사용할 때 처음부터 투명한 채로 나온다
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
        // IPoolable 은 "풀에서 꺼낼 때 / 돌려놓을 때 알려줘"라는 약속(인터페이스)이다.
        // 풀에서 꺼낸 폭탄은 지난번에 터졌던 모습 그대로일 수 있어서, 여기서 새 폭탄처럼 되돌린다.

        // 풀에서 꺼낸 직후 ObjectPool 이 부른다(이 다음에 WeaponBomb 이 Arm 을 부른다).
        // 첫 심지 그림으로 되돌리고 폭발 원은 숨긴다
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

        // 풀로 돌려놓기 직전 ObjectPool 이 부른다.
        // 다 터져서 돌아올 때뿐 아니라, 게임 재시작(ReleaseAllActive)으로 심지가 타는 도중에 거둬질 때도 불린다.
        // 그래서 돌던 타이머를 꼭 멈춘다. 안 멈추면 창고 안에서 혼자 터질 수 있다
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

        // 에디터에서 선택했을 때 씬 뷰에 폭발 반경을 원으로 그려준다(게임 화면에는 안 보인다)
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
