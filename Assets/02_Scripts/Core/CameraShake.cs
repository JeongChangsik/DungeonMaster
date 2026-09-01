using Unity.Cinemachine;
using UnityEngine;

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

        Debug.Log($"화면흔들림 : {velocity}");
    }
}
