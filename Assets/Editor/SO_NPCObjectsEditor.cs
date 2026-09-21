using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(SO_NPCObjects))]
[CanEditMultipleObjects]
public sealed class SO_NPCObjectsEditor : Editor
{
    private SerializedProperty listProperty;
    private ReorderableList npcList;

    private void OnEnable()
    {
        listProperty = serializedObject.FindProperty("list");
        npcList = new ReorderableList(serializedObject, listProperty, true, true, true, true);
        npcList.drawHeaderCallback = DrawHeader;
        npcList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index >= listProperty.arraySize) return;
            var element = listProperty.GetArrayElementAtIndex(index);
            var id = element.FindPropertyRelative("id");
            string characterId = id.hasMultipleDifferentValues ? "—" : id.intValue.ToString();
            rect.xMin += 12f;
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            rect.height = EditorGUI.GetPropertyHeight(element, true);
            EditorGUI.PropertyField(rect, element, new GUIContent($"{index}_{characterId}"), true);
        };
        npcList.elementHeightCallback = index =>
            (index < listProperty.arraySize
                ? EditorGUI.GetPropertyHeight(listProperty.GetArrayElementAtIndex(index), true)
                : EditorGUIUtility.singleLineHeight) + EditorGUIUtility.standardVerticalSpacing * 2f;
    }

    private void DrawHeader(Rect rect)
    {
        var sizeRect = new Rect(rect.xMax - 50f, rect.y, 50f, EditorGUIUtility.singleLineHeight);
        rect.xMax = sizeRect.xMin - 8f;
        rect.xMin += 12f;
        listProperty.isExpanded = EditorGUI.Foldout(rect, listProperty.isExpanded,
            listProperty.displayName, true);
        EditorGUI.PropertyField(sizeRect, listProperty.FindPropertyRelative("Array.size"), GUIContent.none);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        if (listProperty.isExpanded)
            npcList.DoLayoutList();
        else
            DrawHeader(EditorGUILayout.GetControlRect());

        DrawPropertiesExcluding(serializedObject, "m_Script", "list");
        serializedObject.ApplyModifiedProperties();
    }
}
