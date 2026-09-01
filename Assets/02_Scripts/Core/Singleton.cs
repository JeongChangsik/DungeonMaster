using UnityEngine;

// public class Singleton<T> : MonoBehaviour where T : MonoBehaviour    // 이것도 됨
// 이 클래스를 직접 사용하지 말고 상속하여 사용하도록 abstract 추가
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                // 씬에서 이미 존재하는지 찾아봄 (인스펙터에 미리 배치해둔 경우 대응)
                // FindObjectOfType -> 이건 deplecated 삭제된 함수임
                // instance = FindObjectOfType<T>();

                // FindObjectsInactive.Include => 비활성화된 오브젝트를 포함해서 검색할 수 있음
                instance = FindAnyObjectByType<T>(FindObjectsInactive.Include);

                if (instance == null)
                {
                    // 없으면(씬에 존재하지 않으면) 새로 만들어줌 (편의성, 하지만 남용은 주의)
                    Debug.Log($"typeof.name: {typeof(T).Name}");
                    Debug.Log($"nameof: {nameof(T)}");

                    GameObject obj = new GameObject(typeof(T).Name);
                    instance = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        // 싱글턴 인스턴스가 이미 존재한 상태에서 처리해야할 것
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            // instance != this 추가하는 이유
            // 씬을 왔다갔을 때, Entity ID가 서로 다르게 생성됨. 
            // 하지만 instance의 값은 static이라 값이 유지되어 버릴 수 있음
            Destroy(gameObject); // 중복 생성 방지
            return;
        }
    }

    protected virtual void OnDestroy()
    {
        // 싱글턴 인스턴스가 파괴될 때 null 초기화
        if(instance == this) instance = null;
    }
}