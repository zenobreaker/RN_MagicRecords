using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static Codice.CM.Common.CmCallContext;
using static UnityEngine.Rendering.DebugUI;

[CustomPropertyDrawer(typeof(SelectImplementationAttribute))]
public class SelectImplementationDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. 라벨 설정 (생략 가능하나 가독성을 위해 유지)
        if (property.managedReferenceValue != null)
        {
            var triggerTimeProp = property.FindPropertyRelative("triggerTime");
            string triggerText = triggerTimeProp != null ? $"[{triggerTimeProp.enumDisplayNames[triggerTimeProp.enumValueIndex]}] " : "";
            string typeName = property.managedReferenceValue.GetType().Name.Replace("Module_", "");
            label.text = $"{triggerText}{typeName}";
        }
        else
        {
            label.text = $"{label.text} (Empty - Click +)";
        }


        EditorGUI.BeginProperty(position, label, property);

        // 2. 헤더 영역
        Rect headerRect = new Rect(position.x, position.y, position.width - 30, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

        if (GUI.Button(new Rect(position.xMax - 25, position.y, 25, EditorGUIUtility.singleLineHeight), "+"))
        {
            ShowTypeMenu(property);
        }

        // 3. 내부 필드 수동 렌더링
        if (property.isExpanded && property.managedReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            float currentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // [수정] 필드 목록을 가져올 때, 부모 클래스(SkillModule)의 필드부터 가져오도록 정렬하거나 
            // triggerTime을 수동으로 먼저 그립니다.
            Type type = property.managedReferenceValue.GetType();

            // BindingFlags를 수정하여 부모의 public 필드도 포함하도록 합니다.
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            // triggerTime 필드를 먼저 찾아서 그립니다.
            var triggerField = fields.FirstOrDefault(f => f.Name == "triggerTime");
            if (triggerField != null)
            {
                DrawField(triggerField);
            }


            // [변경점] 필드 반복문 밖에서 "딱 한 번만" 체크하고 경고창 그리기
            if (property.managedReferenceValue is Module_SpawnWarningSign signModule)
            {
                var moduleProp = property.serializedObject.FindProperty(GetParentPath(property.propertyPath));
                if (moduleProp != null && moduleProp.isArray)
                {
                    bool hasTargetModule = false;
                    for (int i = 0; i < moduleProp.arraySize; i++)
                    {
                        var m = moduleProp.GetArrayElementAtIndex(i).managedReferenceValue;
                        if (m is Module_SetTargetByPerception)
                        {
                            hasTargetModule = true;
                            break;
                        }
                    }

                    if (!hasTargetModule)
                    {
                        // EditorGUILayout은 수동 Rect 계산이 필요 없지만 
                        // PropertyDrawer 내부에선 위치가 꼬일 수 있으므로 
                        // 가급적 EditorGUI.HelpBox를 쓰고 currentY를 더해주는 게 안전합니다.
                        float helpBoxHeight = 40f;
                        Rect helpBoxRect = new Rect(position.x, currentY, position.width, helpBoxHeight);

                        EditorGUI.HelpBox(helpBoxRect,
                            "Warning! 'Module_SetTargetByPerception'이 리스트에 없습니다.\n타겟 위치가 설정되지 않아 오작동할 수 있습니다.",
                            MessageType.Warning);

                        currentY += helpBoxHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }


            // 나머지 필드들을 그립니다 (triggerTime 제외)
            foreach (var field in fields)
            {
                if (field.Name == "triggerTime") continue;
                DrawField(field);
            }

            // 로컬 함수로 드로잉 로직 분리 (중복 제거)
            void DrawField(FieldInfo field)
            {
                SerializedProperty childProp = property.FindPropertyRelative(field.Name);
                if (childProp == null) return;

                // 타입별 가시성 필터링 추가 
                if(property.managedReferenceValue is Module_SpawnWarningSign signModule)
                {
                    // 특정 프로퍼티 숨기기 
                    bool shouldShow = true; 
                    switch(field.Name)
                    {
                        case "radius": shouldShow = signModule.signType == WarningSignType.Circle;break; 
                        case "rectSize": shouldShow = signModule.signType == WarningSignType.Rectangle;break; 
                        case "maxRectSize": shouldShow = signModule.signType == WarningSignType.Rectangle;break; 
                        case "fanRadius": shouldShow = signModule.signType == WarningSignType.Fan;break; 
                        case "fanAngle": shouldShow = signModule.signType == WarningSignType.Fan;break; 
                    }
                    if (!shouldShow) return;  // 해당 타입이 아니면 그리지 않음
                }

                //////////////////////////////////////////////

                float childHeight = EditorGUI.GetPropertyHeight(childProp, true);
                Rect childRect = new Rect(position.x, currentY, position.width, childHeight);

                if (field.FieldType == typeof(Unity.Cinemachine.NoiseSettings))
                {
                    childProp.objectReferenceValue = EditorGUI.ObjectField(
                        childRect,
                        new GUIContent(childProp.displayName),
                        childProp.objectReferenceValue,
                        typeof(Unity.Cinemachine.NoiseSettings),
                        false
                    );
                }
                else
                {
                    EditorGUI.PropertyField(childRect, childProp, true);
                }

                currentY += childHeight + EditorGUIUtility.standardVerticalSpacing;
            }// End DrawField 

            EditorGUI.indentLevel--;

           
        }// End  if (property.isExpanded && property.managedReferenceValue != null)

        EditorGUI.EndProperty();
    }

    // 높이 계산 로직도 동일하게 필드 기반으로 수정
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded || property.managedReferenceValue == null)
            return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        var fields = property.managedReferenceValue.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            SerializedProperty childProp = property.FindPropertyRelative(field.Name);
            if (childProp != null)
                height += EditorGUI.GetPropertyHeight(childProp, true) + EditorGUIUtility.standardVerticalSpacing;
        }
        return height;
    }

    private void ShowTypeMenu(SerializedProperty property)
    {
        Type targetType = GetTargetType(property);
        if (targetType == null) return;
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => targetType.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);

        GenericMenu menu = new GenericMenu();
        foreach (var type in types)
        {
            menu.AddItem(new GUIContent(type.Name), false, () =>
            {
                property.managedReferenceValue = Activator.CreateInstance(type);
                property.isExpanded = true;
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
            });
        }
        menu.ShowAsContext();
    }

    private Type GetTargetType(SerializedProperty property)
    {
        if (fieldInfo != null)
        {
            Type type = fieldInfo.FieldType;
            if (type.IsGenericType) return type.GetGenericArguments()[0];
            return type;
        }
        return null;
    }

    // 부모 경로를 찾는 헬퍼 (배열 내 요소일 경우 사용)
    private string GetParentPath(string path)
    {
        int lastDot = path.LastIndexOf(".Array.data[");
        if (lastDot == -1) return "";
        return path.Substring(0, lastDot);
    }
}