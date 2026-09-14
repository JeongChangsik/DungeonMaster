using UnityEngine;
using DungeonMaster.Character.Player;

// 경험치 코인. 오브젝트 풀에서 재사용된다.
// IPoolable 은 반드시 프리팹 루트에 있어야 한다(ObjectPool.Spawn 이 GetComponent 로 찾음).
//
// [하는 일] 적이 죽을 때 떨구는 경험치 코인. 플레이어가 가까이 오면 끌려가고, 닿으면 경험치를 주고 사라진다.
// [붙이는 곳] ExpCoin 프리팹의 루트. Rigidbody2D 와 트리거 콜라이더가 같이 있어야 한다.
// [연결] RoguelikeEnemy.DropCoin 이 ObjectPool.Spawn 으로 꺼낸다.
//        끌려오는 범위는 RoguelikePlayer.PickupRadius 에서 읽고, 경험치는 RoguelikePlayer.AddExp 로 넘긴다.
// [설계] 범위를 코인이 그때그때 플레이어에게 물어본다.
//        그래서 "픽업 범위 증가" 업그레이드를 찍어도 코인 쪽은 고칠 것이 없다.
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

    // 풀에서 나온 개체인지. 씬에 직접 놓인 코인은 Release 하면 안 사라진다
    private bool _fromPool;

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
        // 한 번에 여러 개를 줍는 일이 잦아서 간격을 넉넉히 준다
        AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.coinPickupSFX : null, 0.15f, 0.15f);

        // 풀 출신이면 창고로 반납, 씬에 직접 놓인 코인이면 파괴
        _released = true;
        if (_fromPool && ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
        else Destroy(gameObject);
    }

    #region IPoolable
    // Awake/Start 는 최초 1회뿐이므로 재사용 시 초기화는 여기서 한다
    // 지난번에 끌려가던 상태, 붙은 속도가 남아 있으면 떨어지자마자 날아가 버린다
    public void OnSpawnFromPool()
    {
        _fromPool = true;
        _isChasing = false;
        _released = false;
        _currentSpeed = 1.0f;

        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        // 씬 재시작 대비. 파괴된 옛 플레이어를 물고 있으면 안 된다
        if (_player == null) FindPlayer();
    }

    // ObjectPool.Release 가 창고에 넣기 직전에 부른다. 남은 속도만 지워 둔다
    public void OnReturnToPool()
    {
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
    }
    #endregion
}
