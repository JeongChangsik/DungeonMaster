using UnityEngine;

public class Test : MonoBehaviour
{
    Vector2 _position;


    // Awake -> OnEnable -> Start
    // 객체가 로딩(생성)될 때 한 번 호출
    // 스크립트가 유니티 엔진에서 비활성화 처리되어도 실행됨(중요!)
    void Awake()
    {
        // 전역 게임 데이터 초기화
        Debug.Log("Awake() 호출");
    }

    void OnEnable()
    {
        // 스크립트가 활성화될 때마다 매 번 호출
        Debug.Log("OnEnable() 호출");

    }
    // OnDisable()도 있음

    // 스크립트가 실행할 때 한 번 호출된다.
    void Start()
    {
        // 자신의 클래스 내의 변수 초기화
        Debug.Log("Start() 호출");
    }

    // 매 프레임마다 호출되는 콜백(Callback function/method, Event function)
    // 화면을 렌더링하는 주기(평균 60 fps => 1/60 간격으로 호출하되 오차는 있음)
    void Update()
    {
        Debug.Log("Update() 호출");
    }

    // Update와의 차이는 고정된 주기를 가짐
    // 0.02f 초 간격 호출 (정확한 간격으로 호출됨)
    // 호출 주기 = 물리엔진의 계산주기
    void FixedUpdate()
    {
        Debug.Log("FixedUpdate() 호출");
        Debug.Log($"FixedUpdate() 호출 간격: {Time.fixedDeltaTime}");
        
    }

    // Update() 호출 후 호출됨. 그러므로 호출 주기는 Update()와 동일함
    void LateUpdate()
    {
        // Update()에서 선행 작업 결과 데이터를 바탕으로 후속 작업
        // 다른 클래스의 Update() 후의 결과를 이용할 때 자주 사용함
        Debug.Log("LateUpdate() 호출");
    }
}
