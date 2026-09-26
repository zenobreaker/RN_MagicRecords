using UnityEditor;
using UnityEngine;

// 💡 타겟을 PassiveModule로 변경
[CustomPropertyDrawer(typeof(PassiveModule), true)]
public class PassiveModuleDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 1. 기본 프로퍼티 그리기
        EditorGUI.PropertyField(position, property, label, true);

        // 2. 우측 [+] 버튼 위치 계산 (유니티 기본 UI와 겹치지 않게 살짝 여백)
        float buttonWidth = 22f;
        Rect buttonRect = new Rect(
            position.x + position.width - buttonWidth,
            position.y,
            buttonWidth,
            EditorGUIUtility.singleLineHeight
        );

        // 3. 버튼 클릭 이벤트
        if (GUI.Button(buttonRect, new GUIContent("+", "패시브 모듈 변경")))
        {
            ShowCategoryMenu(property);
        }

        EditorGUI.EndProperty();
    }

    private void ShowCategoryMenu(SerializedProperty property)
    {
        var menu = new GenericMenu();
        var target = property.serializedObject.targetObject;
        string path = property.propertyPath;

        void SetModule(PassiveModule module)
        {
            if (target == null) return;
            using (var data = new SerializedObject(target))
            {
                data.Update();
                var current = data.FindProperty(path);
                if (current == null) return;
                current.managedReferenceValue = module;
                data.ApplyModifiedProperties();
            }
        }

        menu.AddItem(new GUIContent("None / Clear"), false, () => SetModule(null));
        menu.AddSeparator("");
        PassiveModuleMenu.AddItems(menu, SetModule);
        menu.ShowAsContext();
    }
}
