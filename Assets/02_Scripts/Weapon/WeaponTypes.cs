namespace DungeonMaster.Weapon
{
    // 무기 종류. 카드 SO가 "어떤 무기를" 강화할지 고르는 데 쓴다.
    // 새 무기를 추가할 때 여기에 한 줄 추가하면 된다.
    // ※ 뒤에만 추가할 것. 중간에 끼워 넣으면 이미 만들어둔 .asset 의 선택값이 밀린다.
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
    public enum WeaponStat
    {
        Damage,     // 피해량
        Count,      // 개수 (칼날 수 / 발사 수)
        Range,      // 범위 (궤도 반경 / 사거리 / 오라 반경)
        Rate,       // 속도 (회전 속도 / 연사 속도 / 틱 속도)
        Pierce,     // 관통 (투사체 전용)
    }
}
