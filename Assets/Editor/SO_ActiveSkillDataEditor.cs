using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SO_ActiveSkillData))]
public class SO_ActiveSkillDataEditor : Editor
{
    // 접힘 상태 변수 (static으로 선언하면 다른 파일을 클릭해도 상태가 유지됩니다)
    private static bool showSkillInfo = true;
    private static bool showLeadingIds = false;
    private static bool showSettings = true;
    private static bool showPhases = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ---------------------------------------------------------
        // 1. Skill Info Section
        // ---------------------------------------------------------
        showSkillInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showSkillInfo, "▣ Skill Info");
        EditorGUILayout.EndFoldoutHeaderGroup(); // 헤더를 바로 닫아 중첩 오류 방지

        if (showSkillInfo)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("skillName"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("skillDescription"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("learnableLevel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxLevel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("skillUpgradeCost"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("skillImage"));
            }
            EditorGUILayout.Space(10);
        }

        // ---------------------------------------------------------
        // 2. Leading Skill Section
        // ---------------------------------------------------------
        showLeadingIds = EditorGUILayout.BeginFoldoutHeaderGroup(showLeadingIds, "▣ Skill Leading ID's");
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (showLeadingIds)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("leadingSkillList"));
            }
            EditorGUILayout.Space(10);
        }

        // ---------------------------------------------------------
        // 3. Skill Settings Section
        // ---------------------------------------------------------
        showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSettings, "▣ Skill Settings");
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (showSettings)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cost"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldown"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("limitCooldown"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("castingTime"));
            }
            EditorGUILayout.Space(10);
        }

        // ---------------------------------------------------------
        // 4. Phase Section
        // ---------------------------------------------------------
        showPhases = EditorGUILayout.BeginFoldoutHeaderGroup(showPhases, "▣ Phase List");
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (showPhases)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("phaseList"));
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}