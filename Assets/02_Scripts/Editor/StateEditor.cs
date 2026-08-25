using UnityEditor;
using DungeonMaster.Character.Enemy;
using DungeonMaster.Character.Enemy.FSM;
using UnityEngine;

[CustomEditor(typeof(Enemy), true)]      // 속성 (atrribute)
public class StateEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        // 기본적인 인스펙터 내용을 드로잉
        DrawDefaultInspector();

        // 적용 대상 클래스를 가져오기
        Enemy enemy = (Enemy)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("상태 머신 테스트", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서만 상태를 전환할 수 있습니다.", MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
        if (GUILayout.Button("Idle"))   enemy.ChangeState<IdleState>();
        if (GUILayout.Button("Chase"))  enemy.ChangeState<ChaseState>();
        if (GUILayout.Button("Attack")) enemy.ChangeState<AttackState>();
        }
    }
}