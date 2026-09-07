using UnityEngine;
using DungeonMaster.Character.Player;

public class Coin : MonoBehaviour
{
    [SerializeField] private int _expAmount = 10;

    [Header("자석")]
    [SerializeField] private float _magnetRadius = 3f;
    [SerializeField] private float _acceleration = 25f;   // _moveSpeed 대신

    private float _currentSpeed = 1.0f;

    private Rigidbody2D _rb;
    private Transform _player;

    // 한 번 끌려가기 시작하면 다시 멀어져도 계속 쫓아감
    // 이게 없으면 경계선 근처에서 붙었다 떨어졌다 하며 덜덜 떨림
    private bool _isChasing;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _player = GameObject.FindWithTag("PLAYER")?.transform;
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        if (!_isChasing)
        {
            if ((_player.position - transform.position).sqrMagnitude > _magnetRadius * _magnetRadius) return;
            _isChasing = true;
        }

        // 처음엔 스멀스멀 끌려오다가 점점 빨라져서 훅 빨려 들어감
        // 비행 거리가 짧아서(최대 _magnetRadius) 상한을 둘 필요가 없음
        _currentSpeed += _acceleration * Time.fixedDeltaTime;
        _rb.MovePosition(Vector2.MoveTowards(_rb.position, _player.position, _currentSpeed * Time.fixedDeltaTime));
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PLAYER")) return;

        other.GetComponent<RoguelikePlayer>()?.AddExp(_expAmount);
        Destroy(gameObject);
    }
}
