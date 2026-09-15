using DungeonMaster.Character.Player;
using UnityEngine;

public class WarriorAttackHandler : MonoBehaviour
{
    private Warrior _warrior;

    void Start()
    {
        // _warrior = transform.parent.parent.GetComponent<Warrior>();
        _warrior = transform.root.GetComponent<Warrior>();
    }

    public void OnAttackAnimEvent()
    {
        _warrior?.OnAttackAnimEvent();
    }

}
