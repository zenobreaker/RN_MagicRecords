using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class AnimationEventEditor : EditorWindow
{
    private GameObject targetAsset;
    private AnimationClip targetClip;
    private List<AnimationClip> availableClips = new List<AnimationClip>();

    // 🎬 커스텀 3D 프리뷰 엔진 변수
    private PreviewRenderUtility previewUtility;
    private GameObject previewInstance;
    private Vector2 previewRotation = new Vector2(150, -20);
    private float zoomMultiplier = 3f;
    private Vector3 targetCenter;
    private float targetMagnitude;

    private Vector2 mainScrollPos;
    private Vector2 clipListScrollPos;
    private Vector2 assetListScrollPos;

    private string[] functionPresets = { "Start_DoAction", "Begin_DoAction", "End_DoAction", "Begin_JudgeAttack",
                                       "End_JudgeAttack","Play_Sound", "Play_CameraShake" ,"End_Damaged" };
    private int selectedPresetIndex = 0;

    private float targetTime = 0f;
    private bool isPlaying = false;
    private double lastUpdateTime;

    private float eventFloat = 0f;
    private int eventInt = 0;
    private string eventString = "";
    private Object eventObject = null;

    private int selectedEventIndex = -1;

    // 💡 에셋 검색 및 필터용 변수
    public enum AssetFilterType { All, PrefabOnly, ModelOnly }
    private AssetFilterType currentFilter = AssetFilterType.All;
    private string searchQuery = "";
    private List<GameObject> cachedAssets = new List<GameObject>();

    [MenuItem("Window/Project Magical Records/Asset Animation Editor")]
    public static void ShowWindow() => GetWindow<AnimationEventEditor>("Event Editor");

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        lastUpdateTime = EditorApplication.timeSinceStartup;

        if (targetAsset != null && previewUtility == null) InitPreview();

        RefreshAssetList();
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        CleanupPreview();
    }

    private void OnEditorUpdate()
    {
        if (isPlaying && targetClip != null)
        {
            double deltaTime = EditorApplication.timeSinceStartup - lastUpdateTime;
            targetTime += (float)deltaTime;

            if (targetTime >= targetClip.length) targetTime = 0;
            Repaint();
        }
        lastUpdateTime = EditorApplication.timeSinceStartup;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // ==========================================
        // 💡 [왼쪽 패널]: 에셋, 클립, 프리뷰, 리스트 영역
        // ==========================================
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);

        GUILayout.Label("Asset-Based Event Editor (Independent)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        targetAsset = (GameObject)EditorGUILayout.ObjectField("Target Asset", targetAsset, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            RefreshClipList();
            InitPreview();
        }

        if (targetAsset != null)
        {
            DrawClipList();

            if (targetClip != null)
            {
                EditorGUILayout.Space(10);
                GUILayout.Label($"Editing: {targetClip.name}", EditorStyles.boldLabel);

                DrawPlaybackControls();
                DrawCustomPreview();
                DrawTimelineGraph();

                EditorGUI.BeginChangeCheck();
                targetTime = EditorGUILayout.Slider("Timeline Scrubber", targetTime, 0f, targetClip.length);
                if (EditorGUI.EndChangeCheck())
                {
                    isPlaying = false;
                    Repaint();
                }

                EditorGUILayout.Space(10);
                DrawEventListPanel();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Project 창에 있는 원본 Prefab이나 FBX 모델을 넣거나, 아래 목록에서 선택하세요.", MessageType.Info);
        }

        DrawAssetSearchPanel();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // ==========================================
        // 💡 [오른쪽 패널]: 이벤트 입력 폼 (고정)
        // ==========================================
        EditorGUILayout.BeginVertical("window", GUILayout.Width(300), GUILayout.ExpandHeight(true));
        DrawEventInputsPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    // ==========================================
    // 💡 에셋 검색 패널 (진짜 3D 모델 프리뷰 적용!)
    // ==========================================
    private void DrawAssetSearchPanel()
    {
        EditorGUILayout.Space(20);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
        GUILayout.Label("📂 Target Asset Browser", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        currentFilter = (AssetFilterType)EditorGUILayout.EnumPopup(currentFilter, GUILayout.Width(100));

        searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(60)))
        {
            RefreshAssetList();
        }
        if (EditorGUI.EndChangeCheck())
        {
            RefreshAssetList();
        }
        EditorGUILayout.EndHorizontal();

        assetListScrollPos = EditorGUILayout.BeginScrollView(assetListScrollPos, "helpbox", GUILayout.Height(250));

        if (cachedAssets.Count == 0)
        {
            GUILayout.Label("조건에 맞는 에셋이 없습니다.", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            // 💡 버튼 텍스트와 썸네일을 위한 스타일 세팅
            GUIStyle itemStyle = new GUIStyle(GUI.skin.button);
            itemStyle.alignment = TextAnchor.MiddleLeft;
            itemStyle.imagePosition = ImagePosition.ImageLeft;

            bool isAnyPreviewLoading = false;

            foreach (var asset in cachedAssets)
            {
                // 💡 [핵심] 미니 아이콘이 아니라 진짜 3D 모델 프리뷰를 가져옵니다.
                Texture2D previewIcon = AssetPreview.GetAssetPreview(asset);

                // 유니티가 백그라운드에서 프리뷰를 렌더링 중일 때는 임시로 미니 아이콘을 보여줍니다.
                if (previewIcon == null)
                {
                    previewIcon = AssetPreview.GetMiniThumbnail(asset);
                    isAnyPreviewLoading = true;
                }

                GUIContent content = new GUIContent("  " + asset.name, previewIcon);

                // 높이를 40픽셀로 큼직하게 줘서 모델의 형태가 잘 보이게 합니다.
                if (GUILayout.Button(content, itemStyle, GUILayout.Height(40)))
                {
                    targetAsset = asset;
                    RefreshClipList();
                    InitPreview();
                    GUI.FocusControl(null);
                }
            }

            // 프리뷰 렌더링이 아직 안 끝난 에셋이 있다면, 완료될 때까지 에디터 창을 계속 새로고침합니다.
            if (isAnyPreviewLoading)
            {
                Repaint();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void RefreshAssetList()
    {
        cachedAssets.Clear();
        string[] guids = AssetDatabase.FindAssets("t:GameObject");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            bool isPrefab = path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase);
            bool isModel = path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) || path.EndsWith(".obj", System.StringComparison.OrdinalIgnoreCase);

            if (!isPrefab && !isModel) continue;
            if (currentFilter == AssetFilterType.PrefabOnly && !isPrefab) continue;
            if (currentFilter == AssetFilterType.ModelOnly && !isModel) continue;

            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (obj != null)
            {
                if (obj.GetComponentInChildren<Animator>() != null)
                {
                    if (string.IsNullOrEmpty(searchQuery) || obj.name.ToLower().Contains(searchQuery.ToLower()))
                    {
                        cachedAssets.Add(obj);
                    }
                }
            }
        }
    }

    private void DrawEventListPanel()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Event List", EditorStyles.boldLabel);
        if (targetClip == null) { EditorGUILayout.EndVertical(); return; }

        var events = AnimationUtility.GetAnimationEvents(targetClip);
        for (int i = 0; i < events.Length; i++)
        {
            var ev = events[i];

            var oldColor = GUI.backgroundColor;
            if (i == selectedEventIndex) GUI.backgroundColor = Color.green;

            EditorGUILayout.BeginHorizontal("helpbox");
            GUI.backgroundColor = oldColor;

            if (GUILayout.Button($"{ev.time:F2}s", GUILayout.Width(45))) SelectEventForEdit(i, ev);

            string paramSummary = ev.functionName;
            if (ev.floatParameter != 0) paramSummary += $" (F: {ev.floatParameter})";
            if (ev.intParameter != 0) paramSummary += $" (I: {ev.intParameter})";
            if (!string.IsNullOrEmpty(ev.stringParameter)) paramSummary += $" (S: {ev.stringParameter})";
            if (ev.objectReferenceParameter != null) paramSummary += $" (O: {ev.objectReferenceParameter.name})";

            if (GUILayout.Button(paramSummary, EditorStyles.miniLabel)) SelectEventForEdit(i, ev);

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (selectedEventIndex == i) selectedEventIndex = -1;
                RemoveEvent(ev);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawEventInputsPanel()
    {
        EditorGUILayout.BeginVertical();

        if (targetClip == null)
        {
            GUILayout.Label("에셋과 클립을 먼저 선택해주세요.", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        if (selectedEventIndex == -1)
            GUILayout.Label("✨ Add New Event", EditorStyles.boldLabel);
        else
            GUILayout.Label($"✏️ Edit Event (Selected)", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        selectedPresetIndex = EditorGUILayout.Popup("Function", selectedPresetIndex, functionPresets);
        eventFloat = EditorGUILayout.FloatField("Float", eventFloat);
        eventInt = EditorGUILayout.IntField("Int", eventInt);
        eventString = EditorGUILayout.TextField("String", eventString);
        eventObject = EditorGUILayout.ObjectField("Object", eventObject, typeof(Object), false);

        GUILayout.Space(15);

        if (selectedEventIndex == -1)
        {
            if (GUILayout.Button("Add Event", GUILayout.Height(35)))
            { AddEvent(); GUIUtility.ExitGUI(); }
        }
        else
        {
            if (GUILayout.Button("Apply Changes", GUILayout.Height(35)))
            { ApplyEventChanges(); GUIUtility.ExitGUI(); }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear Params", GUILayout.Height(25)))
            {
                eventFloat = 0f;
                eventInt = 0;
                eventString = "";
                eventObject = null;
                GUI.FocusControl(null);
                ApplyEventChanges();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("Cancel", GUILayout.Height(25)))
            { selectedEventIndex = -1; GUIUtility.ExitGUI(); }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void InitPreview()
    {
        CleanupPreview();
        if (targetAsset == null) return;

        previewUtility = new PreviewRenderUtility();
        previewInstance = Instantiate(targetAsset);
        previewInstance.hideFlags = HideFlags.HideAndDontSave;

        foreach (var mb in previewInstance.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;

        previewUtility.AddSingleGO(previewInstance);

        Bounds bounds = new Bounds(previewInstance.transform.position, Vector3.zero);
        foreach (Renderer r in previewInstance.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
        if (bounds.extents == Vector3.zero) bounds.extents = new Vector3(1, 1, 1);

        targetCenter = bounds.center;
        targetMagnitude = bounds.extents.magnitude;
        zoomMultiplier = 3f;

        previewUtility.camera.clearFlags = CameraClearFlags.Color;
        previewUtility.camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        previewUtility.lights[0].transform.rotation = Quaternion.Euler(30, -30, 0);
        previewUtility.lights[1].transform.rotation = Quaternion.Euler(30, 150, 0);

        // 💡 꼼수: 바닥 역할을 할 Plane을 생성해서 프리뷰 씬에 던져 넣습니다.
        GameObject gridPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        gridPlane.hideFlags = HideFlags.HideAndDontSave;
        gridPlane.transform.position = Vector3.zero; // 발밑에 위치

        // 투명도 있는 회색 재질 입히기 (선택 사항)
        Material gridMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        gridMat.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        gridPlane.GetComponent<MeshRenderer>().sharedMaterial = gridMat;

        previewUtility.AddSingleGO(gridPlane); // 모델과 함께 렌더링!
    }

    private void DrawCustomPreview()
    {
        if (previewUtility == null || previewInstance == null) return;

        Rect previewRect = GUILayoutUtility.GetRect(200, 250, GUILayout.ExpandWidth(true));

        Event evt = Event.current;
        if (previewRect.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.MouseDrag)
            {
                previewRotation.x -= evt.delta.x * 2f;
                previewRotation.y -= evt.delta.y * 2f;
                evt.Use();
            }
            else if (evt.type == EventType.ScrollWheel)
            {
                zoomMultiplier += evt.delta.y * 0.05f;
                zoomMultiplier = Mathf.Clamp(zoomMultiplier, 0.5f, 10f);
                evt.Use();
            }
        }

        previewUtility.BeginPreview(previewRect, EditorStyles.helpBox);

        // ==========================================================
        // 💡 [수정된 부분] 모델 대신 카메라를 회전시킵니다 (Orbit Camera)
        // ==========================================================
        // 1. 카메라가 바라볼 타겟 지점 (모델의 가슴 쯤)
        Vector3 lookAtPoint = targetCenter + new Vector3(0, targetMagnitude * 0.2f, 0);

        // 2. 마우스 드래그로 계산된 회전값
        Quaternion camRotation = Quaternion.Euler(previewRotation.y, previewRotation.x, 0);

        // 3. 카메라가 타겟으로부터 떨어져 있을 거리 (줌 반영)
        Vector3 distanceOffset = new Vector3(0, 0, -targetMagnitude * zoomMultiplier);

        // 4. 최종 카메라 위치 = 타겟 지점 + (회전값 * 거리)
        previewUtility.camera.transform.position = lookAtPoint + (camRotation * distanceOffset);

        // 5. 카메라는 항상 타겟을 바라봄
        previewUtility.camera.transform.LookAt(lookAtPoint);

        // 🚨 기존에 있던 previewInstance.transform.rotation = ... 줄은 완전히 삭제합니다!
        // 모델은 항상 (0,0,0)을 보게 두고, 카메라가 주변을 빙글빙글 돕니다.
        // ==========================================================

        if (targetClip != null)
            targetClip.SampleAnimation(previewInstance, targetTime);

        previewUtility.camera.Render();
        Texture resultTexture = previewUtility.EndPreview();

        GUI.DrawTexture(previewRect, resultTexture, ScaleMode.StretchToFill, false);
        GUILayout.Label("💡 드래그: 회전 / 마우스 휠: 줌 인·아웃", EditorStyles.centeredGreyMiniLabel);
    }

    private void CleanupPreview()
    {
        if (previewInstance != null) DestroyImmediate(previewInstance);
        if (previewUtility != null)
        {
            previewUtility.Cleanup();
            previewUtility = null;
        }
    }

    private void DrawClipList()
    {
        GUILayout.Label("Available Animations", EditorStyles.miniBoldLabel);
        clipListScrollPos = EditorGUILayout.BeginScrollView(clipListScrollPos, "box", GUILayout.Height(120));

        if (availableClips.Count == 0) GUILayout.Label("애니메이션을 찾을 수 없습니다.");

        foreach (var clip in availableClips)
        {
            if (GUILayout.Toggle(targetClip == clip, clip.name, "Button"))
            {
                if (targetClip != clip) { targetClip = clip; targetTime = 0; }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void RefreshClipList()
    {
        availableClips.Clear();
        targetClip = null;
        if (targetAsset == null) return;

        var animator = targetAsset.GetComponentInChildren<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
            availableClips.AddRange(animator.runtimeAnimatorController.animationClips);

        if (availableClips.Count == 0)
        {
            string path = AssetDatabase.GetAssetPath(targetAsset);
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                        availableClips.Add(clip);
                }
            }
        }
        availableClips = availableClips.Distinct().ToList();
        if (availableClips.Count > 0) targetClip = availableClips[0];
    }

    private void DrawPlaybackControls()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(isPlaying ? "⏸ Pause" : "▶ Play", GUILayout.Height(25)))
        {
            isPlaying = !isPlaying;
            if (isPlaying) lastUpdateTime = EditorApplication.timeSinceStartup;
        }
        if (GUILayout.Button("Stop", GUILayout.Height(25)))
        {
            isPlaying = false;
            targetTime = 0;
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTimelineGraph()
    {
        Rect rect = GUILayoutUtility.GetRect(10, 25, GUILayout.ExpandWidth(true));
        GUI.Box(rect, "", EditorStyles.helpBox);
        if (targetClip == null || targetClip.length <= 0) return;

        float currentX = rect.x + (targetTime / targetClip.length) * rect.width;
        EditorGUI.DrawRect(new Rect(currentX - 1, rect.y, 2, rect.height), Color.red);

        var events = AnimationUtility.GetAnimationEvents(targetClip);
        for (int i = 0; i < events.Length; i++)
        {
            var ev = events[i];
            float eventX = rect.x + (ev.time / targetClip.length) * rect.width;

            var oldColor = GUI.backgroundColor;
            if (i == selectedEventIndex) GUI.backgroundColor = Color.green;

            if (GUI.Button(new Rect(eventX - 5, rect.y + 5, 10, 15), "", EditorStyles.radioButton))
            {
                SelectEventForEdit(i, ev);
            }
            GUI.backgroundColor = oldColor;
        }
    }

    private void AddEvent()
    {
        if (targetClip == null) return;

        var newEvent = new AnimationEvent
        {
            functionName = functionPresets[selectedPresetIndex],
            time = targetTime,
            floatParameter = eventFloat,
            intParameter = eventInt,
            stringParameter = eventString,
            objectReferenceParameter = eventObject
        };

        var events = AnimationUtility.GetAnimationEvents(targetClip).ToList();
        events.Add(newEvent);

        SaveEventsToClip(events.OrderBy(e => e.time).ToArray());
    }

    private void RemoveEvent(AnimationEvent ev)
    {
        if (targetClip == null) return;

        var events = AnimationUtility.GetAnimationEvents(targetClip).ToList();
        events.RemoveAll(e => e.time == ev.time && e.functionName == ev.functionName);

        SaveEventsToClip(events.ToArray());
    }

    private void SaveEventsToClip(AnimationEvent[] newEvents)
    {
        string assetPath = AssetDatabase.GetAssetPath(targetClip);
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        string clipName = targetClip.name;
        float clipLength = targetClip.length > 0 ? targetClip.length : 1f;

        if (importer != null)
        {
            ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations;
            if (clipAnimations == null || clipAnimations.Length == 0)
                clipAnimations = importer.defaultClipAnimations;

            bool isModified = false;

            for (int i = 0; i < clipAnimations.Length; i++)
            {
                if (clipAnimations[i].name == clipName)
                {
                    AnimationEvent[] normalizedEvents = new AnimationEvent[newEvents.Length];
                    for (int j = 0; j < newEvents.Length; j++)
                    {
                        normalizedEvents[j] = new AnimationEvent
                        {
                            functionName = newEvents[j].functionName,
                            floatParameter = newEvents[j].floatParameter,
                            intParameter = newEvents[j].intParameter,
                            stringParameter = newEvents[j].stringParameter,
                            objectReferenceParameter = newEvents[j].objectReferenceParameter,
                            messageOptions = newEvents[j].messageOptions,
                            time = newEvents[j].time / clipLength
                        };
                    }

                    clipAnimations[i].events = normalizedEvents;
                    isModified = true;
                    break;
                }
            }

            if (isModified)
            {
                importer.clipAnimations = clipAnimations;

                EditorApplication.delayCall += () =>
                {
                    importer.SaveAndReimport();
                    Debug.Log($"[성공] FBX 원본({clipName}) 보존 및 저장 완료!");

                    RefreshClipList();
                    targetClip = availableClips.FirstOrDefault(c => c.name == clipName);
                    Repaint();
                };
            }
        }
        else
        {
            AnimationUtility.SetAnimationEvents(targetClip, newEvents);
            EditorUtility.SetDirty(targetClip);
            AssetDatabase.SaveAssets();
        }
    }

    private void SelectEventForEdit(int index, AnimationEvent ev)
    {
        selectedEventIndex = index;
        targetTime = ev.time;
        isPlaying = false;

        selectedPresetIndex = System.Array.IndexOf(functionPresets, ev.functionName);
        if (selectedPresetIndex < 0) selectedPresetIndex = 0;

        eventFloat = ev.floatParameter;
        eventInt = ev.intParameter;
        eventString = ev.stringParameter;
        eventObject = ev.objectReferenceParameter;

        Repaint();
    }

    private void ApplyEventChanges()
    {
        if (targetClip == null || selectedEventIndex < 0) return;

        var events = AnimationUtility.GetAnimationEvents(targetClip).ToList();
        if (selectedEventIndex < events.Count)
        {
            events[selectedEventIndex] = new AnimationEvent
            {
                functionName = functionPresets[selectedPresetIndex],
                time = targetTime,
                floatParameter = eventFloat,
                intParameter = eventInt,
                stringParameter = eventString,
                objectReferenceParameter = eventObject
            };

            SaveEventsToClip(events.OrderBy(e => e.time).ToArray());
            selectedEventIndex = -1;
        }
    }
}