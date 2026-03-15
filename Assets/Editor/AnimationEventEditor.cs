using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class AnimationEventEditor : EditorWindow
{
    private GameObject targetAsset; // 씬 객체가 아닌 프로젝트 에셋(Prefab/FBX)
    private AnimationClip targetClip;
    private List<AnimationClip> availableClips = new List<AnimationClip>();

    // 🎬 커스텀 3D 프리뷰 엔진 변수
    private PreviewRenderUtility previewUtility;
    private GameObject previewInstance;
    private Vector2 previewRotation = new Vector2(150, -20); // 마우스 드래그 회전값
    private float zoomMultiplier = 3f; // 카메라 줌(거리) 배율
    private Vector3 targetCenter;      // 모델의 중심점 캐싱
    private float targetMagnitude;     // 모델의 크기 캐싱

    private Vector2 mainScrollPos;
    private Vector2 clipListScrollPos;

    private string[] functionPresets = { "Start_DoAction", "Begin_DoAction", "End_DoAction", "Begin_JudgeAttack",
                                       "End_JudgeAttack","Play_Sound", "Play_CameraShake" ,"End_Damaged" };
    private int selectedPresetIndex = 0;

    private float targetTime = 0f;
    private bool isPlaying = false;
    private double lastUpdateTime;

    // 💡 이벤트 파라미터 입력용 변수
    private float eventFloat = 0f;
    private int eventInt = 0;
    private string eventString = "";
    private Object eventObject = null;

    // 💡 선택 상태 추적용 변수 (새로 추가!)
    private int selectedEventIndex = -1; // -1이면 '새 이벤트 추가', 0 이상이면 '기존 이벤트 수정'

    [MenuItem("Window/Project Magical Records/Asset Animation Editor")]
    public static void ShowWindow() => GetWindow<AnimationEventEditor>("Event Editor");

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        lastUpdateTime = EditorApplication.timeSinceStartup;

        // 창이 다시 열렸을 때 프리뷰 엔진 재가동
        if (targetAsset != null && previewUtility == null) InitPreview();
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        CleanupPreview(); // 메모리 릭 방지
    }

    private void OnEditorUpdate()
    {
        if (isPlaying && targetClip != null)
        {
            double deltaTime = EditorApplication.timeSinceStartup - lastUpdateTime;
            targetTime += (float)deltaTime;

            if (targetTime >= targetClip.length) targetTime = 0;
            Repaint(); // 씬 뷰가 아닌, 이 에디터 창만 새로고침!
        }
        lastUpdateTime = EditorApplication.timeSinceStartup;
    }

    private void OnGUI()
    {
        mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);
        GUILayout.Label("Asset-Based Event Editor (Independent)", EditorStyles.boldLabel);

        // 1. 프로젝트 에셋 선택 (allowSceneObjects = false 로 설정하여 씬 객체 차단)
        EditorGUI.BeginChangeCheck();
        targetAsset = (GameObject)EditorGUILayout.ObjectField("Target Asset (Prefab/FBX)", targetAsset, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            RefreshClipList();
            InitPreview();
        }

        if (targetAsset == null)
        {
            EditorGUILayout.HelpBox("프로젝트(Project) 창에 있는 원본 Prefab이나 FBX 모델을 넣어주세요.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        DrawClipList();

        if (targetClip != null)
        {
            EditorGUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            GUILayout.Label($"Editing: {targetClip.name}", EditorStyles.boldLabel);

            DrawPlaybackControls();

            // 🎬 독립된 3D 프리뷰 그리기
            DrawCustomPreview();

            DrawTimelineGraph();

            EditorGUI.BeginChangeCheck();
            targetTime = EditorGUILayout.Slider("Timeline Scrubber", targetTime, 0f, targetClip.length);
            if (EditorGUI.EndChangeCheck())
            {
                isPlaying = false;
                Repaint(); // 슬라이더 움직이면 프리뷰 즉시 갱신
            }

            EditorGUILayout.Space(10);
            DrawEventControls();
        }

        EditorGUILayout.EndScrollView();
    }

    // ==========================================
    // 💡 100% 독립된 3D 프리뷰 렌더링 로직
    // ==========================================
    private void InitPreview()
    {
        CleanupPreview();
        if (targetAsset == null) return;

        previewUtility = new PreviewRenderUtility();
        previewInstance = Instantiate(targetAsset);
        previewInstance.hideFlags = HideFlags.HideAndDontSave;

        foreach (var mb in previewInstance.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;

        previewUtility.AddSingleGO(previewInstance);

        // 모델 크기 측정 후 클래스 변수에 저장
        Bounds bounds = new Bounds(previewInstance.transform.position, Vector3.zero);
        foreach (Renderer r in previewInstance.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
        if (bounds.extents == Vector3.zero) bounds.extents = new Vector3(1, 1, 1);

        targetCenter = bounds.center;
        targetMagnitude = bounds.extents.magnitude;
        zoomMultiplier = 3f; // 새 모델을 넣을 때마다 기본 줌으로 초기화

        previewUtility.camera.clearFlags = CameraClearFlags.Color;
        previewUtility.camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        previewUtility.lights[0].transform.rotation = Quaternion.Euler(30, -30, 0);
        previewUtility.lights[1].transform.rotation = Quaternion.Euler(30, 150, 0);
    }

    private void DrawCustomPreview()
    {
        if (previewUtility == null || previewInstance == null) return;

        Rect previewRect = GUILayoutUtility.GetRect(200, 250, GUILayout.ExpandWidth(true));

        Event evt = Event.current;
        if (previewRect.Contains(evt.mousePosition))
        {
            // 드래그 (회전)
            if (evt.type == EventType.MouseDrag)
            {
                previewRotation.x -= evt.delta.x * 2f;
                previewRotation.y -= evt.delta.y * 2f;
                evt.Use();
            }
            // 💡 마우스 휠 (줌 인/아웃) 추가!
            else if (evt.type == EventType.ScrollWheel)
            {
                zoomMultiplier += evt.delta.y * 0.05f; // 스크롤 감도
                zoomMultiplier = Mathf.Clamp(zoomMultiplier, 0.5f, 10f); // 너무 가깝거나 멀어지지 않게 제한
                evt.Use();
            }
        }

        previewUtility.BeginPreview(previewRect, EditorStyles.helpBox);

        // 실시간 카메라 줌 적용
        previewUtility.camera.transform.position = targetCenter + new Vector3(0, targetMagnitude * 0.5f, -targetMagnitude * zoomMultiplier);
        previewUtility.camera.transform.LookAt(targetCenter);

        previewInstance.transform.rotation = Quaternion.Euler(previewRotation.y, previewRotation.x, 0);

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

    // ==========================================
    // 나머지 UI 및 로직 (기존과 동일)
    // ==========================================
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

            // 💡 선택된 이벤트 마커는 초록색으로 강조
            var oldColor = GUI.backgroundColor;
            if (i == selectedEventIndex) GUI.backgroundColor = Color.green;

            if (GUI.Button(new Rect(eventX - 5, rect.y + 5, 10, 15), "", EditorStyles.radioButton))
            {
                SelectEventForEdit(i, ev); // 마커 클릭 시 수정 모드 진입!
            }

            GUI.backgroundColor = oldColor; // 색상 원상복구
        }
    }

    private void DrawEventControls()
    {
        EditorGUILayout.BeginVertical("box");

        // 💡 상태에 따라 타이틀 변경
        if (selectedEventIndex == -1)
            GUILayout.Label("Add New Event", EditorStyles.boldLabel);
        else
            GUILayout.Label($"Edit Event (Selected)", EditorStyles.boldLabel);

        selectedPresetIndex = EditorGUILayout.Popup("Function", selectedPresetIndex, functionPresets);
        eventFloat = EditorGUILayout.FloatField("Float", eventFloat);
        eventInt = EditorGUILayout.IntField("Int", eventInt);
        eventString = EditorGUILayout.TextField("String", eventString);
        eventObject = EditorGUILayout.ObjectField("Object", eventObject, typeof(Object), false);

        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();

        // 💡 선택 상태에 따라 버튼 체인지
        if (selectedEventIndex == -1)
        {
            if (GUILayout.Button("Add Event", GUILayout.Height(30)))
            { AddEvent(); GUIUtility.ExitGUI(); }
        }
        else
        {
            if (GUILayout.Button("Apply Changes", GUILayout.Height(30)))
            { ApplyEventChanges(); GUIUtility.ExitGUI(); }

            if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(30)))
            { selectedEventIndex = -1; GUIUtility.ExitGUI(); }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // --- 기존 이벤트 리스트 박스 ---
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Event List", EditorStyles.boldLabel);
        if (targetClip == null) { EditorGUILayout.EndVertical(); return; }

        var events = AnimationUtility.GetAnimationEvents(targetClip);
        for (int i = 0; i < events.Length; i++)
        {
            var ev = events[i];

            var oldColor = GUI.backgroundColor;
            if (i == selectedEventIndex) GUI.backgroundColor = Color.green; // 리스트도 초록색 강조

            EditorGUILayout.BeginHorizontal("helpbox");
            GUI.backgroundColor = oldColor;

            // 💡 시간이나 파라미터 라벨을 누르면 해당 이벤트 수정 모드로 진입
            if (GUILayout.Button($"{ev.time:F2}s", GUILayout.Width(45))) SelectEventForEdit(i, ev);

            string paramSummary = ev.functionName;
            if (ev.floatParameter != 0) paramSummary += $" (F: {ev.floatParameter})";
            if (ev.intParameter != 0) paramSummary += $" (I: {ev.intParameter})";
            if (!string.IsNullOrEmpty(ev.stringParameter)) paramSummary += $" (S: {ev.stringParameter})";
            if (ev.objectReferenceParameter != null) paramSummary += $" (O: {ev.objectReferenceParameter.name})";

            if (GUILayout.Button(paramSummary, EditorStyles.miniLabel)) SelectEventForEdit(i, ev);

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (selectedEventIndex == i) selectedEventIndex = -1; // 삭제하면 수정 모드 해제
                RemoveEvent(ev);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void AddEvent()
    {
        if (targetClip == null) return;

        // 💡 새 이벤트 객체를 만들 때, 적어둔 파라미터 값들도 싹 다 집어넣습니다.
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

    // 💡 마법의 핵심: FBX 원본인지 검사하고 맞게 저장해 주는 만능 저장소
    private void SaveEventsToClip(AnimationEvent[] newEvents)
    {
        string assetPath = AssetDatabase.GetAssetPath(targetClip);
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        string clipName = targetClip.name;

        float clipLength = targetClip.length > 0 ? targetClip.length : 1f;

        if (importer != null)
        {
            // 🚨 핵심 수정: 이미 다른 클립에 저장해둔 정보가 날아가지 않도록, 
            // '순정 상태(default)'가 아니라 '현재 수정된 상태(clipAnimations)'를 먼저 가져옵니다!
            ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations;

            // 만약 FBX를 유니티에 넣은 후 한 번도 수정한 적이 없다면 순정 상태를 가져옵니다.
            if (clipAnimations == null || clipAnimations.Length == 0)
            {
                clipAnimations = importer.defaultClipAnimations;
            }

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
                importer.clipAnimations = clipAnimations; // 다른 클립 정보도 안전하게 같이 저장됨!

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

    // 💡 리스트나 마커를 클릭했을 때 에디터를 '수정 모드'로 세팅하는 함수
    private void SelectEventForEdit(int index, AnimationEvent ev)
    {
        selectedEventIndex = index;
        targetTime = ev.time;
        isPlaying = false;

        // 함수 이름 프리셋 인덱스 찾기
        selectedPresetIndex = System.Array.IndexOf(functionPresets, ev.functionName);
        if (selectedPresetIndex < 0) selectedPresetIndex = 0;

        // 파라미터 값들 불러오기
        eventFloat = ev.floatParameter;
        eventInt = ev.intParameter;
        eventString = ev.stringParameter;
        eventObject = ev.objectReferenceParameter;

        //UpdatePose();
        Repaint();
    }

    // 💡 'Apply Changes(적용)' 버튼을 눌렀을 때 덮어씌워 저장하는 함수
    private void ApplyEventChanges()
    {
        if (targetClip == null || selectedEventIndex < 0) return;

        var events = AnimationUtility.GetAnimationEvents(targetClip).ToList();
        if (selectedEventIndex < events.Count)
        {
            events[selectedEventIndex] = new AnimationEvent
            {
                functionName = functionPresets[selectedPresetIndex],
                time = targetTime, // 수정된 시간(위치) 반영
                floatParameter = eventFloat,
                intParameter = eventInt,
                stringParameter = eventString,
                objectReferenceParameter = eventObject
            };

            SaveEventsToClip(events.OrderBy(e => e.time).ToArray());
            selectedEventIndex = -1; // 저장 후 수정 모드 종료
        }
    }
}