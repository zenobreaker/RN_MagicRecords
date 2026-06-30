using System;
using System.Linq;
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
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("None / Clear"), false, () => {
            property.serializedObject.Update();
            property.managedReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
        });

        menu.AddSeparator("");

        // 💡 PassiveModule을 상속받은 클래스들만 긁어옵니다.
        var types = TypeCache.GetTypesDerivedFrom<PassiveModule>()
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var type in types)
        {
            string menuPath = type.Name; // 기본값

            // [ModuleCategory] 어트리뷰트를 이용한 폴더링 (무결점 리플렉션 버전)
            object[] attributes = type.GetCustomAttributes(true);
            foreach (var attr in attributes)
            {
                if (attr.GetType().Name == "ModuleCategoryAttribute")
                {
                    var pathProperty = attr.GetType().GetProperty("Path");
                    if (pathProperty != null)
                    {
                        string pathValue = pathProperty.GetValue(attr) as string;
                        if (!string.IsNullOrEmpty(pathValue))
                        {
                            menuPath = pathValue;
                        }
                    }
                    break;
                }
            }

            menu.AddItem(new GUIContent(menuPath), false, () => {
                property.serializedObject.Update();
                property.managedReferenceValue = Activator.CreateInstance(type);
                property.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }
}