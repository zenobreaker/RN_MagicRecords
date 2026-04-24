using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PropPlacerTool : EditorWindow
{
    [Header("Settings")]
    public GameObject targetMap;
    public SO_EnvironmentPalette palette;

    [Header("Grid Settings")]
    public int gridX = 10;
    public int gridZ = 10;

    private int selectedPrefabIndex = 0;
    private bool isPlacingMode = false;

    // 💡 썸네일 스크롤을 위한 변수 추가
    private Vector2 scrollPos;

    [MenuItem("Tools/Prop Placer Tool")]
    public static void ShowWindow()
    {
        // 창을 띄울 때 기본 사이즈를 조금 넓게 잡아줍니다.
        PropPlacerTool window = GetWindow<PropPlacerTool>("Prop Placer");
        window.minSize = new Vector2(700, 400);
    }

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnGUI()
    {
        EditorGUILayout.Space();

        // =========================================================
        // 💡 좌/우 분할 레이아웃 시작
        GUILayout.BeginHorizontal();

        // ---------------------------------------------------------
        // [왼쪽 창] 세팅 및 조작 버튼 (너비 고정)
        // ---------------------------------------------------------
        GUILayout.BeginVertical(GUILayout.Width(320));

        GUILayout.Label("격자 기반 환경 배치 툴", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetMap = (GameObject)EditorGUILayout.ObjectField("대상 맵 (부모)", targetMap, typeof(GameObject), true);
        palette = (SO_EnvironmentPalette)EditorGUILayout.ObjectField("환경 팔레트 (SO)", palette, typeof(SO_EnvironmentPalette), false);

        EditorGUILayout.Space();
        gridX = EditorGUILayout.IntSlider("격자 X (가로)", gridX, 1, 50);
        gridZ = EditorGUILayout.IntSlider("격자 Z (세로)", gridZ, 1, 50);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // 배치 모드 토글 버튼
        GUI.backgroundColor = isPlacingMode ? Color.green : Color.white;
        if (GUILayout.Button(isPlacingMode ? "배치 모드 활성화됨 (클릭하여 끄기)" : "배치 모드 켜기 (씬에서 클릭)", GUILayout.Height(50)))
        {
            isPlacingMode = !isPlacingMode;
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("배치 모드를 켜고 씬(Scene) 화면에서 바닥을 '마우스 좌클릭'하면 오브젝트가 배치됩니다.\n취소: 'Ctrl+Z'", MessageType.Info);

        GUILayout.FlexibleSpace(); // 버튼을 위쪽으로 밀어주고 아래 여백을 채움

        if (GUILayout.Button("현재 맵을 프리팹으로 저장", GUILayout.Height(35))) SaveMapToPrefab();

        GUILayout.EndVertical();
        // ---------------------------------------------------------


        // ---------------------------------------------------------
        // [오른쪽 창] 프리팹 썸네일 갤러리 (유동적 너비)
        // ---------------------------------------------------------
        GUILayout.BeginVertical("box");

        if (palette != null && palette.prefabs.Count > 0)
        {
            GUILayout.Label("오브젝트 선택 갤러리", EditorStyles.boldLabel);
            DrawPaletteThumbnails();
        }
        else
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("환경 팔레트(SO)를 등록하면\n여기에 오브젝트가 표시됩니다.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
        }

        GUILayout.EndVertical();
        // ---------------------------------------------------------

        GUILayout.EndHorizontal();
        // 💡 좌/우 분할 레이아웃 종료
        // =========================================================
    }

    private void DrawPaletteThumbnails()
    {
        List<GUIContent> icons = new List<GUIContent>();
        for (int i = 0; i < palette.prefabs.Count; i++)
        {
            GameObject prefab = palette.prefabs[i];
            if (prefab != null)
            {
                // 프리팹의 미리보기 이미지를 가져옵니다. (로딩 안됐을 땐 텍스트 띄움)
                Texture2D preview = AssetPreview.GetAssetPreview(prefab);
                if (preview != null)
                    icons.Add(new GUIContent(preview, prefab.name)); // 마우스 올리면 이름 나옴
                else
                    icons.Add(new GUIContent(prefab.name));
            }
        }

        // 스크롤 뷰 시작
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        // 창 크기에 맞춰서 썸네일 열(Column) 개수를 자동 계산합니다.
        // (전체 너비 - 왼쪽 창 너비 320 - 여백) / 썸네일 최소 크기 80
        float rightPaneWidth = position.width - 340;
        int columns = Mathf.Max(1, Mathf.FloorToInt(rightPaneWidth / 80f));

        // 아이콘들을 넓고 큼직하게 그려줍니다.
        selectedPrefabIndex = GUILayout.SelectionGrid(
            selectedPrefabIndex,
            icons.ToArray(),
            columns,
            GUILayout.Width(rightPaneWidth)
        );

        GUILayout.EndScrollView();
    }

    // 씬 화면(Scene View)에 직접 그려지고 클릭을 감지하는 부분
    private void OnSceneGUI(SceneView sceneView)
    {
        if (targetMap == null || !isPlacingMode) return;

        Renderer mapRenderer = targetMap.GetComponent<Renderer>();
        if (mapRenderer == null) return;

        Bounds bounds = mapRenderer.bounds;
        Event e = Event.current;

        DrawGridInScene(bounds);

        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, bounds.center);

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                if (hitPoint.x >= bounds.min.x && hitPoint.x <= bounds.max.x &&
                    hitPoint.z >= bounds.min.z && hitPoint.z <= bounds.max.z)
                {
                    PlaceObjectAt(hitPoint, bounds);
                    e.Use();
                }
            }
        }
    }

    private void DrawGridInScene(Bounds bounds)
    {
        Handles.color = new Color(0, 1, 0, 0.3f);

        float stepX = bounds.size.x / gridX;
        float stepZ = bounds.size.z / gridZ;

        for (int x = 0; x <= gridX; x++)
        {
            Vector3 start = new Vector3(bounds.min.x + (x * stepX), bounds.max.y, bounds.min.z);
            Vector3 end = new Vector3(bounds.min.x + (x * stepX), bounds.max.y, bounds.max.z);
            Handles.DrawLine(start, end);
        }

        for (int z = 0; z <= gridZ; z++)
        {
            Vector3 start = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z + (z * stepZ));
            Vector3 end = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z + (z * stepZ));
            Handles.DrawLine(start, end);
        }
    }

    private void PlaceObjectAt(Vector3 hitPoint, Bounds bounds)
    {
        if (palette == null || palette.prefabs.Count == 0 || selectedPrefabIndex >= palette.prefabs.Count) return;

        GameObject prefabToPlace = palette.prefabs[selectedPrefabIndex];
        if (prefabToPlace == null) return;

        float stepX = bounds.size.x / gridX;
        float stepZ = bounds.size.z / gridZ;

        float snappedX = bounds.min.x + (Mathf.Floor((hitPoint.x - bounds.min.x) / stepX) * stepX) + (stepX / 2f);
        float snappedZ = bounds.min.z + (Mathf.Floor((hitPoint.z - bounds.min.z) / stepZ) * stepZ) + (stepZ / 2f);

        Vector3 finalPosition = new Vector3(snappedX, bounds.max.y, snappedZ);

        GameObject newProp = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace);
        newProp.transform.position = finalPosition;
        newProp.transform.SetParent(targetMap.transform);

        Undo.RegisterCreatedObjectUndo(newProp, "Place Environment Prop");
    }

    private void SaveMapToPrefab()
    {
        if (targetMap == null) return;

        string path = EditorUtility.SaveFilePanelInProject("Save Map Prefab", targetMap.name, "prefab", "Save the decorated map as a prefab.");
        if (string.IsNullOrEmpty(path)) return;

        PrefabUtility.SaveAsPrefabAssetAndConnect(targetMap, path, InteractionMode.UserAction);
        Debug.Log($"<color=cyan>맵 프리팹 저장 완료: {path}</color>");
    }
}