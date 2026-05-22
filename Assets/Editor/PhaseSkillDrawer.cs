using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomPropertyDrawer(typeof(PhaseSkill))]
public class PhaseSkillDrawer : PropertyDrawer
{
    private Dictionary<string, ReorderableList> listCache = new Dictionary<string, ReorderableList>();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if (property.isExpanded)
        {
            SerializedProperty prop = property.Copy();
            SerializedProperty endProp = prop.GetEndProperty();
            prop.NextVisible(true);

            while (!SerializedProperty.EqualContents(prop, endProp))
            {
                if (prop.name == "modules") // 💡 실제 변수명과 일치해야 합니다!
                {
                    // 수정됨: property를 직접 넘김
                    height += GetList(property).GetHeight() + EditorGUIUtility.standardVerticalSpacing;
                }
                else
                {
                    height += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
                }
                prop.NextVisible(false);
            }
        }
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        property.isExpanded = EditorGUI.Foldout(currentRect, property.isExpanded, label, true);
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            SerializedProperty prop = property.Copy();
            SerializedProperty endProp = prop.GetEndProperty();
            prop.NextVisible(true);

            while (!SerializedProperty.EqualContents(prop, endProp))
            {
                if (prop.name == "modules")
                {
                    Rect listRect = new Rect(currentRect.x + 15, currentRect.y, currentRect.width - 15, currentRect.height);

                    // 수정됨: property를 직접 넘겨서 안정적인 참조를 만듦
                    var list = GetList(property);
                    list.DoList(listRect);

                    currentRect.y += list.GetHeight() + EditorGUIUtility.standardVerticalSpacing;
                }
                else
                {
                    float h = EditorGUI.GetPropertyHeight(prop, true);
                    EditorGUI.PropertyField(new Rect(currentRect.x, currentRect.y, currentRect.width, h), prop, true);
                    currentRect.y += h + EditorGUIUtility.standardVerticalSpacing;
                }
                prop.NextVisible(false);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    // 💡 [핵심 수정 구간] 움직이는 이터레이터를 캐싱하지 않고, 고정된 참조를 찾아서 캐싱합니다!
    private ReorderableList GetList(SerializedProperty parentProperty)
    {
        string key = parentProperty.propertyPath;

        if (!listCache.ContainsKey(key))
        {
            // 움직이지 않는 안전한 프로퍼티 복사본을 직접 가져옵니다.
            SerializedProperty modulesProperty = parentProperty.FindPropertyRelative("modules");

            ReorderableList list = new ReorderableList(modulesProperty.serializedObject, modulesProperty, true, true, true, true);

            list.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, modulesProperty.displayName);
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);

                // 위아래 여백 살짝 띄우기
                rect.y += 2;

                // 💡 [핵심 해결책] 왼쪽 햄버거(드래그) 아이콘이 숨 쉴 공간(약 15px)을 비워주고 오른쪽으로 밉니다!
                float dragHandleSpace = 15f;
                rect.x += dragHandleSpace;
                rect.width -= dragHandleSpace;

                // 밀어낸 공간을 기준으로 프로퍼티(모듈)를 그립니다.
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUI.GetPropertyHeight(element)), element, true);
            };

            list.elementHeightCallback = (int index) => {
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 4;
            };

            // [+] 버튼 클릭 시 카테고리 팝업
            list.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) => {
                ShowCategoryMenu(l.serializedProperty);
            };

            listCache[key] = list;
        }

        return listCache[key];
    }

    private void ShowCategoryMenu(SerializedProperty listProperty)
    {
        GenericMenu menu = new GenericMenu();

        var types = TypeCache.GetTypesDerivedFrom<SkillModule>()
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var type in types)
        {
            string menuPath = type.Name;

            var attributes = type.GetCustomAttributes(typeof(ModuleCategoryAttribute), false);
            if (attributes.Length > 0)
            {
                menuPath = ((ModuleCategoryAttribute)attributes[0]).Path;
            }

            menu.AddItem(new GUIContent(menuPath), false, () => {
                listProperty.serializedObject.Update();

                int index = listProperty.arraySize;
                listProperty.arraySize++;
                SerializedProperty newElement = listProperty.GetArrayElementAtIndex(index);

                newElement.managedReferenceValue = Activator.CreateInstance(type);

                listProperty.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }
}