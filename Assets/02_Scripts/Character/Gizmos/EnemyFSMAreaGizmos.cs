using UnityEngine;

public class EnemyFSMAreaGizmos : MonoBehaviour
{
    [Header("기본 스탯")]
    [SerializeField] protected EnemySO _enemySO;

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.aquamarine;
        Gizmos.DrawWireSphere(transform.position, _enemySO.chaseDistance);  // 3d로 그림

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _enemySO.attackDistance);
    }
}
