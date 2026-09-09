using UnityEngine;
using DungeonMaster.Character.Player;

[CreateAssetMenu(menuName = "DungeonMaster/Upgrade/공전 무기 공격력")]
public class OrbitDamageUpgradeSO : UpgradeSO
{
    [SerializeField] private float _amountPerLevel = 5f;

    public override void Apply(RoguelikePlayer player, int level)
    {
        player.Orbit.AddDamage(_amountPerLevel);
    }
}
