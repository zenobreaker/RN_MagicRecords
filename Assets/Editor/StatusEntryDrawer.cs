using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;


[CustomPropertyDrawer(typeof(StatusEntry))]
public class StatusEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty typeProp = property.FindPropertyRelative("type");
        SerializedProperty valueProp = property.FindPropertyRelative("value");

        // 타입 이름을 라벨로 사용하기
        label = new GUIContent(typeProp.enumDisplayNames[typeProp.enumValueIndex]);

        // 기본 프로퍼티 필드 그리기
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
