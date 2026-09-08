using UnityEngine;
using DungeonMaster.Character.Player;

// 업그레이드 한 종류를 나타내는 데이터 + 적용 로직
// abstract이므로 CreateAssetMenu를 달지 않는다(에셋으로 만들 수 없음)
public abstract class UpgradeSO : ScriptableObject
{
    [Header("카드 표시")]
    public string Title;
    [TextArea] public string Description;
    public Sprite Image;

    [Header("규칙")]
    [Min(1)] public int MaxLevel = 5;

    // level: 이번에 적용될 레벨 (1부터)
    public abstract void Apply(RoguelikePlayer player, int level);
}
