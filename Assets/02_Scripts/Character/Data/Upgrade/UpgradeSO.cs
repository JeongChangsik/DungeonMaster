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

    // 지금 이 카드가 후보에 오를 수 있는가.
    // 아직 해금되지 않은 무기의 "강화" 카드가 뽑히는 걸 막는 용도.
    // 기본은 항상 후보에 오른다.
    public virtual bool IsAvailable(RoguelikePlayer player) => true;

    // level: 이번에 적용될 레벨 (1부터)
    public abstract void Apply(RoguelikePlayer player, int level);
}
