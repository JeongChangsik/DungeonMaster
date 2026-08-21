using UnityEditor;
using DungeonMaster.Character.Player;
using UnityEngine;

[CustomEditor(typeof(Player), true)]      // 속성 (atrribute)
public class PlayerEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        // 기본적인 인스펙터 내용을 드로잉
        DrawDefaultInspector();

        // 적용 대상 클래스를 가져오기
        Player player = (Player)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("테스트", EditorStyles.boldLabel);
        var atk = EditorGUILayout.FloatField("공격 데미지", player.AttackDamage);

        if (GUILayout.Button($"피격({atk})"))
        {
            player.TakeDamage(atk);
        }
    }
}