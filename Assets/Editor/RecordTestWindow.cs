#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

public class RecordTestWindow : EditorWindow
{
    [MenuItem("Tools/Record Tester")]
    public static void ShowWindow() => GetWindow<RecordTestWindow>("Record Tester");

    Vector2 scrollPos;
    string searchFilter = "";
    TargetFilterType selectedTarget = TargetFilterType.ALL;

    void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서만 사용 가능합니다.", MessageType.Warning);
            return;
        }

        GUILayout.Label("레코드 즉시 지급 도구", EditorStyles.boldLabel);
        
        // --- 필터링 UI 구역 ---
        EditorGUILayout.BeginVertical("helpbox");
        searchFilter = EditorGUILayout.TextField("이름 검색", searchFilter);
        selectedTarget = (TargetFilterType)EditorGUILayout.EnumPopup("대상 필터 (Target)", selectedTarget);
        EditorGUILayout.EndVertical();
        // ----------------------
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // DataManager에 저장된 전체 레코드 리스트 순회
        var records = AppManager.Instance.GetAllRecordData();
        if (records == null)
        {
            EditorGUILayout.EndScrollView();
            return;
        }

        // LINQ를 사용하여 이름과 타겟 필터를 동시에 필터링
        var filteredRecords = records.Where(r =>
        {
            // 이름 검색어 체크 
            bool nameMatch = string.IsNullOrEmpty(searchFilter) || r.recordName.Contains(searchFilter);

            // 타겟 필터 체크 (ALL 일 경우 모두 토오가, 아니면 문자열 비교
            bool targetMatch = (selectedTarget == TargetFilterType.ALL) ||
                                (r.targetFilter.ToString().Equals(selectedTarget.ToString()));

            return nameMatch && targetMatch;
        });

        foreach (var record in records)
        {
            if (!string.IsNullOrEmpty(searchFilter) && !record.recordName.Contains(searchFilter))
                continue;

            EditorGUILayout.BeginHorizontal("box");
            
            EditorGUILayout.LabelField($"[{record.id}] {record.recordName}", GUILayout.Width(200));
            EditorGUILayout.LabelField($"<{record.targetFilter}>", EditorStyles.miniLabel, GUILayout.Width(80));

            if (GUILayout.Button("지급", GUILayout.Width(60)))
            {
                AppManager.Instance.OnRecordSelected(record);
                RecordImporter.AddRecordDirectly(record);
                GameManager.Instance.SetProcessBattle();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("UI 강제 호출 (랜덤 3개)", GUILayout.Height(30)))
        {
            AppManager.Instance.GenerateRecord(3);
            GameManager.Instance.SetProcessBattle();
        }
    }
}
#endif