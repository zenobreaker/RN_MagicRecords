#if UNITY_EDITOR
using BT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


[CustomEditor(typeof(SO_Blackboard))]
public class BlackboardEditor : Editor
{

    // 사용할 데이터 타입들을 Enum으로 정의
    private enum BlackboardType
    {
        Int,
        Float,
        String,
        Vector3,
        GameObject
    }

    private static readonly Dictionary<BlackboardType, Type> TypeMap = new Dictionary<BlackboardType, Type>
    {
        { BlackboardType.Int, typeof(int) },
        { BlackboardType.Float, typeof(float) },
        { BlackboardType.String, typeof(string) },
        { BlackboardType.Vector3, typeof(Vector3) },
        { BlackboardType.GameObject, typeof(GameObject) }
    };

    private string newKey;
    private object newValue;

    private BlackboardType selectedType;
    private Type selectedCustomEnumType;
    private Enum selectedEnumValue;
    private List<Type> customEnums; // 사용자 정의 enum 타입 리스트

    private List<string> typeOptions;         // BlackboardType 및 커스텀 enum 이름 목록
    private int selectedTypeIndex = 0;        // Popup에서 선택한 인덱스


    private int selectedKeyIndex = 0;       // 블랙보드의 키를 선택한 인덱스

    private void OnEnable()
    {
        // 기본 enum 이름 추가 
        typeOptions = Enum.GetNames(typeof(BlackboardType)).ToList();

        // 애트리뷰트가 붙은 모든 enum 타입을 검색.
        customEnums = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsEnum && type.GetCustomAttribute<BlackboardTypeAttribute>() != null)
            .ToList();

        // 커스텀 enum 이름을 typeOptions에 추가 
        typeOptions.AddRange(customEnums.Select(t=>t.Name));
    }

    private void Test()
    {
        // 타입 선택 (여기서는 간단하게 int, float, string만 선택 가능)
        //selectedType = (BlackboardType)EditorGUILayout.EnumPopup("Type", selectedType);
        //int selectedTypeIndex = BlackboardTypeManager.Types.IndexOf(selectedType);
        //selectedTypeIndex = EditorGUILayout.Popup("Type", selectedTypeIndex, BlackboardTypeManager.Types.ToArray());
        //selectedType = BlackboardTypeManager.Types[selectedTypeIndex];
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SO_Blackboard blackboard = (SO_Blackboard)target;

        // 키 보여주는 섹션
        DisplayKeys(blackboard);

        // 키 추가 섹션
        AddKeySection(blackboard);

        //TODO: 값을 설정할 필요가 있을까? 블랙보드는 해당하는 이름의 키만 추가해서 게임내에서 처리할 것인데
        // 값 설정 섹션
        SetValueSection(blackboard);

        // 블랙보드 저장 섹션
        SaveBlackboardSection(blackboard);
    }

    private void SaveBlackboardSection(SO_Blackboard blackboard)
    {
        if (GUILayout.Button("Save"))
        {
            EditorUtility.SetDirty(blackboard);
            AssetDatabase.SaveAssets();
        }
    }

    private void DisplayKeys(SO_Blackboard blackboard)
    {
        GUILayout.Label("Current Keys", EditorStyles.boldLabel);

        var keys = blackboard.GetAllKeys();
        foreach (var key in keys)
        {
            EditorGUILayout.LabelField("Key Name:", key.Key);
            EditorGUILayout.LabelField("Key Type:", blackboard.GeteKeyType(key.Key).Name);
            EditorGUILayout.LabelField("Current Value:", key.Value.GetValue()?.ToString() ?? "null");
            EditorGUILayout.Space();
        }
    }

    private void AddKeySection(SO_Blackboard blackboard)
    {
        // 새로운 키 추가 섹션
        GUILayout.Label("Add New Key", EditorStyles.boldLabel);
        newKey = EditorGUILayout.TextField("Key Name", newKey);

        //'Popup'으로 기본 및 커스텀 enum 선택 
        selectedTypeIndex = EditorGUILayout.Popup("Type", selectedTypeIndex, typeOptions.ToArray());

        bool isDefault = true; 
        // 기본 `BlackboardType` 선택 시
        if (selectedTypeIndex < Enum.GetNames(typeof(BlackboardType)).Length)
        {
            selectedType = (BlackboardType)Enum.Parse(typeof(BlackboardType), typeOptions[selectedTypeIndex]);
            GUILayout.Label($"Selected BlackboardType: {selectedType}");
        }
        else
        {
            isDefault = false;
            // 커스텀 enum 선택 시
            int customEnumIndex = selectedTypeIndex - Enum.GetNames(typeof(BlackboardType)).Length;

            // typeOptions에서 해당 enum 타입의 전체 이름을 가져오기
            string enumTypeName = typeOptions[selectedTypeIndex];

            // enum의 네임스페이스 및 어셈블리 포함하여 전체 이름 구성
            string fullTypeName = $"{enumTypeName}, Assembly-CSharp"; 

            // 만약 어셈블리 이름이 필요하다면 다음과 같이 추가
            // string fullTypeName = $"YourNamespace.{enumTypeName}, YourAssemblyName";
            //$"Assembly-CSharp.클래스이름+{enumTypeName}"; // 클래스 내부에 있다면 다음과같이 변경

            var customEnumType = Type.GetType(fullTypeName);
            
            // 커스텀 enum의 값 표시
            if (customEnumType != null && customEnumType.IsEnum)
            {
                selectedCustomEnumType = customEnumType;
                Array enumValues = Enum.GetValues(customEnumType);
                selectedEnumValue = (Enum)EditorGUILayout.EnumPopup("Enum Value", selectedEnumValue ?? (Enum)enumValues.GetValue(0));
            }
            else
            {
                GUILayout.Label("Invalid Enum Type");
            }
        }


        if (GUILayout.Button("Add Key"))
        {
            Undo.RecordObject(blackboard, "Add Key");

            if (string.IsNullOrEmpty(newKey))
            {
                Debug.LogError("Key Name is Null or Empty!");
                return; 
            }
            if (isDefault)
            {
                if (TypeMap.TryGetValue(selectedType, out var type))
                    blackboard.AddKey(type, newKey);
            }
            else
                blackboard.AddKey(selectedCustomEnumType, newKey);

            // 변경사항 저장
            EditorUtility.SetDirty(blackboard);
            AssetDatabase.SaveAssets();
        }

    }

    // 값 설정 섹션
    void SetValueSection(SO_Blackboard blackboard)
    {
        if (blackboard.GetAllKeys().Count == 0)
        {
            GUILayout.Label("No keys available. Add key to use this section.", EditorStyles.helpBox);
            return;
        }

        // 키 드롭 다운선택 섹션
        GUILayout.Label("Set Value", EditorStyles.boldLabel);


        // 블랙 보드에 있는 모든 키들을 드롭다운에 표시
        List<string> allKeys = new List<string>(blackboard.GetAllKeys().Keys);
        selectedKeyIndex = EditorGUILayout.Popup("Select Key", selectedKeyIndex, allKeys.ToArray());
        string selectedKey = allKeys[selectedKeyIndex];


        // 현재 선택된 키의 타입에 맞춰 'newValue' 초기화 및 설정 
        Type keyType = blackboard.GeteKeyType(selectedKey);
        if(newValue == null || newValue.GetType() != keyType)
        {
            newValue = null;
        }

        // 키 타입에 따라 다른 입력 필드 표시
        if (keyType == typeof(int))
        {
            newValue = EditorGUILayout.IntField("Value", newValue != null ? (int)newValue : 0);
        }
        else if (keyType == typeof(float))
        {
            newValue = EditorGUILayout.FloatField("Value", newValue != null ? (float)newValue : 0f);
        }
        else if (keyType == typeof(string))
        {
            newValue = EditorGUILayout.TextField("Value", newValue != null ? (string)newValue : "");
        }
        else if (keyType.IsEnum)
        {
            newValue = EditorGUILayout.EnumPopup("Value", newValue != null ? (Enum)newValue : (Enum)Enum.GetValues(keyType).GetValue(0));
        }
        else
        {
            GUILayout.Label($"Unsupported type: {keyType}");
        }


        // Set Value 버튼 
        if (GUILayout.Button("Set Value"))
        {
            blackboard.SetValue(selectedKey, newValue);

            // 변경사항 저장
            EditorUtility.SetDirty(blackboard);
        }

        // 현재 블랙보드의 모든 키와 값 출력
        GUILayout.Label("Current Values", EditorStyles.boldLabel);
        foreach (KeyValuePair<string, IBlackboardKey> key in blackboard.GetAllKeys())
        {
            GUILayout.Label($"{key.Key}: {key.Value.GetValue()}");
        }

     
        // key 전부 지우기 
        if (GUILayout.Button("Clear Values"))
        {
            Undo.RecordObject(blackboard, "Clear Values");

            blackboard.ClearKeys();
            GUILayout.Label("All Clear");

            // 변경사항 저장
            EditorUtility.SetDirty(blackboard);
        }
    }

    public Type ConvertStringToType(string typeName)
    {
        switch (typeName)
        {
            case "Int":
            return typeof(int);
            case "Float":
            return typeof(float);
            case "String":
            return typeof(string);
            
            // 필요한 사용자 정의 타입들을 추가
            default:
            Debug.LogError($"Type '{typeName}' is not defined!");
            return null;
        }
    }
}


#endif