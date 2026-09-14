using UnityEditor;
using DungeonMaster.Character.Player;
using UnityEngine;

// [하는 일] 플레이어를 선택했을 때 인스펙터 창 맨 아래에 "피격" 테스트 버튼을 하나 더 그린다.
//          숫자를 적고 버튼을 누르면 그만큼 플레이어가 맞는다. 적을 데려오지 않고도 피격/사망 연출을 확인할 수 있다.
// [붙이는 곳] 오브젝트에 붙이지 않는다. 유니티가 인스펙터를 그릴 때 알아서 이 스크립트를 쓴다.
//            "Editor" 라는 이름의 폴더 안에 있는 스크립트는 에디터 전용이라, 빌드한 게임에는 들어가지 않는다.
//            (UnityEditor 네임스페이스는 빌드에 없으므로 반드시 Editor 폴더에 둬야 빌드가 깨지지 않는다)
// [연결] Player(추상 클래스)와 그 자식 전부(Warrior, RoguelikeWarrior 등)의 인스펙터에 적용된다.
// [설계] [CustomEditor(typeof(Player), true)] 의 두 번째 값 true 는 "자식 클래스에도 적용"이라는 뜻이다.
//        그래서 직업이 늘어나도 이 파일을 고칠 필요가 없다.
// 주의: 버튼은 플레이 모드에서만 눌러야 한다. 플레이 전에는 Awake 가 안 돌아서
//       애니메이터 같은 캐싱 변수가 비어 있어 에러가 난다.
[CustomEditor(typeof(Player), true)]      // 속성 (atrribute)
public class PlayerEditor : Editor 
{
    // 인스펙터를 그릴 때마다 유니티가 부른다. override 이므로 원래 그리기 방식을 이 내용으로 바꾼다
    public override void OnInspectorGUI()
    {
        // 기본적인 인스펙터 내용을 드로잉
        DrawDefaultInspector();

        // 적용 대상 클래스를 가져오기
        Player player = (Player)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("테스트", EditorStyles.boldLabel);
        // 입력칸의 처음 값은 플레이어 자신의 공격력이다. 값을 바꿔도 저장되지 않고 이번 클릭에만 쓰인다
        var atk = EditorGUILayout.FloatField("공격 데미지", player.AttackDamage);

        if (GUILayout.Button($"피격({atk})"))
        {
            player.TakeDamage(atk);
        }
    }
}