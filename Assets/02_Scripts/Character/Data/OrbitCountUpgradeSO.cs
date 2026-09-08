using UnityEngine;
using DungeonMaster.Character.Player;

[CreateAssetMenu(menuName = "DungeonMaster/Upgrade/공전 무기 개수")]
public class OrbitCountUpgradeSO : UpgradeSO
{
    [SerializeField] private int _amountPerLevel = 1;

    public override void Apply(RoguelikePlayer player, int level)
    {
        player.Orbit.AddCount(_amountPerLevel);
    }
}
