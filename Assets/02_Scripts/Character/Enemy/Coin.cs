using UnityEngine;
using DungeonMaster.Character.Player;

// 경험치 코인. 오브젝트 풀에서 재사용된다.
// IPoolable 은 반드시 프리팹 루트에 있어야 한다(ObjectPool.Spawn 이 GetComponent 로 찾음).
public class Coin : MonoBehaviour, IPoolable
{
    [SerializeField] private int _expAmount = 10;

    [Header("자석")]
    [SerializeField] private float _magnetRadius = 3f;
    [SerializeField] private float _acceleration = 25f;   // _moveSpeed 대신

    private float _currentSpeed = 1.0f;

    private Rigidbody2D _rb;
    private Transform _player;
    // 자석 범위를 플레이어에게서 읽어오기 위해 컴포넌트 자체를 캐싱한다.
    // (픽업 범위 업그레이드가 코인 하나하나를 찾아다니지 않아도 되도록)
    private RoguelikePlayer _playerRef;

    // 한 번 끌려가기 시작하면 다시 멀어져도 계속 쫓아감
    // 이게 없으면 경계선 근처에서 붙었다 떨어졌다 하며 덜덜 떨림
    private bool _isChasing;

    // 이미 반납했는지. 먹은 프레임에 트리거가 또 들어와도 중복 반납되지 않게
    private bool _released;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        FindPlayer();
    }

    // Start 는 최초 1회뿐이라 풀에서 재사용될 때는 안 불린다.
    // 씬을 다시 시작하면 옛 플레이어가 파괴되므로 매번 다시 잡아야 한다.
    private void FindPlayer()
    {
        GameObject go = GameObject.FindWithTag("PLAYER");
        if (go == null) return;

        _player = go.transform;
        _playerRef = go.GetComponent<RoguelikePlayer>();
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        if (!_isChasing)
        {
            // 플레이어가 있으면 강화된 픽업 범위를 쓰고, 없으면 인스펙터 기본값으로 폴백
            float radius = _playerRef != null ? _playerRef.PickupRadius : _magnetRadius;

            if ((_player.position - transform.position).sqrMagnitude > radius * radius) return;
            _isChasing = true;
        }

        // 처음엔 스멀스멀 끌려오다가 점점 빨라져서 훅 빨려 들어감
        // 비행 거리가 짧아서(최대 _magnetRadius) 상한을 둘 필요가 없음
        _currentSpeed += _acceleration * Time.fixedDeltaTime;
        _rb.MovePosition(Vector2.MoveTowards(_rb.position, _player.position, _currentSpeed * Time.fixedDeltaTime));
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_released) return;
        if (!other.CompareTag("PLAYER")) return;

        other.GetComponent<RoguelikePlayer>()?.AddExp(_expAmount);

        _released = true;
        if (ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
        else Destroy(gameObject);
    }

    #region IPoolable
    // Awake/Start 는 최초 1회뿐이므로 재사용 시 초기화는 여기서 한다
    public void OnSpawnFromPool()
    {
        _isChasing = false;
        _released = false;
        _currentSpeed = 1.0f;

        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        // 씬 재시작 대비. 파괴된 옛 플레이어를 물고 있으면 안 된다
        if (_player == null) FindPlayer();
    }

    public void OnReturnToPool()
    {
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
    }
    #endregion
}
