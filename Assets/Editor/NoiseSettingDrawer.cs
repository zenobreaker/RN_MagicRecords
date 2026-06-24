using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


[CustomPropertyDrawer(typeof(NoiseSettings))]
public class NoiseSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {

        Debug.Log("DRAWER!");
        // UI Toolkit을 무시하고 레거시 ObjectField로 강제 렌더링
        //property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(NoiseSettings), false);
        EditorGUI.ObjectField(
         position,
         property,
         typeof(Unity.Cinemachine.NoiseSettings),
         label);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        return new PropertyField(property);
    }
}
