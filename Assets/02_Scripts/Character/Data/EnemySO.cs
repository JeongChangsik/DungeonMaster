using UnityEngine;

[CreateAssetMenu(fileName = "EnemySO", menuName = "DungeonMaster/EnemySO", order = 0)]
public class EnemySO : ScriptableObject
{
    [Header("기본 스탯")]
    public float maxHp = 100f;
    public float moveSpeed = 1f;
    public float chaseDistance = 5f;
    public float attackDistance = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
}
