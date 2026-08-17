using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(SO_ActiveSkillData))]
public class SO_ActiveSkillDataEditor : Editor
{
    // 접힘 상태 변수 (static으로 선언하면 다른 파일을 클릭해도 상태가 유지됩니다)
    private static bool showSkillInfo = true;
    private static bool showLeadingIds = false;
    private static bool showSettings = true;
    private static bool showDatas= true;
    private static bool showPhases = true;
    private ReorderableList phaseList;

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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("levelDatas"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("range"));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldown"));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("limitCooldown"));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("castingTime"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isConcurrentSkill"));
            }
            EditorGUILayout.Space(10);
        }


        showDatas = EditorGUILayout.BeginFoldoutHeaderGroup(showDatas, "▣ Skill Datas");
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (showDatas)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("actionData"));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("damageData"));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("bonusOptionList"));
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
                GetPhaseList().DoLayoutList();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private ReorderableList GetPhaseList()
    {
        SerializedProperty phaseListProperty = serializedObject.FindProperty("phaseList");
        if (phaseList != null &&
            phaseList.serializedProperty.serializedObject.targetObject == target &&
            phaseList.serializedProperty.propertyPath == phaseListProperty.propertyPath)
            return phaseList;

        phaseList = new ReorderableList(serializedObject, phaseListProperty, true, true, true, true);
        phaseList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Phase List");

        phaseList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SerializedProperty element = phaseList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2f;
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUI.GetPropertyHeight(element, true)),
                element,
                new GUIContent($"Phase {index}"),
                true);
        };

        phaseList.elementHeightCallback = index =>
        {
            SerializedProperty element = phaseList.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + 4f;
        };

        phaseList.onAddCallback = _ => AddIndependentPhase();
        return phaseList;
    }

    private void AddIndependentPhase()
    {
        serializedObject.ApplyModifiedProperties();
        SO_ActiveSkillData skillData = (SO_ActiveSkillData)target;
        Undo.RecordObject(skillData, "Add Independent Skill Phase");

        if (skillData.phaseList == null)
            skillData.phaseList = new System.Collections.Generic.List<PhaseSkill>();

        // 새 Phase와 모듈 리스트를 직접 생성하여 기존 Phase의 SerializeReference를 공유하지 않습니다.
        skillData.phaseList.Add(new PhaseSkill());
        EditorUtility.SetDirty(skillData);
        serializedObject.Update();
        phaseList.index = skillData.phaseList.Count - 1;
    }
}
