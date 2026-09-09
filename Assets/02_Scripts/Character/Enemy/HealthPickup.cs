using DungeonMaster.Character.Player;
using UnityEngine;

// 적이 가끔 떨구는 회복 아이템. 오브젝트 풀에서 재사용된다.
// IPoolable 은 반드시 프리팹 루트에 있어야 한다(ObjectPool.Spawn 이 GetComponent 로 찾음).
//
// 경험치 코인과 달리 **자석으로 끌려오지 않는다.**
// 저절로 딸려오면 그냥 시간이 지나면 회복되는 것과 같아서 아무 결정도 아니게 된다.
// 직접 걸어가서 주워야 "적 사이를 뚫고 갈 만한가"를 판단하게 된다.
public class HealthPickup : MonoBehaviour, IPoolable
{
    [Header("회복량")]
    [Tooltip("최대 체력의 몇 %를 회복하는가")]
    [Range(0.05f, 1f)] [SerializeField] private float _healPercent = 0.3f;
    [Tooltip("% 로 계산한 값이 이보다 작으면 이 값만큼 회복한다")]
    [SerializeField] private float _minHeal = 20f;

    [Header("수명")]
    [Tooltip("이 시간이 지나면 사라진다. 안 그러면 못 주운 아이템이 맵에 계속 쌓인다")]
    [SerializeField] private float _lifetime = 30f;
    [Tooltip("사라지기 전 이 시간 동안 깜빡인다")]
    [SerializeField] private float _blinkDuration = 5f;
    [SerializeField] private float _blinkInterval = 0.15f;

    private SpriteRenderer _spriteRenderer;
    private float _spawnTime;
    private bool _released;
    private bool _fromPool;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spawnTime = Time.time;
    }

    private void Update()
    {
        float age = Time.time - _spawnTime;

        if (age >= _lifetime)
        {
            Remove();
            return;
        }

        // 사라지기 직전에 깜빡여서 "곧 없어진다"를 알린다
        if (_spriteRenderer == null) return;

        float remain = _lifetime - age;
        if (remain > _blinkDuration)
        {
            if (!_spriteRenderer.enabled) _spriteRenderer.enabled = true;
            return;
        }

        _spriteRenderer.enabled = Mathf.FloorToInt(remain / _blinkInterval) % 2 == 0;
    }

    // Enter 가 아니라 Stay 를 쓴다.
    // Enter 로 하면 "체력이 꽉 찬 채로 하트를 밟고 그 자리에 서 있다가 맞은" 경우
    // 들어오는 순간이 이미 지나가서 영영 못 줍는다. 실제로 테스트에서 이 상황이 나왔다.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (_released) return;
        if (!other.CompareTag("PLAYER")) return;

        RoguelikePlayer player = other.GetComponent<RoguelikePlayer>();
        if (player == null) return;

        // 체력이 꽉 찼으면 먹지 않고 그대로 둔다.
        // 지나가다 낭비되면 아까우니까
        if (player.CurrHp >= player.MaxHp) return;

        player.Heal(Mathf.Max(_minHeal, player.MaxHp * _healPercent));
        AudioManager.Play(AudioManager.Data != null ? AudioManager.Data.itemPickupSFX : null, 0.1f);

        Remove();
    }

    // 풀 출신이면 반납, 아니면 파괴.
    // 씬에 직접 놓인 것을 Release 하면 풀이 모르는 오브젝트라 그냥 화면에 남는다
    private void Remove()
    {
        _released = true;

        if (_spriteRenderer != null) _spriteRenderer.enabled = true;

        if (_fromPool && ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
        else Destroy(gameObject);
    }

    #region IPoolable
    // Awake/Start 는 최초 1회뿐이므로 재사용 시 초기화는 전부 여기서 한다.
    // 수명을 다시 재지 않으면 두 번째부터는 나오자마자 사라진다
    public void OnSpawnFromPool()
    {
        _fromPool = true;
        _released = false;
        _spawnTime = Time.time;

        if (_spriteRenderer != null) _spriteRenderer.enabled = true;
    }

    public void OnReturnToPool()
    {
        if (_spriteRenderer != null) _spriteRenderer.enabled = true;
    }
    #endregion
}
