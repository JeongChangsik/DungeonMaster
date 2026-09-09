using System.Collections.Generic;
using UnityEngine;

// 풀에서 꺼내지고 반납될 때 알림을 받고 싶은 오브젝트가 구현하는 인터페이스
// Awake/Start는 최초 1회만 호출되므로, 재사용 시 초기화는 여기서 처리해야 함
public interface IPoolable
{
    // 풀에서 꺼내진 직후 호출 (HP, 상태, 속도 등 초기화)
    void OnSpawnFromPool();

    // 풀로 반납되기 직전 호출 (코루틴 정지, 참조 해제 등 정리)
    void OnReturnToPool();
}

public class ObjectPool : Singleton<ObjectPool>
{
    // 인스펙터에서 프리팹별 초기 개수를 설정하기 위한 클래스
    // [System.Serializable]을 붙여야 클래스가 인스펙터에 노출됨
    [System.Serializable]
    public class PoolConfig
    {
        public GameObject prefab;
        public int initialSize = 10;
    }

    [Header("미리 생성해 둘 풀 목록")]
    [SerializeField] private List<PoolConfig> _configs = new List<PoolConfig>();

    // 프리팹 -> 대기 중인(비활성) 인스턴스 큐
    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();

    // 인스턴스 -> 어떤 프리팹에서 나왔는지 역방향 조회용
    // Release()에서 어느 큐로 돌려보낼지 알아내려면 이 정보가 필요함
    private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new Dictionary<GameObject, GameObject>();

    #region 유니티 생명주기
    protected override void Awake()
    {
        // Singleton<T>의 Awake에서 instance 등록 + DontDestroyOnLoad 처리
        base.Awake();

        foreach (var config in _configs)
        {
            Prewarm(config.prefab, config.initialSize);
        }
    }
    #endregion

    #region 공개 메서드
    // 지정한 개수만큼 미리 만들어 둠(게임 중 Instantiate로 인한 프레임 끊김 방지)
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null)
        {
            Debug.LogError($"ObjectPool::Prewarm() prefab이 비어 있습니다.");
            return;
        }

        Queue<GameObject> queue = GetQueue(prefab);
        for (int i = 0; i < count; i++)
        {
            queue.Enqueue(CreateInstance(prefab));
        }
    }

    // 풀에서 꺼내서 지정 위치에 활성화
    // 대기 중인 것이 없으면 새로 만들어 늘림(상한 없음)
    public GameObject Spawn(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogError($"ObjectPool::Spawn() prefab이 비어 있습니다.");
            return null;
        }

        Queue<GameObject> queue = GetQueue(prefab);
        GameObject instance = queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab);

        instance.transform.position = position;
        instance.SetActive(true);

        // SetActive(true) 이후에 호출해야 함
        // 비활성 상태에서는 StartCoroutine이 실패하기 때문
        instance.GetComponent<IPoolable>()?.OnSpawnFromPool();

        return instance;
    }

    // 풀로 반납(파괴하지 않고 비활성화만)
    public void Release(GameObject instance)
    {
        if (instance == null) return;

        // 이 풀에서 나온 오브젝트가 아니면 반납받지 않음
        // 그냥 큐에 넣으면 다른 프리팹이 섞여서 추적 불가능한 버그가 됨
        if (!_instanceToPrefab.TryGetValue(instance, out GameObject prefab))
        {
            Debug.LogWarning($"ObjectPool::Release() {instance.name}은(는) 풀에서 생성된 오브젝트가 아닙니다.");
            return;
        }

        // 이미 반납된 오브젝트를 또 반납하면 큐에 같은 인스턴스가 두 번 들어감
        // -> 서로 다른 두 곳에서 동시에 같은 오브젝트를 쓰게 되는 심각한 버그
        if (!instance.activeSelf) return;

        instance.GetComponent<IPoolable>()?.OnReturnToPool();
        instance.SetActive(false);

        _pools[prefab].Enqueue(instance);
    }

    // 화면에 나와 있는 것을 전부 창고로 되돌린다.
    //
    // 이 창고는 DontDestroyOnLoad 라서 씬을 다시 불러와도 살아남는다.
    // 정리하지 않고 재시작하면 죽기 직전의 적과 코인이 새 판에 그대로 남아 있게 된다.
    public void ReleaseAllActive()
    {
        foreach (KeyValuePair<GameObject, GameObject> pair in _instanceToPrefab)
        {
            GameObject instance = pair.Key;
            if (instance == null || !instance.activeSelf) continue;

            instance.GetComponent<IPoolable>()?.OnReturnToPool();
            instance.SetActive(false);
            _pools[pair.Value].Enqueue(instance);
        }
    }
    #endregion

    #region 내부 메서드
    private Queue<GameObject> GetQueue(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefab] = queue;
        }
        return queue;
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        // 풀 오브젝트의 자식으로 생성 -> 하이어라키가 정리됨
        GameObject instance = Instantiate(prefab, transform);
        instance.name = prefab.name;    // Instantiate는 이름 뒤에 "(Clone)"을 붙임

        instance.SetActive(false);
        _instanceToPrefab[instance] = prefab;

        return instance;
    }
    #endregion
}
