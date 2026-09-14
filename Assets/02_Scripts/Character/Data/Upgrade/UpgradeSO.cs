using UnityEngine;
using DungeonMaster.Character.Player;

// 업그레이드 한 종류를 나타내는 데이터 + 적용 로직
// abstract이므로 CreateAssetMenu를 달지 않는다(에셋으로 만들 수 없음)
//
// [하는 일] 레벨업 카드 한 장의 공통 틀이다. 카드 이름/설명/그림/최대 레벨을 들고,
//          "뽑혀도 되는가(IsAvailable)"와 "고르면 무슨 일이 생기나(Apply)"를 약속한다.
// [붙이는 곳] 오브젝트에 붙이지 않는다. ScriptableObject 는 씬 밖에 파일(.asset)로 저장되는 데이터 묶음이다.
//          이 클래스는 abstract 라 직접 에셋을 못 만들고, 자식 클래스(WeaponUpgradeSO 등)로
//          Project 창 우클릭 > Create > DungeonMaster > Upgrade 메뉴에서 만든다.
// [연결] 만든 에셋은 씬의 LevelUpUI 인스펙터 _pool(카드 후보 목록)이나 _startingUpgrades(시작 장비)에 넣어야 등장한다.
//          LevelUpUI 가 IsAvailable 로 후보를 거르고, 카드를 고르면 Apply 를 부른다. LevelUpCard 가 Title/Description/Image 를 보여준다.
// [설계] 카드마다 할 일은 다르지만 LevelUpUI 는 "UpgradeSO" 하나로만 다룬다.
//          그래서 새 종류의 카드는 이 클래스를 물려받아 Apply 만 채우면 되고, LevelUpUI 는 고칠 필요가 없다.
//          (abstract = "틀만 있고 내용은 자식이 채워라", override = 자식이 그 내용을 채우는 것)
//          단, 대부분의 카드는 새 .cs 없이 WeaponUpgradeSO / PlayerStatUpgradeSO 에셋을 하나 더 만드는 것으로 충분하다.
//          카드별 현재 레벨은 이 에셋이 아니라 LevelUpUI 가 들고 있다(에셋에 저장하면 플레이를 멈춰도 값이 남기 때문).
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
    // LevelUpUI 가 카드를 뽑기 직전에, 최대 레벨 검사와 함께 후보마다 물어본다.
    public virtual bool IsAvailable(RoguelikePlayer player) => true;

    // level: 이번에 적용될 레벨 (1부터)
    public abstract void Apply(RoguelikePlayer player, int level);
}
