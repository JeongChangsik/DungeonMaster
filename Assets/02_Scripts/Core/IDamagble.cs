namespace DungeonMaster.Core
{
    // [하는 일] "맞을 수 있는 것"이라는 약속(인터페이스)이다. 이 약속을 지키는 클래스는 TakeDamage 를 꼭 가져야 한다.
    // [붙이는 곳] 컴포넌트가 아니라서 오브젝트에 붙이지 않는다. 클래스 이름 뒤에 ": IDamagable" 로 적는다.
    // [연결] Player, Enemy(구 GamePlay 씬), RoguelikeEnemy 가 이 약속을 지킨다.
    //        때리는 쪽(OrbitWeapon, Projectile, Bomb, WeaponAura, 적의 몸통 박치기 등)은
    //        GetComponent<IDamagable>() 로 찾아서 TakeDamage 만 부른다.
    // [설계] 인터페이스는 "무엇을 할 수 있는지"만 적은 목록이고, "어떻게 하는지"는 각 클래스가 정한다.
    //        그래서 칼날은 상대가 플레이어인지 적인지 몰라도 된다. "맞을 수 있냐?"만 물어보면 된다.
    //        예를 들어 로그라이크 전사는 방어력과 무적 시간을 자기 TakeDamage 안에서 알아서 처리한다.
    // 참고: 파일 이름(IDamagble)과 인터페이스 이름(IDamagable)의 철자가 조금 다르다.
    //       MonoBehaviour 가 아니라서 이름이 달라도 동작에는 문제가 없다.
    public interface IDamagable
    {
        void TakeDamage(float damage);
    }
}