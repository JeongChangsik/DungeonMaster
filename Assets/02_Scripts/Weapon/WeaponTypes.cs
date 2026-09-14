namespace DungeonMaster.Weapon
{
    // 무기 이름표와 스탯 이름표를 모아 둔 파일. 컴포넌트가 아니라서 어디에도 붙이지 않는다.
    //
    // [하는 일] "어떤 무기의 어떤 값을 올릴까"를 숫자 대신 이름으로 고를 수 있게 한다.
    // [연결] WeaponUpgradeSO 인스펙터의 드롭다운(_target, _stat)에 이 목록이 나온다.
    //        WeaponBase.Id 가 WeaponId 를, WeaponBase.AddStat 이 WeaponStat 을 받는다.
    // [설계] 문자열("Orbit")로 고르면 오타가 나도 컴파일러가 못 잡는다. enum 은 오타가 나면 바로 빨간 줄이 뜬다.

    // 무기 종류. 카드 SO가 "어떤 무기를" 강화할지 고르는 데 쓴다.
    // 새 무기를 추가할 때 여기에 한 줄 추가하면 된다.
    // ※ 뒤에만 추가할 것. 중간에 끼워 넣으면 이미 만들어둔 .asset 의 선택값이 밀린다.
    //   (유니티는 enum 을 이름이 아니라 순서 번호 0, 1, 2... 로 저장한다.
    //    Orbit 과 Projectile 사이에 새 값을 넣으면 1번이던 카드가 새 무기를 가리키게 된다.)
    public enum WeaponId
    {
        Orbit,          // 공전 칼날
        Projectile,     // 투척 단검
        Aura,           // 화염 오라
        Boomerang,      // 부메랑 도끼
        Bomb,           // 폭탄
    }

    // 무기의 어떤 값을 올릴 것인가.
    // 무기마다 의미가 조금씩 다르다(아래는 공전 칼날 기준)
    // 그 무기에 없는 스탯을 받으면 조용히 무시된다(예: 오라에 Pierce). 각 무기의 AddStat switch 참고.
    // ※ WeaponId 와 같은 이유로 뒤에만 추가할 것.
    public enum WeaponStat
    {
        Damage,     // 피해량
        Count,      // 개수 (칼날 수 / 발사 수)
        Range,      // 범위 (궤도 반경 / 사거리 / 오라 반경)
        Rate,       // 속도 (회전 속도 / 연사 속도 / 틱 속도)
        Pierce,     // 관통 (투사체 전용)
    }
}
