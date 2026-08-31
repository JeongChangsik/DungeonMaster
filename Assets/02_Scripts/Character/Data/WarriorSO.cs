using UnityEngine;

[CreateAssetMenu(fileName = "WarriorSO", menuName = "DungeonMaster/WarriorSO")]
public class WarriorSO : ScriptableObject
{
    [Header("전사 기본 스탯")]
    public float maxHp = 150f;
    public float moveSpeed = 4f;
    public float attackDamage = 25f;
    public float attackCooldown = 0.7f;
    public float defense = 10f;
}
