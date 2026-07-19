#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class RecordTestWindow : EditorWindow
{
    private const string RecordFolder = "Assets/10.ScriptableObjects/Records";
    private const float LeftPanelWidth = 270f;
    private const float GridItemWidth = 96f;

    private readonly List<SO_RecordData> records = new();
    private SO_RecordData selectedRecord;
    private Vector2 scrollPos;
    private string searchFilter = "";
    private TargetFilterType selectedTarget = TargetFilterType.ALL;

    [MenuItem("Tools/Record Tester")]
    public static void ShowWindow()
    {
        RecordTestWindow window = GetWindow<RecordTestWindow>("Record Tester");
        window.minSize = new Vector2(860f, 520f);
        window.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(860f, 520f);
        RefreshRecords();
    }

    private void RefreshRecords()
    {
        records.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:SO_RecordData", new[] { RecordFolder }))
        {
            SO_RecordData record = AssetDatabase.LoadAssetAtPath<SO_RecordData>(AssetDatabase.GUIDToAssetPath(guid));
            if (record != null)
                records.Add(record);
        }
        records.Sort((left, right) => left.id.CompareTo(right.id));
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("레코드 즉시 지급 도구", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        EditorGUILayout.Space(6f);
        DrawRecordList();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(LeftPanelWidth), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("필터", EditorStyles.boldLabel);
        searchFilter = EditorGUILayout.TextField("이름 검색", searchFilter);
        selectedTarget = (TargetFilterType)EditorGUILayout.EnumPopup("대상", selectedTarget);
        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("선택한 레코드", EditorStyles.boldLabel);
        if (selectedRecord == null)
        {
            EditorGUILayout.HelpBox("오른쪽 목록에서 지급할 레코드를 선택하세요.", MessageType.Info);
        }
        else
        {
            Texture2D icon = selectedRecord.icon != null ? selectedRecord.icon.texture : null;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(icon, GUILayout.Width(64f), GUILayout.Height(64f));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(selectedRecord.recordName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"ID : {selectedRecord.id}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"대상 : {selectedRecord.targetFilter}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6f);
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("선택 레코드 지급", GUILayout.Height(28f)))
                GrantRecord(selectedRecord);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("레코드 선택 해제"))
                selectedRecord = null;
        }

        EditorGUILayout.Space(8f);
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        if (GUILayout.Button("UI 강제 호출 (랜덤 3개)", GUILayout.Height(28f)))
        {
            AppManager.Instance.GenerateRecord_Test(3);
            GameManager.Instance.OnPrecessBattle();
        }
        EditorGUI.EndDisabledGroup();
        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("레코드 지급은 플레이 모드에서만 가능합니다.", MessageType.Warning);
        EditorGUILayout.EndVertical();
    }

    private void DrawRecordList()
    {
        IEnumerable<SO_RecordData> filteredRecords = records.Where(record =>
        {
            bool nameMatch = string.IsNullOrEmpty(searchFilter)
                || record.recordName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            bool targetMatch = selectedTarget == TargetFilterType.ALL || record.targetFilter == selectedTarget;
            return nameMatch && targetMatch;
        });

        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"레코드 목록 ({filteredRecords.Count()})", EditorStyles.boldLabel);
        if (GUILayout.Button("새로고침", GUILayout.Width(70f)))
            RefreshRecords();
        EditorGUILayout.EndHorizontal();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - LeftPanelWidth - 40f) / GridItemWidth));
        int index = 0;
        foreach (SO_RecordData record in filteredRecords)
        {
            if (index % columns == 0)
                EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(GridItemWidth - 4f));
            Texture2D icon = record.icon != null ? record.icon.texture : null;
            Color previousColor = GUI.backgroundColor;
            if (record == selectedRecord)
                GUI.backgroundColor = new Color(0.45f, 0.75f, 1f);
            if (GUILayout.Button(icon, GUILayout.Width(64f), GUILayout.Height(64f)))
                selectedRecord = record;
            GUI.backgroundColor = previousColor;
            EditorGUILayout.LabelField(record.recordName, EditorStyles.miniLabel, GUILayout.Width(GridItemWidth - 4f));
            EditorGUILayout.LabelField($"[{record.id}] {record.targetFilter}", EditorStyles.miniLabel, GUILayout.Width(GridItemWidth - 4f));
            EditorGUILayout.EndVertical();
            index++;
            if (index % columns == 0)
                EditorGUILayout.EndHorizontal();
        }
        if (index % columns != 0)
            EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void GrantRecord(SO_RecordData recordAsset)
    {
        RecordData record = recordAsset.GetRecordData();
        AppManager.Instance.OnRecordSelected(record);
        RecordImporter.AddRecordDirectly(record);
        GameManager.Instance.OnPrecessBattle();
    }
}
#endif
