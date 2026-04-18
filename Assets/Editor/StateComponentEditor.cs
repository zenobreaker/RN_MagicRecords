using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StateComponent))]
public class StateComponentEditor : Editor
{
    // 💡 플레이 모드 중 상태가 바뀔 때 인스펙터를 실시간으로 새로고침합니다.
    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }

    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 그리기
        base.OnInspectorGUI();

        StateComponent stateComp = (StateComponent)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🔍 실시간 상태 디버거", EditorStyles.boldLabel);

        // 상태별로 배경색 다르게 지정
        Color stateColor = GetStateColor(stateComp.Type);

        // 기존 색상 저장
        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = stateColor;

        // 큼직하고 예쁜 박스 스타일 생성
        GUIStyle stateStyle = new GUIStyle(GUI.skin.box)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        // 박스 그리기
        EditorGUILayout.BeginVertical("helpbox");
        GUILayout.Box($"Current State : {stateComp.Type}", stateStyle, GUILayout.Height(30), GUILayout.ExpandWidth(true));
        EditorGUILayout.EndVertical();

        // 색상 원상복구
        GUI.backgroundColor = oldColor;

        // 플레이 모드가 아닐 때 안내 문구
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("게임이 실행되면 상태가 실시간으로 표시됩니다.", MessageType.Info);
        }
    }

    // 💡 씬(Scene) 뷰에서 캐릭터 머리 위에 텍스트를 띄워줍니다! (진짜 유용함)
    private void OnSceneGUI()
    {
        if (!Application.isPlaying) return;

        StateComponent stateComp = (StateComponent)target;
        if (stateComp == null || !stateComp.gameObject.activeInHierarchy) return;

        // 캐릭터 머리 위쪽(대략 2유닛 위) 위치 계산
        Vector3 labelPosition = stateComp.transform.position + Vector3.up * 2.2f;

        // 텍스트 스타일 세팅
        GUIStyle style = new GUIStyle();
        style.normal.textColor = GetStateColor(stateComp.Type);
        style.fontSize = 15;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        // 씬 뷰에 글자 그리기
        Handles.BeginGUI();
        Vector2 screenPos = HandleUtility.WorldToGUIPoint(labelPosition);

        // 글자에 검은색 외곽선을 줘서 잘 보이게 만듦
        DrawOutlineText(new Rect(screenPos.x - 50, screenPos.y - 20, 100, 40), stateComp.Type.ToString(), style);
        Handles.EndGUI();
    }

    // 상태별 직관적인 컬러 반환 함수
    private Color GetStateColor(StateType type)
    {
        switch (type)
        {
            case StateType.Idle: return Color.green;
            case StateType.Equip: return Color.cyan;
            case StateType.Action: return new Color(1f, 0.6f, 0f); // 주황색
            case StateType.Evade: return new Color(0.2f, 0.6f, 1f); // 파란색
            case StateType.Damaged: return Color.red;
            case StateType.Stop: return Color.magenta;
            case StateType.Dead: return Color.gray;
            default: return Color.white;
        }
    }

    // 텍스트 외곽선 렌더링 헬퍼
    private void DrawOutlineText(Rect rect, string text, GUIStyle style)
    {
        Color originalColor = style.normal.textColor;
        style.normal.textColor = Color.black;

        // 상하좌우로 1픽셀씩 밀어서 검은색 먼저 그림
        GUI.Label(new Rect(rect.x - 1, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + 1, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y - 1, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y + 1, rect.width, rect.height), text, style);

        // 원래 색상으로 덮어쓰기
        style.normal.textColor = originalColor;
        GUI.Label(rect, text, style);
    }
}