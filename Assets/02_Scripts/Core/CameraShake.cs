using Unity.Cinemachine;
using UnityEngine;

// [하는 일] 화면을 순간적으로 흔든다. 플레이어가 맞거나, 공격이 적에게 맞았을 때 쓴다.
// [붙이는 곳] 씬에 직접 배치하지 않아도 된다(지금도 씬에는 없다). 처음 CameraShake.Instance 를 부르는 순간
//          Singleton<T> 가 "CameraShake" 라는 오브젝트를 새로 만들고 이 컴포넌트를 붙인다.
// [연결] RoguelikePlayer(피격), RoguelikeWarrior / Warrior(공격 적중)가 CameraShake.Instance.Shake() 를 부른다.
//        실제로 흔들리려면 Cinemachine 카메라 쪽에 CinemachineImpulseListener(충격을 받아 흔들리는 쪽)가 있어야 한다.
// [설계] 카메라 위치를 직접 움직이지 않고, Cinemachine 의 Impulse(충격 신호)를 보낸다.
//        CinemachineImpulseSource 는 "충격을 보내는 쪽"이다. 없으면 Awake 에서 자동으로 붙인다.
public class CameraShake : Singleton<CameraShake>
{
    private CinemachineImpulseSource _impulseSource;

    protected override void Awake()
    {
        base.Awake();

        // TryGetComponent로 컴포넌트가 있으면 가져오는 게 가능함.
        // or TryGetComponent<CinemachineImpulseSource>(out _impulseSource);
        if(!TryGetComponent(out _impulseSource))
        {
            _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        }

        _impulseSource.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        _impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;
    }

    // 쉐이크를 생성하는 메서드
    public void Shake(float force = 0.5f)
    {
        /* 난수 발생
         * Random.Range(0, 10) => 0, 1, ..., 9 (정수)
         * Random.Range(0.0f, 10.0f) => 0.0f, 0.1f, ..., 10.0f (실수)
         */

        var velocity = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
        velocity = velocity * force;
        _impulseSource.GenerateImpulse(velocity);
    }
}
