using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SkillModule), true)]
public class SkillModuleDrawer : PropertyDrawer
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
        if (GUI.Button(buttonRect, new GUIContent("+", "모듈 타입 변경")))
        {
            ShowCategoryMenu(property);
        }

        EditorGUI.EndProperty();
    }

    // 💡 [핵심] 카테고리 메뉴를 그리는 로직 (무결점 리플렉션 버전)
    private void ShowCategoryMenu(SerializedProperty property)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("None / Clear"), false, () => {
            property.serializedObject.Update();
            property.managedReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
        });

        menu.AddSeparator("");

        var types = TypeCache.GetTypesDerivedFrom<SkillModule>()
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var type in types)
        {
            string menuPath = type.Name; // 기본값

            // 💡 [강제 리플렉션] typeof() 비교를 안 쓰고, 어트리뷰트 이름으로 냅다 찾아버립니다!
            // 이렇게 하면 AssemblyDefinition 설정이 꼬여있어도 100% 무조건 찾아옵니다.
            object[] attributes = type.GetCustomAttributes(true);
            foreach (var attr in attributes)
            {
                if (attr.GetType().Name == "ModuleCategoryAttribute") // 이름이 같으면!
                {
                    // Path 프로퍼티의 값을 강제로 뽑아옵니다.
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