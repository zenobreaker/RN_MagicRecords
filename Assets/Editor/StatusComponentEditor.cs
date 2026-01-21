using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(StatusComponent))]
public class StatusComponentEditor : Editor
{
    private bool showStats = true;

    public override void OnInspectorGUI()
    {
        // 기존 인스펙터 속성들 (SerializedProperty) 출력
        base.OnInspectorGUI();

        StatusComponent component = (StatusComponent)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical("box");

        showStats = EditorGUILayout.BeginFoldoutHeaderGroup(showStats, "실시간 최종 스탯 (Final Values)");

        if (showStats)
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("최종 스탯은 플레이 모드에서만 실시간으로 확인 가능합니다.", MessageType.Info);
            }

            // 필터 타입 표시
            EditorGUILayout.LabelField("Character Job", component.FilterType.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 모든 StatusType 순회하며 표시
            // Enum.GetValues를 통해 자동으로 모든 스탯 타입을 가져옵니다.
            foreach (StatusType type in System.Enum.GetValues(typeof(StatusType)))
            {
                float finalVal = component.GetStatusValue(type);

                // 스탯 이름과 값을 한 줄에 표시
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(type.ToString(), GUILayout.Width(120));

                // 값의 크기에 따라 색상 강조 (예: 공격력이 0보다 크면 녹색)
                GUI.color = finalVal > 0 ? Color.green : Color.white;
                EditorGUILayout.LabelField(finalVal.ToString("F2"), EditorStyles.numberField);
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("데이터 강제 갱신 (Recalculate)"))
            {
                // IsDirty를 강제로 발생시키기 위해 현재 값을 재설정하거나 
                // GetStatusValue 호출만으로도 Recalculate가 실행됩니다.
                Repaint();
            }
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.EndVertical();
    }

    // 씬 뷰에서 오브젝트를 선택하지 않아도 정보를 살짝 띄워주고 싶을 때 (선택 사항)
    private void OnSceneGUI()
    {
        StatusComponent component = (StatusComponent)target;
        Handles.Label(component.transform.position + Vector3.up * 2f,
            $"[HP: {component.GetCurrentHP()}/{component.GetMaxHP()}]\nJob: {component.FilterType}");
    }
}