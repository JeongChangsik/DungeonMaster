# DungeonMaster 프로젝트 기반 — C# / Unity 예상 시험 문제

> **출제 범위:** 이 프로젝트(`Assets/02_Scripts`)에 실제로 등장한 문법과 개념만으로 구성했습니다.
> **문항 수:** 객관식·단답 100문항 + 서술형 15문항 + 디버깅 10문항 + 실기 5문항
> **정답과 해설은 문서 맨 아래**에 있습니다. 먼저 풀고 채점하세요.
> **답 작성:** 객관식은 `**➡ 답:** (   )` 의 괄호 **안에** 번호만 적으세요(예: `( 2 )`). 서술형·디버깅·실기는 아래 코드 블록 안에 작성하세요.

---

## 📋 출제 범위 체크리스트

풀기 전에 각 항목을 "남에게 설명할 수 있는지" 스스로 체크해보세요.

### C# 기본
- [ ] 접근 제한자 (`public` / `private` / `protected` / `internal`)
- [ ] `class` vs `struct` (참조 타입 vs 값 타입, 힙 vs 스택)
- [ ] 상속, `base`, 생성자 호출 순서
- [ ] `abstract` 클래스 / `abstract` 메서드 / `virtual` / `override`
- [ ] `interface` 와 다중 구현
- [ ] 프로퍼티, 표현식 본문 멤버(`=>`)
- [ ] `static`, `readonly`, `const`
- [ ] 문자열 보간(`$""`), `namespace`, 캐스팅, `typeof`
- [ ] `out` 변수, 널 조건부 연산자(`?.`)

### C# 심화
- [ ] 제네릭 메서드와 `where` 제약 조건
- [ ] `delegate` / `event` / `Action` / 멀티캐스트
- [ ] 구독(`+=`)과 해제(`-=`), 이벤트로 인한 메모리 누수
- [ ] 람다식, LINQ (`OrderBy`, `First`)
- [ ] `Dictionary<TKey,TValue>`, `TryGetValue`, 컬렉션/인덱스 초기화
- [ ] `IDisposable`, `using` 지시문/문/선언

### Unity 기본
- [ ] 생명주기(Lifecycle) 순서와 각 함수의 역할
- [ ] `MonoBehaviour`, 컴포넌트, `GetComponent` 캐싱
- [ ] 어트리뷰트: `[SerializeField]` `[Header]` `[RequireComponent]` `[CreateAssetMenu]` `[CustomEditor]`
- [ ] `ScriptableObject`
- [ ] `Rigidbody2D`, `Collider2D`, `Physics2D`, `LayerMask`
- [ ] `Animator`, `StringToHash`, 애니메이션 파라미터
- [ ] `Gizmos`, 에디터 확장(`Editor`, `OnInspectorGUI`)
- [ ] `Time.time` / `deltaTime` / `fixedDeltaTime`
- [ ] New Input System (`InputAction`, phase, `CallbackContext`)

### 설계 패턴
- [ ] 유한 상태 머신(FSM)
- [ ] 어댑터(Adapter) 계층 — `InputHandler`
- [ ] 데이터와 로직의 분리 — `ScriptableObject`

---

# Part 1. C# 기본 문법 (Q1 ~ Q30)

**Q1.** `Player.cs`에서 `protected float _maxHp;`로 선언했다. `protected`의 의미로 옳은 것은?
1. 같은 클래스 안에서만 접근 가능
2. 같은 클래스와 **상속받은 자식 클래스**에서 접근 가능
3. 같은 어셈블리 안에서만 접근 가능
4. 모든 곳에서 접근 가능

**➡ 답:** ( 2 )

**Q2.** 다음 코드에서 `_isDead`는 무엇인가?
```csharp
protected bool _isDead => _currHp <= 0f;
```
1. 필드(Field)
2. 메서드(Method)
3. 읽기 전용 프로퍼티 (표현식 본문 멤버)
4. 상수(const)

**➡ 답:** ( 1 )

**Q3.** Q2의 `_isDead`에 대한 설명 중 **틀린** 것은?
1. `get` 접근자만 있고 `set`은 없다
2. 값을 저장하는 별도 메모리 공간이 없다
3. 접근할 때마다 `_currHp <= 0f`를 새로 계산한다
4. `_isDead = true;` 처럼 값을 대입할 수 있다

**➡ 답:** (  )

**Q4.** `public abstract class Player : MonoBehaviour, IDamagable` 에 대한 설명 중 옳은 것은?
1. `Player` 컴포넌트를 게임오브젝트에 직접 붙일 수 있다
2. `new Player()` 로 인스턴스를 만들 수 있다
3. `Player`를 상속한 `Warrior` 같은 자식 클래스만 실제로 사용할 수 있다
4. `abstract` 클래스는 필드를 가질 수 없다

**➡ 답:** (   )

**Q5.** `abstract` 메서드와 `virtual` 메서드의 차이로 옳은 것은?
1. `abstract`는 본문이 없고 자식이 **반드시** 재정의해야 한다 / `virtual`은 본문이 있고 재정의는 선택이다
2. `abstract`는 본문이 있고 `virtual`은 없다
3. 둘 다 `override` 없이 재정의할 수 있다
4. 차이가 없다

**➡ 답:** (   )

**Q6.** `Warrior.cs`의 코드에서 `base.TakeDamage(actualDamage);`의 역할은?
```csharp
public override void TakeDamage(float damage)
{
    float actualDamage = Mathf.Max(1f, damage - _defense);
    base.TakeDamage(actualDamage);
}
```
1. 자기 자신을 재귀 호출한다
2. 부모 클래스(`Player`)의 `TakeDamage`를 호출한다
3. 아무 일도 하지 않는다
4. 자식 클래스의 메서드를 호출한다

**➡ 답:** (   )

**Q7.** Q6에서 `base.TakeDamage(damage)`가 아니라 `base.TakeDamage(actualDamage)`를 넘긴 이유는?
1. 실수다
2. 방어력을 뺀 **실제 데미지**를 부모의 체력 감소 로직에 전달하기 위해
3. `damage`는 읽기 전용이라서
4. 둘 다 같은 값이라서

**➡ 답:** (   )

**Q8.** `Warrior.Awake()`에서 `base.Awake()`를 **호출하지 않으면** 어떤 일이 벌어지는가? (`Player.Awake()`는 컴포넌트를 캐싱한다)
1. 컴파일 에러가 난다
2. `_rb`, `_animator`, `_inputHandler` 등이 `null`로 남아 `NullReferenceException`이 발생한다
3. Unity가 자동으로 부모의 `Awake`를 대신 호출해준다
4. 아무 문제 없다

**➡ 답:** (   )

**Q9.** `IDamagable` 인터페이스에 대한 설명 중 **틀린** 것은?
```csharp
public interface IDamagable { void TakeDamage(float damage); }
```
1. 구현하는 클래스는 `TakeDamage`를 반드시 제공해야 한다
2. 인터페이스 멤버는 기본적으로 `public`이다
3. 인터페이스는 여러 개를 동시에 구현할 수 있다
4. 인터페이스는 인스턴스 필드(변수)를 가질 수 있다

**➡ 답:** (   )

**Q10.** `class A : B, IC, ID` 에서 `B`는 무엇이며 어떤 규칙이 있는가?
1. 인터페이스이며 순서는 상관없다
2. 기저(부모) 클래스이며 반드시 맨 앞에 와야 한다
3. 제네릭 파라미터
4. 네임스페이스

**➡ 답:** (   )

**Q11.** `InputHandler.cs` 주석에 따르면 `Vector2`는 구조체(struct)다. 구조체 설명으로 옳은 것은?
1. 참조 타입이며 힙(Heap)에 저장된다
2. 값 타입이며 주로 스택(Stack)에 저장되고 클래스 상속이 불가능하다
3. 항상 `null`을 대입할 수 있다
4. `new` 없이는 절대 사용할 수 없다

**➡ 답:** (   )

**Q12.** 다음 중 **참조 타입(Reference type)**이 아닌 것은?
1. `Transform`
2. `Collider2D[]`
3. `Vector2`
4. `string`

**➡ 답:** (   )

**Q13.** `protected static readonly int hashIsWalk = Animator.StringToHash("IsWalk");`
여기서 `static readonly` 대신 `const`를 쓸 수 **없는** 이유는?
1. `int`는 `const`가 될 수 없다
2. `const`는 컴파일 타임에 값이 확정되어야 하는데 `StringToHash()`는 런타임에 실행된다
3. `static`과 `const`는 같이 쓸 수 없다
4. `readonly`가 `const`보다 빠르다

**➡ 답:** (   )

**Q14.** `static` 멤버에 대한 설명으로 옳은 것은?
1. 인스턴스마다 각각 하나씩 만들어진다
2. 클래스당 하나만 존재하며 모든 인스턴스가 공유한다
3. 절대 상속되지 않는다
4. 반드시 `readonly`여야 한다

**➡ 답:** (   )

**Q15.** `hashIsWalk`를 `static`으로 선언해서 얻는 이점은?
1. 적이 100마리여도 해시 계산은 딱 한 번만 수행된다
2. 인스턴스마다 다른 값을 가질 수 있다
3. 인스펙터에 노출된다
4. 값을 자유롭게 바꿀 수 있다

**➡ 답:** (   )

**Q16.** `Debug.Log($"피해: {actualDamage}");` 에서 `$`의 의미는?
1. 정규표현식
2. 문자열 보간(String Interpolation) — `{}` 안의 식을 값으로 치환
3. 서식 문자열 이스케이프
4. 달러 기호 출력

**➡ 답:** (   )

**Q17.** `namespace DungeonMaster.Character.Enemy` 를 사용하는 주된 이유는?
1. 실행 속도가 빨라진다
2. 이름 충돌 방지 및 코드의 논리적 분류
3. 메모리를 절약한다
4. 폴더 구조와 반드시 일치해야 하기 때문

**➡ 답:** (   )

**Q18.** `EnemyEditor.cs`의 `Enemy enemy = (Enemy)target;` 에서 `(Enemy)`는?
1. 생성자 호출
2. 명시적 형변환(캐스팅)
3. 제네릭 지정
4. 어트리뷰트

**➡ 답:** (   )

**Q19.** 캐스팅에 실패했을 때 예외 대신 `null`을 반환하는 연산자는?
1. `is`
2. `as`
3. `typeof`
4. `nameof`

**➡ 답:** (   )

**Q20.** 다음 코드에서 `out IState state`의 의미로 옳은 것은?
```csharp
if (_states.TryGetValue(typeof(T), out IState state))
```
1. `state`의 값을 인자로 전달한다
2. 메서드가 `state`에 값을 채워서 돌려주며, 여기서는 변수 선언까지 동시에 한다
3. `state`는 읽기 전용이다
4. `state`는 호출 전에 반드시 초기화해야 한다

**➡ 답:** (   )

**Q21.** `_stateMachine?.ChangeState(state);` 에서 `?.` 의 동작은?
1. `_stateMachine`이 `null`이면 예외를 던진다
2. `_stateMachine`이 `null`이면 호출을 건너뛰고 `null`을 반환한다
3. `_stateMachine`을 `null`로 만든다
4. `_stateMachine`이 `null`이면 새로 생성한다

**➡ 답:** (   )

**Q22.** `OnMoveAction?.Invoke(ctx.ReadValue<Vector2>());` 에서 `?.`가 **반드시 필요한** 이유는?
1. 이벤트는 항상 null이기 때문
2. 구독자가 한 명도 없으면 델리게이트가 `null`이라, 그냥 `Invoke`하면 `NullReferenceException`이 발생
3. 성능 최적화
4. 컴파일 에러 회피

**➡ 답:** (   )

**Q23.** `typeof(IdleState)`가 반환하는 것은?
1. `IdleState` 인스턴스
2. `System.Type` 객체 — 그 타입의 "설계도 정보"
3. 문자열 `"IdleState"`
4. `bool`

**➡ 답:** (   )

**Q24.** `Swampy.cs`의 다음 문법의 이름은?
```csharp
_states = new Dictionary<Type, IState>
{
    [typeof(IdleState)] = new IdleState(),
    [typeof(ChaseState)] = new ChaseState(),
};
```
1. 컬렉션 초기화자(Collection Initializer)
2. 인덱스 초기화자(Index Initializer)
3. 객체 초기화자(Object Initializer)
4. 익명 타입

**➡ 답:** (   )

**Q25.** Q24의 코드를 `{ { typeof(IdleState), new IdleState() }, ... }` 형태로 바꾸면 어떤 방식이며, 키가 중복되면?
1. 컬렉션 초기화자 — 내부적으로 `Add()`를 호출하므로 키 중복 시 `ArgumentException` 발생
2. 인덱스 초기화자 — 조용히 덮어씀
3. 문법 오류
4. 동일하게 동작

**➡ 답:** (   )

**Q26.** 딕셔너리에 인덱서로 `d[key] = value;` 를 했는데 이미 같은 키가 있으면?
1. 예외가 발생한다
2. 조용히 값을 덮어쓴다
3. 무시된다
4. 두 개가 모두 저장된다

**➡ 답:** (   )

**Q27.** `Enemy.cs` 주석에 나오는 `1 << 8` 의 값은?
1. 8
2. 16
3. 256
4. 128

**➡ 답:** (   )

**Q28.** `LayerMask`에서 비트 시프트를 쓰는 이유는?
1. 레이어 번호를 문자열로 바꾸기 위해
2. 32개 레이어를 32비트 정수의 각 비트(on/off)로 표현하기 위해
3. 속도를 빠르게 하기 위해
4. 유니티의 규칙일 뿐 의미는 없다

**➡ 답:** (   )

**Q29.** `public StateMachine(Enemy enemy) { _enemy = enemy; }` 는 무엇인가?
1. 메서드
2. 생성자(Constructor) — 클래스명과 같고 반환형이 없다
3. 프로퍼티
4. 소멸자

**➡ 답:** (   )

**Q30.** `var atk = EditorGUILayout.FloatField(...);` 에서 `var`에 대한 설명으로 옳은 것은?
1. 타입이 런타임에 결정된다(동적 타입)
2. 컴파일러가 우변을 보고 타입을 추론하며, 한 번 정해지면 바뀌지 않는다
3. 어떤 타입이든 대입할 수 있다
4. `object`와 완전히 같다

**➡ 답:** (   )

---

# Part 2. C# 심화 — 제네릭 · 델리게이트 · LINQ · 리소스 (Q31 ~ Q55)

**Q31.** `public void ChangeState<T>() where T : IState` 에서 `<T>`는?
1. 배열 크기
2. 제네릭 타입 매개변수 — 호출할 때 실제 타입이 정해진다
3. 어트리뷰트
4. 반환 타입

**➡ 답:** (   )

**Q32.** `where T : IState` 제약 조건의 효과는?
1. `T`는 반드시 `IState`를 구현한 타입이어야 한다
2. `T`는 `IState` 그 자체여야 한다
3. `T`는 값 타입이어야 한다
4. 제약이 없다

**➡ 답:** (   )

**Q33.** 다음 중 제네릭 제약 조건이 **아닌** 것은?
1. `where T : class`
2. `where T : new()`
3. `where T : struct`
4. `where T : static`

**➡ 답:** (   )

**Q34.** `ChangeState<IdleState>()` 방식이 `ChangeState(new IdleState())` 방식보다 나은 점은?
1. 딕셔너리에 캐싱된 상태 인스턴스를 재사용하므로 매 전환마다 `new` 할당이 없어 GC 부담이 줄어든다
2. 코드가 더 길어진다
3. 실행 속도가 항상 2배 빨라진다
4. 아무 차이 없다

**➡ 답:** (   )

**Q35.** `public event Action<Vector2> OnMoveAction;` 에서 `Action<Vector2>`는?
1. 반환값이 `Vector2`인 델리게이트
2. `Vector2` 하나를 매개변수로 받고 **반환값이 없는(void)** 델리게이트
3. 클래스
4. 인터페이스

**➡ 답:** (   )

**Q36.** 반환값이 **있는** 델리게이트를 나타내는 내장 타입은?
1. `Action`
2. `Func`
3. `Predicate`만 가능
4. `EventHandler`

**➡ 답:** (   )

**Q37.** `Func<int, string, bool>` 에서 반환 타입은?
1. `int`
2. `string`
3. `bool`
4. `void`

**➡ 답:** (   )

**Q38.** `event` 키워드를 붙인 델리게이트와 그냥 델리게이트 필드(public)의 차이는?
1. 차이 없다
2. `event`는 외부에서 `+=` / `-=`만 가능하고, 대입(`=`)이나 외부 `Invoke`는 막힌다
3. `event`가 더 빠르다
4. `event`는 static이어야 한다

**➡ 답:** (   )

**Q39.** "멀티캐스트 델리게이트"의 의미는?
1. 하나의 델리게이트에 여러 메서드를 연결할 수 있고, 호출하면 전부 순서대로 실행된다
2. 여러 스레드에서 동시에 실행된다
3. 네트워크로 전송된다
4. 반환값을 여러 개 받는다

**➡ 답:** (   )

**Q40.** `Player.OnEnable()`에서 `+=`로 구독하고 `OnDisable()`에서 `-=`로 해제한다. 해제를 **하지 않으면**?
1. 컴파일 에러
2. 이벤트 발행자가 구독자 참조를 계속 붙잡아 GC 회수 실패(메모리 누수) + 파괴된 객체의 메서드 호출 위험
3. 아무 문제 없다
4. 이벤트가 자동으로 해제된다

**➡ 답:** (   )

**Q41.** 구독을 `Awake()`가 아니라 `OnEnable()`에서 하는 이유는?
1. `Awake`가 더 느려서
2. `Awake`는 평생 한 번만 호출되므로, 컴포넌트를 껐다 켜면 `OnDisable`에서 해제만 되고 재구독이 안 돼 입력이 영구히 먹통이 되기 때문
3. `Awake`에서는 이벤트를 쓸 수 없어서
4. 유니티가 금지해서

**➡ 답:** (   )

**Q42.** "콜백(Callback) 함수"의 정의로 가장 적절한 것은?
1. 내가 직접 호출하는 함수
2. 내가 등록해두면 특정 시점에 **다른 쪽이 대신 불러주는** 함수
3. 재귀 호출되는 함수
4. `void` 반환 함수

**➡ 답:** (   )

**Q43.** `.OrderBy(c => (c.transform.position - transform.position).sqrMagnitude)` 에서 `c => ...` 는?
1. 람다식(Lambda) — 이름 없는 익명 함수
2. 제네릭
3. 형변환
4. 삼항 연산자

**➡ 답:** (   )

**Q44.** LINQ의 `First()`와 `FirstOrDefault()`의 차이는?
1. 차이 없다
2. `First()`는 요소가 없으면 `InvalidOperationException`을 던지고, `FirstOrDefault()`는 기본값(참조형이면 `null`)을 반환한다
3. `First()`가 더 느리다
4. `FirstOrDefault()`는 정렬을 수행한다

**➡ 답:** (   )

**Q45.** `Enemy.DetectPlayer()`에서 `if (colliders.Length > 0)` 검사를 먼저 하는 이유는?
1. 성능 때문
2. 빈 배열에 `.First()`를 호출하면 예외가 발생하기 때문
3. 문법상 필수
4. LINQ를 쓰기 위해

**➡ 답:** (   )

**Q46.** LINQ의 "지연 실행(Deferred Execution)"이란?
1. 즉시 결과를 계산한다
2. 실제로 열거(`foreach`, `First()`, `ToList()` 등)될 때까지 계산을 미룬다
3. 백그라운드 스레드에서 실행된다
4. 1프레임 뒤에 실행된다

**➡ 답:** (   )

**Q47.** 거리 비교에서 `magnitude` 대신 `sqrMagnitude`를 쓰는 이유는?
1. 값이 더 정확해서
2. 제곱근(√) 연산을 생략해 훨씬 빠르고, **크기 비교(정렬)만 할 때는 결과 순서가 동일**하기 때문
3. `magnitude`는 존재하지 않아서
4. 메모리를 적게 써서

**➡ 답:** (   )

**Q48.** `Vector2.Distance(a,b)`, `(a-b).magnitude`, `(a-b).sqrMagnitude` 중 실제 거리 **값**이 필요한 경우 쓸 수 **없는** 것은?
1. `Vector2.Distance`
2. `.magnitude`
3. `.sqrMagnitude`
4. 모두 사용 가능

**➡ 답:** (   )

**Q49.** `Dictionary<Type, IState>`에서 키로 조회할 때의 평균 시간 복잡도는?
1. O(1)
2. O(log n)
3. O(n)
4. O(n²)

**➡ 답:** (   )

**Q50.** `_states.TryGetValue(key, out value)` 를 `_states[key]` 대신 쓰는 이유는?
1. 더 빠르다
2. 키가 없을 때 예외(`KeyNotFoundException`) 대신 `false`를 반환하므로 안전하다
3. 값을 수정할 수 있다
4. 차이 없다

**➡ 답:** (   )

**Q51.** C#의 `using` 키워드 3가지 용법을 바르게 나열한 것은?
1. 지시문(namespace 가져오기) / 문(`using (x) { }`) / 선언(`using var x = ...;`)
2. 지시문 / 어트리뷰트 / 캐스팅
3. 네임스페이스 / 상속 / 인터페이스
4. import / include / require

**➡ 답:** (   )

**Q52.** `using (new EditorGUI.DisabledScope(...)) { ... }` 는 어떤 코드로 컴파일되는가?
1. `if` 문
2. `try { ... } finally { obj.Dispose(); }`
3. `while` 루프
4. `switch` 문

**➡ 답:** (   )

**Q53.** 위 `using` 문이 요구하는 인터페이스는?
1. `IEnumerable`
2. `IDisposable`
3. `IComparable`
4. `ICloneable`

**➡ 답:** (   )

**Q54.** `EditorGUI.DisabledScope`에서 `DisabledScope`는 무엇인가?
1. `EditorGUI`의 메서드
2. `EditorGUI` 안에 정의된 **중첩 클래스(Nested Class)**
3. 프로퍼티
4. 열거형

**➡ 답:** (   )

**Q55.** `GUI.enabled = false;` 로 직접 UI를 비활성화한 뒤 `true`로 되돌리지 않으면?
1. 컴파일 에러
2. `GUI.enabled`는 전역(static) 상태라, 그 뒤에 그려지는 **다른 모든 컴포넌트의 인스펙터까지** 회색으로 비활성화된다
3. 다음 프레임에 자동 복구된다
4. 아무 일도 없다

**➡ 답:** (   )

---

# Part 3. Unity 기초 (Q56 ~ Q90)

**Q56.** `Test.cs`에 정리된 유니티 생명주기 함수의 호출 순서로 옳은 것은?
1. `Start` → `Awake` → `OnEnable` → `Update`
2. `Awake` → `OnEnable` → `Start` → `FixedUpdate` → `Update` → `LateUpdate`
3. `OnEnable` → `Awake` → `Start` → `Update`
4. `Awake` → `Start` → `OnEnable` → `Update`

**➡ 답:** (   )

**Q57.** `Awake()`와 `Start()`의 가장 중요한 차이는?
1. `Awake`는 스크립트가 비활성화되어 있어도 호출되고, `Start`는 활성화된 경우에만 호출된다
2. `Start`가 먼저 호출된다
3. `Awake`는 매 프레임 호출된다
4. 차이 없다

**➡ 답:** (   )

**Q58.** "다른 오브젝트의 컴포넌트를 참조하는 초기화"는 어디에서 하는 것이 안전한가?
1. `Awake()` — 모든 오브젝트의 `Awake`가 끝났다는 보장이 없다
2. `Start()` — 모든 오브젝트의 `Awake`가 끝난 뒤 호출되므로 안전하다
3. `Update()`
4. 생성자

**➡ 답:** (   )

**Q59.** `OnEnable()` / `OnDisable()`의 호출 시점은?
1. 평생 한 번
2. 컴포넌트나 게임오브젝트가 활성화/비활성화될 때마다 매번
3. 매 프레임
4. 씬이 로드될 때만

**➡ 답:** (   )

**Q60.** `Update()`와 `FixedUpdate()`의 차이로 옳은 것은?
1. `Update`는 프레임마다(가변 간격), `FixedUpdate`는 고정 간격(기본 0.02초, 물리 연산 주기)
2. `FixedUpdate`가 항상 더 자주 호출된다
3. `Update`가 물리 연산용이다
4. 차이 없다

**➡ 답:** (   )

**Q61.** `Rigidbody2D`에 힘을 가하거나 속도를 설정하는 코드는 어디에 두는 것이 원칙인가?
1. `Update()`
2. `FixedUpdate()`
3. `LateUpdate()`
4. `Awake()`

**➡ 답:** (   )

**Q62.** `LateUpdate()`의 대표적 용도는?
1. 물리 연산
2. 모든 `Update`가 끝난 뒤의 후속 처리 (예: 카메라가 플레이어를 따라가기)
3. 초기화
4. 입력 처리

**➡ 답:** (   )

**Q63.** `Time.deltaTime`의 의미는?
1. 게임 시작 후 흐른 총 시간
2. **직전 프레임과 현재 프레임 사이에 걸린 시간(초)**
3. 항상 0.02초
4. 초당 프레임 수

**➡ 답:** (   )

**Q64.** `Time.deltaTime`을 곱해주는 이유는?
1. 값을 작게 만들려고
2. 프레임률이 달라도 **초당 이동량이 동일**하도록 만들기 위해(프레임 독립성)
3. 물리 엔진 요구사항
4. 필요 없다

**➡ 답:** (   )

**Q65.** `Player.OnAttack()`에서 쓰인 `Time.time`의 의미는?
1. 직전 프레임 소요 시간
2. 게임(또는 씬 로드) 시작 시점부터 지금까지 흐른 총 시간(초)
3. 시스템 현재 시각
4. 고정 delta

**➡ 답:** (   )

**Q66.** 다음 쿨다운 코드의 동작 설명으로 옳은 것은?
```csharp
if (Time.time >= lastAttackTime + _attackCooldown)
{
    lastAttackTime = Time.time;
    Attack();
}
```
1. 매 프레임 공격한다
2. 마지막 공격 후 `_attackCooldown`초가 지나야만 다시 공격한다
3. 공격이 한 번만 가능하다
4. `_attackCooldown`이 0이면 공격이 안 된다

**➡ 답:** (   )

**Q67.** `MonoBehaviour`를 상속한 클래스에 대해 **틀린** 것은?
1. `new`로 직접 생성하면 안 된다
2. `AddComponent<T>()` 또는 인스펙터로 게임오브젝트에 붙여서 사용한다
3. 생성자를 쓰는 대신 `Awake`/`Start`로 초기화한다
4. `MonoBehaviour`를 상속하지 않으면 어떤 클래스도 만들 수 없다

**➡ 답:** (   )

**Q68.** `GetComponent<Rigidbody2D>()`를 `Awake()`에서 한 번만 호출하고 필드에 저장하는 이유는?
1. `GetComponent`는 매번 컴포넌트를 탐색하므로 `Update`에서 반복 호출하면 성능이 나빠진다
2. `Update`에서는 호출이 금지되어 있다
3. 컴파일 에러가 나서
4. 코드가 짧아져서

**➡ 답:** (   )

**Q69.** `Player.cs` 주석에 나오듯 `GameObject.Find()`와 `transform.Find()`의 차이는?
1. 둘 다 동일하다
2. `GameObject.Find()`는 씬 전체(하이어라키 루트부터), `transform.Find()`는 해당 Transform의 자식에서 탐색
3. `transform.Find()`가 씬 전체를 검색한다
4. `GameObject.Find()`는 컴포넌트를 찾는다

**➡ 답:** (   )

**Q70.** `Find()` 계열을 `Update()`에서 쓰면 안 되는 이유는?
1. 컴파일 에러
2. 이름 기반 탐색이라 비용이 크고, 매 프레임 수행하면 심각한 성능 저하를 유발
3. 결과가 항상 null이라서
4. 유니티가 막아놔서

**➡ 답:** (   )

**Q71.** `[SerializeField] protected EnemySO _enemySO;` 에서 `[SerializeField]`의 역할은?
1. `private`/`protected` 필드를 **인스펙터에 노출**하고 직렬화(저장)한다
2. 필드를 `public`으로 바꾼다
3. 값을 상수로 만든다
4. 실행 속도를 높인다

**➡ 답:** (   )

**Q72.** `public` 필드 대신 `[SerializeField] private`를 권장하는 이유는?
1. 더 빠르다
2. 캡슐화를 유지하면서(외부 코드의 임의 접근은 막고) 인스펙터 편집만 허용하기 위해
3. `public`은 직렬화되지 않아서
4. 문법상 필수라서

**➡ 답:** (   )

**Q73.** `[Header("기본 스탯")]`의 역할은?
1. 인스펙터에 굵은 제목을 표시해 필드를 그룹화한다
2. 클래스 이름을 바꾼다
3. 값을 초기화한다
4. 컴파일러에 영향을 준다

**➡ 답:** (   )

**Q74.** `[RequireComponent(typeof(Rigidbody2D))]`의 효과는?
1. 이 스크립트를 오브젝트에 추가하면 `Rigidbody2D`가 자동으로 함께 추가되고, 임의 삭제가 방지된다
2. `Rigidbody2D`를 삭제한다
3. 런타임에 컴포넌트를 생성한다
4. 아무 효과 없다

**➡ 답:** (   )

**Q75.** `[CreateAssetMenu(fileName = "EnemySO", menuName = "DungeonMaster/EnemySO")]`의 역할은?
1. Project 창에서 우클릭 → Create 메뉴로 해당 `ScriptableObject` 에셋을 만들 수 있게 한다
2. 게임오브젝트를 생성한다
3. 씬을 만든다
4. 컴포넌트를 추가한다

**➡ 답:** (   )

**Q76.** `ScriptableObject`를 쓰는 가장 큰 이유는?
1. 데이터를 **에셋 파일**로 분리해 여러 오브젝트가 공유하고, 코드 수정 없이 밸런스를 조정할 수 있다
2. 실행 속도가 빨라져서
3. `MonoBehaviour`보다 기능이 많아서
4. 씬에 배치할 수 있어서

**➡ 답:** (   )

**Q77.** `ScriptableObject`에 대한 설명 중 **틀린** 것은?
1. 게임오브젝트에 컴포넌트로 붙일 수 없다
2. Project 창의 에셋으로 존재한다
3. `Awake`, `Update` 같은 `MonoBehaviour` 생명주기를 그대로 갖는다
4. 여러 프리팹이 같은 SO 에셋을 참조하면 데이터가 공유된다

**➡ 답:** (   )

**Q78.** 플레이 모드 중 `ScriptableObject`의 값을 수정하면 어떻게 되는가?
1. 플레이 종료 시 원래대로 돌아간다
2. **에디터에서는 변경이 그대로 남는다**(에셋에 기록됨) — 런타임 상태 저장용으로 쓰면 위험하다
3. 컴파일 에러
4. 무시된다

**➡ 답:** (   )

**Q79.** `_rb.linearVelocity = ctx * _moveSpeed;` — Unity 6에서 `velocity` 대신 `linearVelocity`를 쓰는 이유는?
1. 회전 속도(`angularVelocity`)와 구분하기 위해 이름이 변경되었고, `velocity`는 사용 중단(deprecated)되었다
2. 성능이 더 좋아서
3. 2D 전용이라서
4. 오타다

**➡ 답:** (   )

**Q80.** `Rigidbody2D`의 Body Type 중 **다른 물체에 밀리지 않으면서 스크립트로 움직이는** 타입은?
1. Dynamic
2. Kinematic
3. Static
4. Trigger

**➡ 답:** (   )

**Q81.** `Collider2D`의 `Is Trigger`를 켜면?
1. 물리적 충돌(밀림)이 사라지고 `OnTriggerEnter2D` 계열 콜백만 발생한다
2. 충돌이 두 배로 강해진다
3. 렌더링이 꺼진다
4. 아무 변화 없다

**➡ 답:** (   )

**Q82.** `Physics2D.OverlapCircleAll(point, radius, layerMask)`의 반환값은?
1. `bool`
2. `Collider2D` — 하나만
3. `Collider2D[]` — 조건에 맞는 것이 없으면 빈 배열
4. `null`

**➡ 답:** (   )

**Q83.** `Enemy.cs` 주석에 따르면 `OverlapCircleAll`의 단점은?
1. 반환할 때마다 배열 메모리가 할당되어 GC 오버헤드가 생길 수 있다
2. 항상 null을 반환한다
3. 3D에서만 쓸 수 있다
4. Rigidbody2D가 반드시 필요하다

**➡ 답:** (   )

**Q84.** `OverlapCircleAll`에 세 번째 인자로 `LayerMask`를 넘기는 이유는?
1. 원의 색을 정하려고
2. 특정 레이어(예: PLAYER)의 콜라이더만 검출하기 위해
3. 반지름을 정하려고
4. 필수 인자라서

**➡ 답:** (   )

**Q85.** `OnDrawGizmos()`와 `OnDrawGizmosSelected()`의 차이는?
1. `OnDrawGizmos`는 항상, `OnDrawGizmosSelected`는 해당 오브젝트가 선택되었을 때만 그려진다
2. 반대다
3. 차이 없다
4. `OnDrawGizmos`는 빌드에도 포함된다

**➡ 답:** (   )

**Q86.** `Gizmos.DrawWireSphere(transform.position, _enemySO.chaseDistance);` 를 쓰는 목적은?
1. 실제 게임 화면에 원을 렌더링한다
2. 씬 뷰에서 추적 범위를 **눈으로 확인**하기 위한 디버그 표시
3. 충돌 판정을 수행한다
4. 성능을 측정한다

**➡ 답:** (   )

**Q87.** `Animator.StringToHash("IsWalk")`를 미리 계산해두는 이유는?
1. `SetBool("IsWalk", ...)`처럼 문자열을 넘기면 내부에서 매번 해시 변환이 일어나 비용이 발생하기 때문
2. 문자열은 사용할 수 없어서
3. 해시가 더 정확해서
4. 유니티가 강제해서

**➡ 답:** (   )

**Q88.** Animator 파라미터 타입과 메서드의 짝이 **틀린** 것은?
1. Bool → `SetBool`
2. Trigger → `SetTrigger`
3. Float → `SetFloat`
4. Int → `SetBool`

**➡ 답:** (   )

**Q89.** `Warrior.OnAttackAnimEvent()` 같은 메서드를 만드는 이유(Animation Event)는?
1. 애니메이션 클립의 특정 프레임에서 이 메서드를 호출해, 실제 타격 판정 타이밍을 애니메이션과 정확히 맞추기 위해
2. 애니메이션을 재생하기 위해
3. 애니메이터를 초기화하기 위해
4. 성능 향상

**➡ 답:** (   )

**Q90.** `[CustomEditor(typeof(Enemy), true)]`에서 두 번째 인자 `true`의 의미는?
1. 편집을 허용한다
2. `editorForChildClasses` — `Enemy`를 상속한 `Swampy` 등 **자식 클래스에도 이 에디터를 적용**한다
3. 멀티 오브젝트 편집을 허용한다
4. 의미 없다

**➡ 답:** (   )

---

# Part 4. 에디터 확장 · New Input System (Q91 ~ Q100)

**Q91.** 에디터 확장 스크립트(`EnemyEditor.cs`)를 반드시 `Editor` 폴더에 두어야 하는 이유는?
1. 정리 목적일 뿐 아무 상관 없다
2. `Editor` 폴더의 스크립트는 **빌드에서 제외**되며, 그렇지 않으면 `UnityEditor` 네임스페이스 때문에 빌드가 실패한다
3. 컴파일 순서 때문
4. 유니티가 자동 생성해서

**➡ 답:** (   )

**Q92.** `OnInspectorGUI()` 안의 `DrawDefaultInspector();`의 역할은?
1. 인스펙터를 지운다
2. 유니티가 원래 그려주던 기본 인스펙터 UI를 그대로 그린다
3. 컴포넌트를 추가한다
4. 값을 저장한다

**➡ 답:** (   )

**Q93.** `Editor` 클래스의 `target` 프로퍼티는?
1. 현재 인스펙터가 편집 중인 객체(`UnityEngine.Object`) — 캐스팅해서 사용한다
2. 마우스 위치
3. 선택된 씬
4. 프리팹 루트

**➡ 답:** (   )

**Q94.** `EnemyEditor.cs`에서 `if (!Application.isPlaying)` 로 버튼을 막는 이유는?
1. 에디트 모드에서는 `Awake`/`Start`가 실행되지 않아 `_stateMachine`이 `null`이므로 버튼을 누르면 예외가 발생하기 때문
2. 성능 때문
3. 버튼이 보이지 않아서
4. 유니티 규칙이라서

**➡ 답:** (   )

**Q95.** New Input System에서 `InputAction`의 phase 3가지를 올바르게 설명한 것은?
1. `started`(입력 시작) → `performed`(조건 충족/값 변경) → `canceled`(입력 해제)
2. `begin` → `middle` → `end`
3. `down` → `hold` → `up`
4. `enter` → `stay` → `exit`

**➡ 답:** (   )

**Q96.** `_moveAction`에 `performed`뿐 아니라 `canceled`에도 콜백을 연결하는 이유는?
1. 두 번 실행하려고
2. 키에서 손을 뗐을 때 `canceled`가 발생하며, 이때 `ReadValue<Vector2>()`가 `(0,0)`을 반환해 **이동을 멈출 수 있기** 때문
3. 성능 때문
4. 필수 규칙이라서

**➡ 답:** (   )

**Q97.** `_attackAction`은 `performed`에만 연결한 이유는?
1. 공격은 "누르는 순간 한 번" 발생하는 **이벤트성** 입력이라 손을 뗀 시점 처리가 필요 없기 때문
2. `canceled`가 없어서
3. 버그다
4. 성능 때문

**➡ 답:** (   )

**Q98.** `_interactAction`은 `performed`와 `canceled` 모두 연결하고 `true`/`false`를 넘긴다. 이 설계의 의도는?
1. 상호작용을 **지속 상태(누르고 있는 동안 유지)**로 다루기 위해
2. 두 번 실행하려고
3. 실수다
4. 이동과 동일하게 맞추려고

**➡ 답:** (   )

**Q99.** `InputHandler`가 `InputAction.CallbackContext`를 받아서 `Action<Vector2>`로 다시 발행하는 구조의 이름과 장점은?
1. 싱글톤 — 전역 접근
2. 어댑터(Adapter) 계층 — 게임플레이 코드가 Input System 타입에 의존하지 않아, 입력 방식을 바꿔도 `Player`를 수정할 필요가 없다
3. 옵저버만 사용
4. 팩토리 패턴

**➡ 답:** (   )

**Q100.** `InputHandler`에서 `_inputActions.Enable()` / `Disable()` 외에 추가로 필요한 정리 작업은?
1. 없음
2. `OnDestroy()`에서 `_inputActions.Dispose()` — `InputActionAsset` 래퍼가 잡고 있는 네이티브 리소스를 해제
3. `Update()`에서 매 프레임 `Disable()`
4. `Awake()`에서 `Dispose()`

**➡ 답:** (   )

---

# Part 5. 서술형 (S1 ~ S15)

**S1.** `Player`를 `abstract class`로 만들고 `Attack()`을 `abstract`로, `TakeDamage()`를 `virtual`로 선언한 설계 의도를 각각 설명하시오.

**➡ 답안:**
```text





```

**S2.** `IDamagable` 인터페이스를 사용했을 때 얻는 이점을, "적의 공격 코드"를 예로 들어 설명하시오. (힌트: 적은 상대가 `Warrior`인지 `Mage`인지 알 필요가 있는가?)

**➡ 답안:**
```text





```

**S3.** 유한 상태 머신(FSM)이란 무엇이며, 이 프로젝트가 `if/else` 뭉치 대신 FSM을 쓴 이유를 설명하시오.

**➡ 답안:**
```text





```

**S4.** `IState` 인터페이스가 `OnEnter` / `OnUpdate` / `OnExit` 세 개로 나뉘어 있는 이유를, 각 메서드에 들어갈 코드 예시와 함께 설명하시오.

**➡ 답안:**
```text





```

**S5.** `StateMachine.ChangeState()`의 다음 코드에서 순서가 왜 중요한지 설명하시오.
```csharp
_currState?.OnExit(_enemy);
_currState = newState;
_currState?.OnEnter(_enemy);
```

**➡ 답안:**
```text





```

**S6.** `Enemy`가 `StateMachine`을 참조하고, `StateMachine`이 다시 `Enemy`를 참조한다. 이런 **순환 참조**가 C#에서 문제가 되지 않는 이유와, 실제로 문제가 되는 "순환"은 어떤 경우인지 설명하시오.

**➡ 답안:**
```text





```

**S7.** `Enemy`가 `Dictionary<Type, IState>`에 상태를 미리 만들어 캐싱하는 방식과, 전환할 때마다 `new IdleState()`를 만드는 방식의 장단점을 비교하시오. (힌트: GC, 상태 내부 변수)

**➡ 답안:**
```text





```

**S8.** `Swampy`가 `Enemy`의 `InitState()`를 `override`하는 구조(템플릿 메서드 패턴)의 장점을 설명하시오. 새로운 적 `Goblin`을 추가한다면 어떤 코드를 작성해야 하는가?

**➡ 답안:**
```text





```

**S9.** `InputHandler`가 Input System 이벤트를 받아 순수 C# `event Action<T>`로 다시 발행하는 2단 구조의 장점을 3가지 이상 쓰시오.

**➡ 답안:**
```text





```

**S10.** `Player`의 `OnEnable`에서 `+=`, `OnDisable`에서 `-=` 하는 코드가 짝을 이루어야 하는 이유를 "메모리 누수" 관점에서 설명하시오.

**➡ 답안:**
```text





```

**S11.** `ScriptableObject`(`EnemySO`)로 스탯을 분리했을 때와, 각 스크립트에 `[SerializeField] float maxHp`로 직접 넣었을 때의 차이를 "슬라임 100마리의 체력을 한 번에 바꿔야 하는 상황"으로 설명하시오.

**➡ 답안:**
```text





```

**S12.** `Animator.StringToHash`로 미리 해시를 뽑아 `static readonly`에 저장하는 최적화가 왜 효과적인지, `static`이 아닌 인스턴스 필드였다면 무엇이 달라지는지 설명하시오.

**➡ 답안:**
```text





```

**S13.** `Player.OnMove()`가 이벤트 콜백 안에서 `_rb.linearVelocity`를 설정하고 있다. 이 방식의 잠재적 문제점과, `FixedUpdate()`에서 처리하는 방식과의 차이를 설명하시오.

**➡ 답안:**
```text





```

**S14.** 대각선 이동 시 속도가 빨라지는 문제가 생길 수 있다. 원인과 해결 방법(`normalized`)을 벡터 관점에서 설명하시오.

**➡ 답안:**
```text





```

**S15.** `using (new EditorGUI.DisabledScope(cond)) { }` 를 쓰지 않고 직접 구현한다면 어떤 코드가 되는지 작성하고, `using`을 쓰는 것이 더 나은 이유를 설명하시오.

**➡ 답안:**
```text





```

---

# Part 6. 디버깅 — 아래 코드의 문제점을 찾으시오 (D1 ~ D10)

**D1.** `Enemy.cs`
```csharp
protected void Awake()   // virtual 아님
{
    InitState();
    InitComponents();
}
protected void Start()   // virtual 아님
{
    _stateMachine = new StateMachine(this);
    ChangeState<IdleState>();
}
```
자식 클래스 `Swampy`가 `private void Awake() { ... }` 를 정의하면 어떤 일이 벌어지는가? 어떻게 고쳐야 하는가?

**➡ 답안:**
```text





```

**D2.** `Enemy.cs`
```csharp
public void OnDrawGizmos()
{
    Gizmos.DrawWireSphere(transform.position, _enemySO.chaseDistance);
}
```
인스펙터에서 `_enemySO`를 아직 지정하지 않았을 때 어떤 문제가 생기며, 어떻게 방어해야 하는가?

**➡ 답안:**
```text





```

**D3.** `Enemy.cs`
```csharp
public void ChangeState<T>() where T : IState
{
    if (_states.TryGetValue(typeof(T), out IState state))
        _stateMachine?.ChangeState(state);
}
```
`?.`가 숨기고 있는 잠재적 버그는 무엇인가?

**➡ 답안:**
```text





```

**D4.** `InputHandler.cs`의 다음 주석은 정확한가? 틀렸다면 바르게 고치시오.
```csharp
// OnDisable 에서는 가장 먼저 액션 시스템을 비활성화 => 안하면 메모리 누수 발생
_inputActions.Disable();
```

**➡ 답안:**
```text





```

**D5.** `IdleState.cs`
```csharp
public void OnEnter(Enemy enemy)
{
    enemy.ChangeState<ChaseState>();   // 이렇게 쓰면?
}
```
어떤 일이 벌어지며, 왜 위험한가?

**➡ 답안:**
```text





```

**D6.** `Player.cs`
```csharp
protected void OnEnable()   // virtual 아님
{
    _inputHandler.OnMoveAction += OnMove;
}
```
`Warrior`가 자체 `OnEnable`을 정의하면 어떤 문제가 생기는가?

**➡ 답안:**
```text





```

**D7.** `Player.cs`의 `OnMove()`
```csharp
private void OnMove(Vector2 ctx)
{
    _rb.linearVelocity = ctx * _moveSpeed;
}
```
키를 누르고 있는 **동안** 이 콜백이 계속 호출되는가? 캐릭터가 계속 움직이는 이유는 무엇인가?

**➡ 답안:**
```text





```

**D8.** 다음 코드에서 발생할 예외와 그 이유를 쓰시오.
```csharp
var colliders = Physics2D.OverlapCircleAll(pos, r, mask);
var nearest = colliders.OrderBy(c => ...).First().transform;
```

**➡ 답안:**
```text





```

**D9.** `Swampy.InitState()`에서 다음처럼 작성했다. 어떤 문제가 있으며, 컴파일/런타임 에러가 나는가?
```csharp
_states = new Dictionary<Type, IState>
{
    [typeof(IdleState)] = new IdleState(),
    [typeof(ChaseState)] = new ChaseState(),
    [typeof(IdleState)] = new IdleState(),   // 중복
};
```

**➡ 답안:**
```text





```

**D10.** `Enemy.Update()`가 `_stateMachine.Update();` 를 `?.` 없이 호출한다. 안전한가? 어떤 조건에서 위험해지는가?

**➡ 답안:**
```text





```

---

# Part 7. 실기 — 코드 작성 (P1 ~ P5)

**P1.** `Enemy`의 `IdleState.OnUpdate()`를 완성하시오.
- 플레이어를 감지하면(`enemy.DetectPlayer()`) `ChaseState`로 전환

**➡ 답안:**
```csharp







```

**P2.** `ChaseState.OnUpdate()`를 작성하시오.
- 타겟이 없으면 `IdleState`로 복귀
- 타겟이 공격 사거리(`attackDistance`) 안이면 `AttackState`로 전환
- 그 외에는 타겟 방향으로 이동

**➡ 답안:**
```csharp







```

**P3.** `Enemy` 클래스에 `IDamagable`을 구현하고, 체력이 0 이하가 되면 `Debug.Log`로 사망을 출력하는 `TakeDamage(float)`를 작성하시오.

**➡ 답안:**
```csharp







```

**P4.** `Player.OnMove`가 이벤트에서 값을 저장만 하고, 실제 이동은 `FixedUpdate()`에서 처리하도록 리팩터링하시오. 대각선 정규화도 포함할 것.

**➡ 답안:**
```csharp







```

**P5.** `Mage`라는 새로운 플레이어 직업 클래스를 작성하시오.
- `MageSO`(maxHp, moveSpeed, attackDamage, attackCooldown, manaMax)를 만들고
- `Player`를 상속해 `Attack()`을 구현
- `Awake()`에서 SO 값을 적용하되 `base.Awake()`를 반드시 호출할 것

**➡ 답안:**
```csharp







```

---
---

# ✅ 정답 및 해설

## Part 1 정답 (Q1 ~ Q30)

| 번호 | 정답 | 해설 |
|---|---|---|
| Q1 | **2** | `protected` = 자기 자신 + 상속받은 자식. 1번은 `private`, 3번은 `internal`, 4번은 `public`. |
| Q2 | **3** | `=>` 를 쓴 표현식 본문 멤버. 필드처럼 보이지만 실제로는 `get` 전용 프로퍼티다. |
| Q3 | **4** | `get`만 있으므로 대입 불가. 컴파일 에러가 난다. |
| Q4 | **3** | `abstract` 클래스는 인스턴스화 불가. 실제 사용은 `Warrior` 같은 구체 클래스로 한다. 필드는 가질 수 있다. |
| Q5 | **1** | `abstract` = 본문 없음 + 자식이 필수 구현. `virtual` = 기본 구현 있음 + 재정의 선택. |
| Q6 | **2** | `base.` 는 부모 구현 호출. 재귀가 아니다. |
| Q7 | **2** | 방어력을 뺀 결과를 부모에 넘겨야 실제 체력이 올바르게 깎인다. |
| Q8 | **2** | `override`는 부모 구현을 **대체**한다. 부모의 초기화가 필요하면 명시적으로 `base.Awake()`를 불러야 한다. Unity가 대신 불러주지 않는다. |
| Q9 | **4** | 인터페이스는 인스턴스 필드를 가질 수 없다(프로퍼티·메서드·이벤트만). |
| Q10 | **2** | C#은 단일 상속 + 다중 인터페이스 구현. 기저 클래스는 목록 맨 앞. |
| Q11 | **2** | `struct` = 값 타입, 스택(또는 담고 있는 객체 내부), 상속 불가, `new` 없이도 기본값으로 사용 가능. |
| Q12 | **3** | `Vector2`는 구조체(값 타입). 나머지는 모두 참조 타입. |
| Q13 | **2** | `const`는 컴파일 타임 상수만 가능. 런타임 함수 결과는 `static readonly`로. |
| Q14 | **2** | `static` = 타입(클래스)에 소속. 인스턴스가 몇 개든 하나만 존재. |
| Q15 | **1** | 문자열 해시 계산을 클래스당 한 번만 수행 → 적이 많아도 비용이 늘지 않는다. |
| Q16 | **2** | 문자열 보간. `string.Format`의 축약형. |
| Q17 | **2** | 이름 충돌 방지 + 논리적 분류. 폴더 구조와 일치시키는 건 관례일 뿐 강제는 아니다. |
| Q18 | **2** | `target`은 `UnityEngine.Object` 타입이라 실제 타입으로 캐스팅해야 멤버에 접근할 수 있다. |
| Q19 | **2** | `as`는 실패 시 `null`. `(T)` 캐스팅은 실패 시 `InvalidCastException`. |
| Q20 | **2** | `out`은 메서드가 값을 채워 돌려주는 매개변수. C# 7부터 인라인 변수 선언이 가능하다. |
| Q21 | **2** | null 조건부 연산자. `null`이면 전체 식이 `null`이 되고 호출은 건너뛴다. |
| Q22 | **2** | 델리게이트는 구독자가 0명이면 `null`. `?.Invoke()`는 사실상 필수 관용구다. |
| Q23 | **2** | `System.Type` 인스턴스. 딕셔너리 키로 쓰기에 적합하다(타입당 유일). |
| Q24 | **2** | `[key] = value` 형태 → 인덱스 초기화자(C# 6+). |
| Q25 | **1** | `{ k, v }` 형태는 `Add()` 호출 → 중복 키면 `ArgumentException`. 실수를 잡아준다는 장점이 있다. |
| Q26 | **2** | 인덱서 대입은 조용히 덮어쓴다. 오타로 중복돼도 에러가 안 나서 발견이 늦어질 수 있다. |
| Q27 | **3** | `1 << 8` = 2⁸ = **256**. |
| Q28 | **2** | 레이어는 32개, `int`는 32비트. 각 비트가 레이어 하나의 on/off. 여러 레이어는 `|`로 합친다. |
| Q29 | **2** | 클래스명과 동일하고 반환 타입이 없는 것이 생성자. |
| Q30 | **2** | `var`는 **정적 타입 추론**. `dynamic`과 달리 런타임 결정이 아니다. |

## Part 2 정답 (Q31 ~ Q55)

| 번호 | 정답 | 해설 |
|---|---|---|
| Q31 | **2** | 제네릭 타입 매개변수. `ChangeState<IdleState>()`로 호출 시 `T = IdleState`. |
| Q32 | **1** | 인터페이스 제약. `IState`를 구현한 타입만 허용 → 컴파일 타임에 잘못된 타입을 차단. |
| Q33 | **4** | `static` 제약은 없다. 나머지는 모두 유효. (`unmanaged`, `notnull`, 기저 클래스 제약도 존재) |
| Q34 | **1** | 상태 인스턴스를 딕셔너리에 캐싱 → 전환마다 힙 할당이 없어 GC 스파이크가 줄어든다. |
| Q35 | **2** | `Action` 계열은 항상 `void` 반환. `Action<T1,T2,...>`로 최대 16개 인자. |
| Q36 | **2** | `Func<..., TResult>` — 마지막 타입 인자가 반환 타입. |
| Q37 | **3** | `Func`는 **마지막**이 반환 타입 → `bool`. 앞의 `int`, `string`은 매개변수. |
| Q38 | **2** | `event`는 캡슐화 장치. 외부에서 `= null`로 초기화하거나 임의로 발행하는 것을 막는다. |
| Q39 | **1** | 델리게이트는 호출 목록(invocation list)을 가진다. `+=` 한 순서대로 전부 실행된다. |
| Q40 | **2** | 발행자가 구독자 참조를 붙잡고 있으면 구독자는 GC 대상이 되지 못한다. 파괴된 `MonoBehaviour`의 메서드가 호출되면 예외도 발생한다. |
| Q41 | **2** | `Awake`는 1회, `OnEnable`/`OnDisable`은 매번. 짝이 맞아야 껐다 켜도 정상 동작한다. |
| Q42 | **2** | 제어의 역전(IoC). 내가 부르는 게 아니라 "불려지는" 함수. |
| Q43 | **1** | `매개변수 => 식` 형태의 람다식. `OrderBy`에 정렬 기준을 넘긴다. |
| Q44 | **2** | 빈 컬렉션 처리 방식이 다르다. 확실치 않으면 `FirstOrDefault()` + null 체크가 안전. |
| Q45 | **2** | `First()`는 요소가 없으면 예외. 그래서 길이 검사를 먼저 한다. |
| Q46 | **2** | 지연 실행. 그래서 원본이 바뀌면 결과도 달라질 수 있다(재열거 시). |
| Q47 | **2** | √ 연산 생략. `a² < b²` ⟺ `a < b` (양수일 때)이므로 **비교·정렬 목적**에는 완전히 동일한 결과. |
| Q48 | **3** | `sqrMagnitude`는 거리의 **제곱**이므로 실제 거리 값이 아니다. 비교용으로만 쓴다. |
| Q49 | **1** | 해시 테이블 기반이라 평균 O(1). |
| Q50 | **2** | 존재 여부 확인 + 값 가져오기를 한 번에. 예외를 흐름 제어에 쓰지 않아도 된다. |
| Q51 | **1** | ① 지시문 ② 문 ③ 선언(C# 8+). 이름은 같지만 완전히 다른 기능이다. |
| Q52 | **2** | `try-finally`로 컴파일. 그래서 예외가 나도 `Dispose()`가 보장된다. |
| Q53 | **2** | `IDisposable`(또는 `IAsyncDisposable`)을 구현해야 `using` 문에 넣을 수 있다. |
| Q54 | **2** | `EditorGUI` 안에 정의된 중첩 클래스이며 `GUI.Scope`를 상속한다. 점(`.`)은 "안에 있는 것"을 가리키는 경로일 뿐, 메서드 호출이 아니다. |
| Q55 | **2** | `GUI.enabled`는 전역 static. 복구하지 않으면 그 이후 그려지는 모든 인스펙터가 회색이 된다. `DisabledScope`는 이걸 자동으로 복구해준다. |

## Part 3 정답 (Q56 ~ Q90)

| 번호 | 정답 | 해설 |
|---|---|---|
| Q56 | **2** | `Awake` → `OnEnable` → `Start` → (`FixedUpdate`) → `Update` → `LateUpdate` → `OnDisable` → `OnDestroy` |
| Q57 | **1** | `Awake`는 스크립트가 비활성 상태여도 호출된다(게임오브젝트가 활성일 때). `Start`는 활성일 때만. |
| Q58 | **2** | "내 것 초기화 → `Awake`", "남의 것 참조 → `Start`" 가 안전한 관례. |
| Q59 | **2** | 활성/비활성 토글마다 매번. 그래서 이벤트 구독/해제 자리로 적합하다. |
| Q60 | **1** | `Update`는 렌더 프레임마다(간격 가변), `FixedUpdate`는 물리 주기마다(기본 0.02초 고정). |
| Q61 | **2** | 물리 연산은 `FixedUpdate`가 원칙. `Update`에서 하면 프레임률에 따라 결과가 흔들린다. |
| Q62 | **2** | 모든 `Update` 종료 후 실행 → 카메라 추적, 최종 위치 보정에 적합. |
| Q63 | **2** | 직전 프레임 소요 시간(초). 60fps면 약 0.0167. |
| Q64 | **2** | 프레임 독립성. `speed * deltaTime`을 매 프레임 더하면 결과적으로 초당 `speed`만큼 이동한다. |
| Q65 | **2** | 게임 시작(또는 씬 로드) 이후 누적 시간. 쿨다운 계산에 자주 쓴다. |
| Q66 | **2** | 마지막 공격 시각 + 쿨다운 ≤ 현재 시각일 때만 공격 허용. |
| Q67 | **4** | 순수 C# 클래스(`StateMachine`, `IState` 구현체들)처럼 `MonoBehaviour`를 상속하지 않는 클래스도 얼마든지 만들 수 있다. |
| Q68 | **1** | `GetComponent`는 탐색 비용이 있다. `Awake`에서 한 번 캐싱하는 것이 표준. |
| Q69 | **2** | `GameObject.Find`는 씬 전체 이름 검색(느림), `transform.Find`는 자식만 검색(상대적으로 저렴). |
| Q70 | **2** | 이름 기반 탐색은 비싸다. 매 프레임 반복은 금물. |
| Q71 | **1** | 캡슐화를 유지하면서 인스펙터 노출 + 직렬화. |
| Q72 | **2** | `public`은 외부 아무 코드나 값을 바꿀 수 있다. `[SerializeField] private`가 캡슐화 측면에서 낫다. |
| Q73 | **1** | 인스펙터 가독성용 어트리뷰트. 실행에는 영향 없다. |
| Q74 | **1** | 의존 컴포넌트를 자동 추가하고 삭제를 막아, "Rigidbody2D를 안 붙여서 생기는 버그"를 원천 차단한다. |
| Q75 | **1** | Create 메뉴 등록. `menuName`으로 메뉴 경로를, `fileName`으로 기본 파일명을 지정한다. |
| Q76 | **1** | 데이터/로직 분리. 에셋 하나를 여러 오브젝트가 공유하며, 프로그래머 없이도 기획자가 값을 조정할 수 있다. |
| Q77 | **3** | `ScriptableObject`에는 `Update`/`FixedUpdate` 같은 프레임 콜백이 없다. (`OnEnable`/`OnDisable`/`Awake`는 존재하지만 의미가 다르다) |
| Q78 | **2** | 에디터에서는 SO 변경이 에셋에 남는다. **런타임 가변 상태(현재 체력 등)는 SO에 두면 안 된다.** |
| Q79 | **1** | Unity 6에서 `Rigidbody2D.velocity` → `linearVelocity`로 개명(각속도와 대칭). |
| Q80 | **2** | Kinematic. 물리 힘의 영향을 받지 않고 스크립트로 직접 이동시킨다. |
| Q81 | **1** | 물리적 반응 없이 겹침만 감지. `OnTriggerEnter2D/Stay2D/Exit2D` 호출. |
| Q82 | **3** | `Collider2D[]`. 결과가 없으면 길이 0인 배열(`null` 아님). |
| Q83 | **1** | 매 호출마다 배열 할당 → GC 부담. 성능이 중요하면 `OverlapCircleNonAlloc`(또는 `Physics2D.OverlapCircle` + 버퍼)을 쓴다. |
| Q84 | **2** | 관심 없는 레이어를 아예 검출하지 않아 성능과 정확도를 동시에 잡는다. |
| Q85 | **1** | `OnDrawGizmos`는 항상 그려진다. 적이 많으면 씬 뷰가 지저분해질 수 있어 `Selected` 버전을 쓰기도 한다. |
| Q86 | **2** | 기즈모는 씬 뷰 전용 디버그 시각화. 게임 화면·빌드에는 나오지 않는다. |
| Q87 | **1** | `SetBool("IsWalk", ...)`은 내부에서 매번 문자열→해시 변환. 미리 뽑아두면 그 비용이 사라진다. |
| Q88 | **4** | Int는 `SetInteger`. |
| Q89 | **1** | Animation Event. 검을 휘두르는 정확한 프레임에 타격 판정을 넣을 수 있다. |
| Q90 | **2** | `editorForChildClasses: true`. 이게 없으면 `Swampy`(자식)에는 커스텀 인스펙터가 적용되지 않는다. |

## Part 4 정답 (Q91 ~ Q100)

| 번호 | 정답 | 해설 |
|---|---|---|
| Q91 | **2** | `Editor` 폴더 스크립트는 빌드 제외. `using UnityEditor;`는 빌드에 포함될 수 없다. |
| Q92 | **2** | 기본 인스펙터를 그대로 그린 뒤, 그 아래에 커스텀 UI를 추가하는 것이 일반적인 패턴. |
| Q93 | **1** | 현재 편집 중인 객체. 다중 선택 시에는 `targets`(배열)를 쓴다. |
| Q94 | **1** | 에디트 모드에서는 `Awake`/`Start`가 실행되지 않아 `_stateMachine`이 `null`. 버튼을 비활성화하는 것이 근본 대응이다. |
| Q95 | **1** | `started`(입력 감지 시작) → `performed`(액션 조건 충족, Value 타입은 값이 바뀔 때마다) → `canceled`(입력 종료). |
| Q96 | **2** | `canceled` 시 `ReadValue<Vector2>()`는 `(0,0)`. 이걸 안 받으면 키에서 손을 떼도 캐릭터가 계속 미끄러진다. |
| Q97 | **1** | 공격은 순간 이벤트. "언제 눌렀나"만 중요하고 "언제 뗐나"는 무의미하다. |
| Q98 | **1** | `true`=시작, `false`=종료 → 지속 상태 표현. 문 잡고 있기, 채집하기 같은 홀드형 상호작용에 쓴다. |
| Q99 | **2** | 어댑터 계층. `Player`는 `Vector2`만 알면 되므로 키보드/패드/모바일 조이스틱 어느 쪽이든 `InputHandler`만 고치면 된다. |
| Q100 | **2** | `InputActionAsset` 래퍼는 `IDisposable`. `OnDestroy()`에서 `Dispose()`가 권장된다. **현재 이 프로젝트에는 빠져 있다.** |

---

## Part 5 서술형 해설 (S1 ~ S15)

**S1.**
- `abstract class Player` — 플레이어 "직업"의 공통 골격만 정의하고, 그 자체로는 씬에 존재할 수 없게 한다. `Player` 컴포넌트를 실수로 붙이는 것을 막는 안전장치.
- `abstract void Attack()` — 공격 방식은 직업마다 **완전히 다르므로** 기본 구현이 존재할 수 없다. 자식이 반드시 구현하도록 강제한다.
- `virtual void TakeDamage()` — 체력 감소·사망 판정은 **공통 로직이 존재**한다. `Warrior`처럼 방어력을 추가로 적용하고 싶은 경우에만 `override` 후 `base` 호출.

**S2.**
`IDamagable`이 있으면 적의 공격 코드는 이렇게 쓸 수 있다.
```csharp
if (hit.TryGetComponent<IDamagable>(out var damagable))
    damagable.TakeDamage(_attackDamage);
```
적은 상대가 `Warrior`인지 `Mage`인지, 심지어 파괴 가능한 상자인지조차 알 필요가 없다. **"맞을 수 있는 것"이라는 계약만 알면 된다.** 새 직업이나 오브젝트를 추가해도 적 코드는 한 줄도 바뀌지 않는다(개방-폐쇄 원칙).

**S3.**
FSM은 객체가 가질 수 있는 상태를 명확히 나열하고, 한 번에 **하나의 상태만** 활성화되며, 정해진 조건으로만 전환되는 구조다.
`if/else` 뭉치는 상태가 늘어날수록 조건이 기하급수로 얽히고, "지금 무슨 상태인지"가 여러 bool 변수에 흩어져 모순 상태(예: `isIdle && isAttacking`)가 생긴다. FSM은 상태를 클래스로 분리해 각 상태의 코드가 독립적이고, 새 상태 추가가 기존 코드를 건드리지 않는다.

**S4.**
- `OnEnter` — 상태에 **진입할 때 딱 한 번**. 애니메이션 전환, 타이머 초기화, 속도 0으로 리셋.
- `OnUpdate` — 상태 유지 중 **매 프레임**. 이동 처리, 전환 조건 검사.
- `OnExit` — 상태를 **떠날 때 딱 한 번**. 이펙트 정리, 코루틴 중지, 잔여 속도 제거.

이렇게 나누면 "진입 시 1회만 해야 할 일"과 "매 프레임 해야 할 일"이 뒤섞이지 않는다.

**S5.**
1. **이전 상태를 먼저 정리**(`OnExit`)해야 한다. 나중에 하면 새 상태가 설정한 값을 이전 상태의 정리 코드가 지워버린다.
2. `_currState`를 **먼저 교체한 뒤** `OnEnter`를 호출해야 한다. `OnEnter` 안에서 현재 상태를 조회하면 새 상태가 나와야 하기 때문.
3. `?.` — 최초 전환 시 `_currState`가 `null`이므로 `OnExit` 호출을 건너뛰어야 한다.

**S6.**
C#은 어셈블리 단위로 한 번에 컴파일하므로 전방 선언이 필요 없고, 클래스 간 상호 참조는 **완전히 정상**이다. Unity 자신도 `gameObject.transform` ↔ `transform.gameObject`처럼 양방향 참조를 쓴다. .NET GC는 mark-and-sweep 방식이라 참조 순환이 있어도 루트에서 도달 불가능해지면 함께 회수된다.

실제로 문제가 되는 "순환"은 두 가지다.
1. **어셈블리 정의(asmdef) 순환 참조** — 컴파일 에러. (이 프로젝트에는 asmdef가 없어 해당 없음)
2. **무한 재귀** — `A.Foo()`가 `B.Bar()`를 부르고 `B.Bar()`가 다시 `A.Foo()`를 부르면 `StackOverflowException`. 이건 `try-catch`로도 잡을 수 없어 **Unity 에디터가 통째로 죽는다.**

**S7.**

| | 캐싱(딕셔너리) | 매번 `new` |
|---|---|---|
| GC | 할당 없음 ✅ | 전환마다 할당 ❌ |
| 상태 내부 변수 | 이전 값이 남음 ⚠️ → `OnEnter`에서 반드시 초기화 | 항상 깨끗함 ✅ |
| 상태 공유 | 적 인스턴스마다 따로 만들어야 함(현재 구조는 각 `Enemy`가 자기 딕셔너리 보유 ✅) | 자동 분리 |

캐싱이 성능상 유리하지만, **`OnEnter`에서 상태 내부 변수를 반드시 리셋**해야 한다는 규칙이 따라온다.

**S8.**
`Enemy`가 "InitState가 호출되는 시점과 흐름"을 고정하고, **무엇을 채울지만** 자식에게 위임하는 템플릿 메서드 패턴이다. `abstract`이므로 자식이 구현을 빠뜨리면 컴파일 에러로 즉시 잡힌다.

`Goblin` 추가 시:
```csharp
public class Goblin : Enemy
{
    protected override void InitState()
    {
        _states = new Dictionary<Type, IState>
        {
            [typeof(IdleState)]   = new IdleState(),
            [typeof(ChaseState)]  = new ChaseState(),
            [typeof(AttackState)] = new AttackState(),
            [typeof(FleeState)]   = new FleeState(),   // 고블린만의 도망 상태
        };
    }
}
```
`Enemy` 코드는 한 줄도 수정하지 않는다.

**S9.**
1. **의존성 차단** — `Player`가 `InputAction.CallbackContext`를 몰라도 된다. Input System을 다른 것으로 교체해도 `InputHandler`만 수정.
2. **테스트 용이성** — 실제 키 입력 없이 `OnMoveAction?.Invoke(Vector2.right)`로 이동 로직을 검증할 수 있다.
3. **다중 구독** — UI, 사운드, 카메라, 튜토리얼 등이 같은 이벤트를 함께 들을 수 있다(멀티캐스트).
4. **의미 변환** — `performed`/`canceled` 두 phase를 `bool` 하나로 정리하는 등, 저수준 개념을 게임 도메인 언어로 번역한다.
5. **입력 차단 일원화** — 컷신 중 입력을 막고 싶으면 `InputHandler` 한 곳만 끄면 된다.

**S10.**
`_inputHandler.OnMoveAction += OnMove;` 는 `InputHandler`의 델리게이트가 **`Player` 인스턴스를 참조**하게 만든다. `Player`가 `Destroy`되어도 이 참조가 살아 있으면 GC가 `Player`를 회수하지 못한다(누수). 게다가 이벤트가 발생하면 이미 파괴된 객체의 메서드가 호출되어 `MissingReferenceException`이 터진다.
`OnEnable`의 모든 `+=`는 `OnDisable`에서 **정확히 같은 개수의 `-=`** 와 짝을 이뤄야 한다.

**S11.**
- `[SerializeField] float maxHp` 방식: 슬라임 100마리가 각각 값을 들고 있다. 프리팹이면 프리팹 하나만 고치면 되지만, 씬에 개별 배치되어 값이 오버라이드된 경우 **100개를 전부 수정**해야 한다.
- `EnemySO` 방식: 모든 슬라임이 같은 SO 에셋을 참조하므로 **에셋 하나의 `maxHp`만 바꾸면 100마리에 즉시 반영**된다. 게다가 종류별 SO(SlimeSO, GoblinSO)를 만들어 프리팹에 끼워넣기만 하면 되므로 밸런싱 작업이 코드와 완전히 분리된다.

**S12.**
`SetBool("IsWalk", ...)`은 호출할 때마다 문자열을 해시로 변환한다. `Update`에서 매 프레임 호출된다면 이 비용이 계속 발생한다. `StringToHash`로 미리 뽑아두면 정수 비교만 남는다.
`static`이 아닌 인스턴스 필드였다면 **적 100마리 × 필드 2개 = 200번** 해시 계산이 일어나고, 메모리도 인스턴스마다 차지한다. 값이 모든 인스턴스에서 동일하므로 `static readonly`가 정확한 선택이다.

**S13.**
문제점:
1. **호출 시점이 물리 프레임과 어긋난다.** 입력 이벤트는 렌더 프레임 타이밍에 오는데 물리는 `FixedUpdate` 주기로 돈다.
2. **Value 액션의 `performed`는 값이 "변할 때"만 발생**한다. 키를 계속 누르고 있으면 콜백이 오지 않는다. (지금 캐릭터가 계속 움직이는 건 `linearVelocity`가 한 번 설정되면 유지되기 때문이다.)
3. 그래서 이동 로직이 "속도 유지"에 의존하게 되어, 마찰(Linear Damping)이나 외부 힘이 개입하면 의도대로 동작하지 않는다.

권장 방식:
```csharp
private Vector2 _moveInput;
private void OnMove(Vector2 ctx) => _moveInput = ctx;   // 저장만
private void FixedUpdate() => _rb.linearVelocity = _moveInput.normalized * _moveSpeed;
```

**S14.**
`(1, 0)`의 크기는 1이지만 `(1, 1)`의 크기는 √2 ≈ 1.414다. WASD 입력을 그대로 쓰면 대각선 이동이 약 41% 빨라진다.
`normalized`는 벡터의 방향은 유지하고 크기를 1로 만든다. `_moveInput.normalized * _moveSpeed`를 쓰면 어느 방향이든 속력이 `_moveSpeed`로 동일해진다.
(단, Input System의 `Vector2Composite`에 Mode를 `Digital Normalized`로 설정하면 입력 단계에서 정규화되기도 한다.)

**S15.**
수동 구현:
```csharp
bool prev = GUI.enabled;                 // ① 원래 값 백업
GUI.enabled = prev && Application.isPlaying;   // ② 변경
{
    if (GUILayout.Button("Idle")) ...
}
GUI.enabled = prev;                      // ③ 복구
```
`using`이 더 나은 이유:
1. **복구를 잊을 수 없다.** `using` 블록을 벗어나면 무조건 `Dispose()`가 호출된다.
2. **예외가 나도 복구된다.** `try-finally`로 컴파일되므로 블록 안에서 예외가 터져도 `GUI.enabled`가 복원된다. 수동 코드는 여기서 상태가 오염된 채로 빠져나간다.
3. **중첩이 안전하다.** `prev`를 스코프 객체가 보관하므로 여러 겹으로 중첩해도 각각 자기 값으로 되돌아간다.
4. **범위가 눈에 보인다.** 들여쓰기만 봐도 "어디까지 비활성인지" 즉시 알 수 있다.

---

## Part 6 디버깅 해설 (D1 ~ D10)

**D1.** `Awake`/`Start`가 `virtual`이 아니므로, `Swampy`가 같은 이름의 메서드를 정의하면 **오버라이드가 아니라 숨김(hiding)** 이 된다. Unity는 리플렉션으로 가장 파생된 클래스의 메시지 함수를 호출하므로 `Swampy.Awake()`만 실행되고 `Enemy.Awake()`는 **호출되지 않는다.** 결과적으로 `InitState()`가 실행되지 않아 `_states`가 `null`이 되고, `ChangeState<T>()`에서 `NullReferenceException`이 난다. (경고 CS0108도 뜬다)

수정:
```csharp
protected virtual void Awake() { ... }
protected virtual void Start() { ... }
```
그리고 자식은 `protected override void Awake() { base.Awake(); ... }` 로 작성한다. — **이 프로젝트의 `Player`/`Warrior`는 이미 `virtual`/`override`로 올바르게 되어 있으나, `Enemy`는 그렇지 않다.**

**D2.** `OnDrawGizmos`는 **에디트 모드에서도, 매 프레임처럼 계속** 호출된다. `_enemySO`가 비어 있으면 `NullReferenceException`이 초당 수십 번 콘솔을 도배해 다른 로그를 못 보게 된다.
```csharp
public void OnDrawGizmos()
{
    if (_enemySO == null) return;
    ...
}
```

**D3.** `?.` 는 `_stateMachine`이 `null`일 때 **조용히 아무것도 하지 않는다.** 에디트 모드의 예외는 막아주지만, 런타임에 초기화 순서가 꼬여(D1 같은 상황) `_stateMachine`이 없을 때도 **상태 전환이 실패했다는 사실 자체가 감춰진다.** 적이 영원히 Idle에 머무는데 에러는 하나도 안 뜬다.
근본 해결은 ① 에디터 버튼을 플레이 모드에서만 활성화(이미 적용됨) ② `Awake`/`Start`를 `virtual`로 만들어 초기화를 보장하는 것이다. 디버깅을 위해 `else Debug.LogWarning(...)` 을 붙이는 것도 좋다.

**D4.** 부정확하다. `Disable()`은 **입력 처리를 멈추는 것**이지 메모리 누수와 직접 관련이 없다. 메모리 누수를 막는 것은 그 아래의 `-=` 구독 해제이며, 실제 리소스 해제는 `Dispose()`다.
```csharp
// OnDisable에서는 먼저 액션 시스템을 비활성화하여 입력 처리를 중단한다.
// (메모리 누수를 막는 것은 아래의 -= 구독 해제이며, 실제 자원 해제는 OnDestroy의 Dispose())
_inputActions.Disable();
```
그리고 다음을 추가해야 한다.
```csharp
private void OnDestroy() => _inputActions?.Dispose();
```

**D5.** `OnEnter` 안에서 다시 `ChangeState`를 호출하면 → `ChangeState`가 `OnEnter`를 호출 → 그 `OnEnter`가 또 `ChangeState`를 호출… **무한 재귀**가 되어 `StackOverflowException`이 발생한다. 이 예외는 `try-catch`로 잡을 수 없고 **Unity 에디터 프로세스 자체가 강제 종료**된다(저장하지 않은 작업이 날아간다).
**규칙: 상태 전환은 `OnUpdate`에서만 한다.** `OnEnter`/`OnExit`에서는 절대 전환하지 않는다.

**D6.** `OnEnable`이 `virtual`이 아니므로 D1과 같은 숨김 문제가 발생한다. `Warrior`가 `OnEnable`을 정의하면 `Player.OnEnable`이 호출되지 않아 **입력 이벤트 구독이 통째로 누락**되고, 캐릭터가 아예 움직이지 않는다. 게다가 `OnDisable`은 그대로 실행되어 구독하지도 않은 것을 해제하려 든다(`-=`는 없는 대상에 대해선 조용히 무시되므로 에러도 안 난다 → 더 찾기 어렵다).
→ `protected virtual void OnEnable()` / `OnDisable()` 로 바꾸는 것이 안전하다.

**D7.** **호출되지 않는다.** Move 액션은 Value 타입이라 `performed`는 **값이 변할 때만** 발생한다. W를 계속 누르고 있으면 값이 `(0,1)`로 고정이므로 콜백은 오지 않는다.
그런데도 캐릭터가 계속 움직이는 이유는, `_rb.linearVelocity`가 **한 번 설정되면 물리 엔진이 그 속도를 계속 유지**하기 때문이다. 그리고 키를 떼면 `canceled`가 발생해 `(0,0)`이 들어가면서 멈춘다.
즉 현재 코드는 "속도 유지" 특성에 의존하고 있다. Linear Damping을 올리거나 넉백 같은 외력이 개입하면 의도대로 동작하지 않는다.

**D8.** `colliders`가 빈 배열일 때 `.First()`가 `InvalidOperationException("Sequence contains no elements")`을 던진다. `OverlapCircleAll`은 결과가 없어도 `null`이 아닌 **길이 0 배열**을 반환하므로 `null` 체크로는 막을 수 없다. `Length > 0` 검사 또는 `FirstOrDefault()` + null 체크가 필요하다.

**D9.** **컴파일 에러도 런타임 에러도 나지 않는다.** 인덱스 초기화자(`[key] = value`)는 인덱서 대입이므로 중복 키를 조용히 덮어쓴다. 다만 `new IdleState()`가 한 번 헛되이 할당되고 즉시 버려진다.
만약 컬렉션 초기화자(`{ key, value }`)를 썼다면 `Add()`가 호출되어 `ArgumentException`으로 **실수를 즉시 잡아줬을** 것이다. 이것이 두 문법의 실질적인 차이다.
(참고: 현재 `Swampy.cs`에는 중복이 없다. 과거에 있었다가 수정된 상태다.)

**D10.** 현재 구조에서는 **동작한다.** 유니티 생명주기가 `Start` → `Update` 순서를 보장하므로, `Update`가 처음 실행될 때는 이미 `Start`에서 `_stateMachine`이 생성되어 있다.
다만 다음 경우 위험해진다.
1. `Enemy`를 상속한 클래스가 `Start`를 숨기면(D1) `_stateMachine`이 영원히 `null`.
2. `Awake`에서 `ChangeState`를 호출하도록 코드를 옮기면 `Start`보다 먼저라 `null`.
3. 오브젝트를 비활성 상태로 생성한 뒤 나중에 켜는 경우 순서가 꼬일 수 있다.

가장 견고한 해결책은 `_stateMachine` 생성을 `Start`가 아니라 **`Awake`의 `InitState()` 직후**로 옮기는 것이다. 그러면 "생성 → 즉시 사용 가능"이 보장된다.

---

## Part 7 실기 모범 답안 (P1 ~ P5)

**P1.**
```csharp
public void OnUpdate(Enemy enemy)
{
    if (enemy.DetectPlayer())
    {
        enemy.ChangeState<ChaseState>();
    }
}
```

**P2.** (`Enemy`에 `EnemySO`와 `_rb` 접근용 프로퍼티가 필요하다)
```csharp
public void OnUpdate(Enemy enemy)
{
    // 1) 타겟을 잃었으면 대기 상태로 복귀
    if (!enemy.DetectPlayer())
    {
        enemy.ChangeState<IdleState>();
        return;
    }

    // 2) 공격 사거리 안이면 공격 상태로 전환
    Vector2 diff = enemy.target.position - enemy.transform.position;
    float attackDist = enemy.Data.attackDistance;
    if (diff.sqrMagnitude <= attackDist * attackDist)   // 제곱끼리 비교 (√ 생략)
    {
        enemy.ChangeState<AttackState>();
        return;
    }

    // 3) 그 외에는 타겟 방향으로 이동
    enemy.Move(diff.normalized);
}
```
> 포인트: `sqrMagnitude`로 비교하려면 **비교 대상도 제곱**해야 한다(`attackDist * attackDist`).

**P3.**
```csharp
public abstract class Enemy : MonoBehaviour, IDamagable
{
    protected float _currHp;

    protected virtual void Awake()
    {
        _currHp = _enemySO.maxHp;
        InitState();
        InitComponents();
    }

    public virtual void TakeDamage(float damage)
    {
        if (_currHp <= 0f) return;      // 이미 죽었으면 무시

        _currHp -= damage;
        _animator.SetTrigger(hashHit);

        if (_currHp <= 0f)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        _currHp = 0f;
        Debug.Log($"{name} 사망");
    }
}
```

**P4.**
```csharp
private Vector2 _moveInput;

private void OnMove(Vector2 ctx)
{
    if (_isDead) return;

    _moveInput = ctx;                       // 값 저장만 한다

    if (ctx.x != 0) FlipDirection(ctx.x > 0);
    _animator.SetBool(hashIsWalk, ctx.sqrMagnitude > 0f);
}

private void FixedUpdate()
{
    if (_isDead)
    {
        _rb.linearVelocity = Vector2.zero;
        return;
    }

    // 대각선 정규화 → 어느 방향이든 속력 동일
    _rb.linearVelocity = _moveInput.normalized * _moveSpeed;
}
```

**P5.**
```csharp
// MageSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "MageSO", menuName = "DungeonMaster/MageSO")]
public class MageSO : ScriptableObject
{
    [Header("마법사 기본 스탯")]
    public float maxHp = 80f;
    public float moveSpeed = 4.5f;
    public float attackDamage = 35f;
    public float attackCooldown = 1.2f;
    public float manaMax = 100f;
}
```
```csharp
// Mage.cs
using UnityEngine;

namespace DungeonMaster.Character.Player
{
    public class Mage : Player
    {
        [Header("마법사 전용 스탯")]
        [SerializeField] private MageSO _mageSO;

        private float _currMana;

        protected override void Awake()
        {
            _maxHp          = _mageSO.maxHp;
            _moveSpeed      = _mageSO.moveSpeed;
            _attackDamage   = _mageSO.attackDamage;
            _attackCooldown = _mageSO.attackCooldown;
            _currMana       = _mageSO.manaMax;

            base.Awake();       // ★ 반드시 호출 — 컴포넌트 캐싱과 체력 초기화가 여기 있다
        }

        protected override void Attack()
        {
            const float manaCost = 10f;
            if (_currMana < manaCost)
            {
                Debug.Log("마나가 부족합니다.");
                return;
            }

            _currMana -= manaCost;
            Debug.Log($"파이어볼 발사! (남은 마나: {_currMana})");
        }
    }
}
```
> 포인트: `base.Awake()`를 **SO 값을 대입한 뒤에** 호출한다. `Player.Awake()`가 `_currHp = _maxHp;` 를 실행하므로, `_maxHp`를 먼저 설정해야 체력이 올바르게 채워진다. (`Warrior.cs`도 동일한 순서를 따르고 있다.)

---

## 📌 자주 틀리는 포인트 요약

1. **`abstract` vs `virtual`** — 본문 유무와 재정의 강제 여부
2. **`base.메서드()` 호출 누락** — 부모 초기화가 통째로 사라진다
3. **`virtual`이 아닌 메서드를 자식이 재정의** — 오버라이드가 아니라 **숨김**, Unity 메시지 함수에서 특히 치명적
4. **`Awake` vs `Start`** — 내 것 초기화 / 남의 것 참조
5. **`OnEnable`에서 구독, `OnDisable`에서 해제** — 개수가 정확히 짝을 이뤄야 함
6. **`event`는 구독자가 없으면 `null`** → `?.Invoke()` 필수
7. **`sqrMagnitude`는 거리의 제곱** — 비교 대상도 제곱해야 함
8. **`First()`는 빈 컬렉션에서 예외** — 길이 검사 또는 `FirstOrDefault()`
9. **인덱스 초기화자는 중복 키를 조용히 덮어씀** — 컬렉션 초기화자는 예외를 던짐
10. **상태 전환은 `OnUpdate`에서만** — `OnEnter`에서 하면 `StackOverflow`로 에디터가 죽는다
11. **`GUI.enabled`는 전역 static** — 복구 필수, `DisabledScope` 사용 권장
12. **Value 액션의 `performed`는 값이 변할 때만** — 누르고 있는 동안 계속 오지 않는다
13. **`ScriptableObject`는 런타임 가변 상태를 담으면 안 됨** — 에디터에서 값이 영구 변경된다
14. **`Editor` 폴더가 아니면 `using UnityEditor;`로 빌드 실패**
15. **`Time.deltaTime` 곱하기를 잊으면 프레임률에 따라 속도가 달라짐**
