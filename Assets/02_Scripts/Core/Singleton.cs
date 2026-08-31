using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                // 씬에서 이미 존재하는지 찾아봄 (인스펙터에 미리 배치해둔 경우 대응)
                instance = FindObjectOfType<T>();

                if (instance == null)
                {
                    // 그래도 없으면 새로 만들어줌 (편의성, 하지만 남용은 주의)
                    GameObject obj = new GameObject(typeof(T).Name);
                    instance = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject); // 중복 생성 방지
        }
    }
}