using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;

[CustomPropertyDrawer(typeof(NoiseSettings))]
public class NoiseSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // UI Toolkit을 무시하고 레거시 ObjectField로 강제 렌더링
        property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(NoiseSettings), false);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
