using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

public class ActiveSkillEditorWindow : EditorWindow
{
    private const string SkillFolder = "Assets/10.ScriptableObjects";

    private List<ScriptableObject> activeSkills = new();
    private ScriptableObject selectedSkill;
    private Vector2 leftScrollPos;
    private Vector2 centerScrollPos;

    // 씬 뷰/노드 에디터 관련 변수
    private Vector2 nodePanOffset = Vector2.zero;
    private bool isDraggingCanvas = false;

    private int selectedPhaseIndex = -1;
    private int selectedModuleIndex = -1;

    [MenuItem("Tools/Active Skill Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<ActiveSkillEditorWindow>("Active Skill Editor");
        window.minSize = new Vector2(1200f, 700f);
        window.Show();
    }

    private void OnEnable() => RefreshSkillList();

    private void RefreshSkillList()
    {
        activeSkills.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { SkillFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var skill = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (skill != null)
            {
                SerializedObject so = new SerializedObject(skill);
                if (so.FindProperty("phaseList") != null) activeSkills.Add(skill);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawLeftListPanel();
        DrawCenterInfoPanel();
        DrawRightNodeCanvasPanel();
        EditorGUILayout.EndHorizontal();
    }

    // ==========================================
    // 1. 왼쪽 패널 (스킬 리스트)
    // ==========================================
    private void DrawLeftListPanel()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(280f), GUILayout.ExpandHeight(true));

        EditorGUILayout.LabelField("⚡ Active Skills", EditorStyles.boldLabel);
        if (GUILayout.Button("새로고침", EditorStyles.miniButton)) RefreshSkillList();
        EditorGUILayout.Space(5f);

        if (GUILayout.Button("+ 새 Active Skill 만들기", GUILayout.Height(30f))) CreateNewActiveSkill();

        EditorGUILayout.Space(10f);
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

        GUIStyle listItemStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            imagePosition = ImagePosition.ImageLeft,
            fixedHeight = 40f,
            fontSize = 11
        };

        foreach (var skill in activeSkills)
        {
            if (skill == null) continue;
            SerializedObject so = new SerializedObject(skill);

            SerializedProperty nameProp = so.FindProperty("skillName") ?? so.FindProperty("SkillName");
            string sName = nameProp != null ? nameProp.stringValue : "";
            if (string.IsNullOrEmpty(sName)) sName = skill.name;

            Texture2D icon = null;
            SerializedProperty imageProp = so.FindProperty("skillImage") ?? so.FindProperty("icon");
            if (imageProp != null && imageProp.objectReferenceValue != null)
            {
                if (imageProp.objectReferenceValue is Sprite spr) icon = spr.texture;
                else if (imageProp.objectReferenceValue is Texture2D tex) icon = tex;
            }

            int sId = so.FindProperty("skillID")?.intValue ?? so.FindProperty("id")?.intValue ?? 0;
            GUIContent content = new GUIContent($" [{sId}] {sName}", icon);

            Color oldBg = GUI.backgroundColor;
            if (selectedSkill == skill) GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);

            if (GUILayout.Button(content, listItemStyle))
            {
                selectedSkill = skill;
                selectedPhaseIndex = -1;
                selectedModuleIndex = -1;
                GUI.FocusControl(null);
            }
            GUI.backgroundColor = oldBg;
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ==========================================
    // 2. 가운데 패널 (페이즈 편집 및 커스텀 모듈 리스트)
    // ==========================================
    private void DrawCenterInfoPanel()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(370f), GUILayout.ExpandHeight(true));

        if (selectedSkill == null)
        {
            EditorGUILayout.HelpBox("왼쪽 목록에서 스킬을 선택해주세요.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        centerScrollPos = EditorGUILayout.BeginScrollView(centerScrollPos);
        SerializedObject so = new SerializedObject(selectedSkill);
        so.Update();

        SerializedProperty phaseListProp = so.FindProperty("phaseList");

        if (selectedPhaseIndex == -1)
        {
            EditorGUILayout.LabelField("📝 Base Properties", EditorStyles.boldLabel);
            DrawPropertyIfExists(so, "id");
            DrawPropertyIfExists(so, "skillName");
            DrawPropertyIfExists(so, "skillImage");
            DrawPropertyIfExists(so, "isConcurrentSkill");
            DrawPropertyIfExists(so, "actionData");
            DrawPropertyIfExists(so, "damageData");
            EditorGUILayout.Space(10f);
            DrawPropertyIfExists(so, "levelDatas");
        }
        else if (phaseListProp != null && selectedPhaseIndex < phaseListProp.arraySize)
        {
            SerializedProperty currentPhaseProp = phaseListProp.GetArrayElementAtIndex(selectedPhaseIndex);

            GUI.color = new Color(0.85f, 0.95f, 1f);
            EditorGUILayout.BeginVertical("helpbox");
            GUI.color = Color.white;

            if (selectedModuleIndex == -1)
            {
                EditorGUILayout.LabelField($"🧩 Editing Phase [{selectedPhaseIndex}]", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(currentPhaseProp.FindPropertyRelative("isInstant"));
                EditorGUILayout.Space(10f);

                // 💡 [레이아웃 붕괴 완벽 해결] 커스텀 리스트 + 스왑(이동) 로직 렌더링
                DrawCustomModuleList(so, currentPhaseProp.FindPropertyRelative("modules"));
            }
            else
            {
                // 특정 모듈 상세 속성 수정 (Detail View)
                SerializedProperty modulesProp = currentPhaseProp.FindPropertyRelative("modules");
                if (selectedModuleIndex < modulesProp.arraySize)
                {
                    SerializedProperty modProp = modulesProp.GetArrayElementAtIndex(selectedModuleIndex);

                    EditorGUILayout.LabelField($"⚙️ Module Detail", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"[{GetTriggerGroupName(modProp)}] {GetModuleSummary(modProp)}", EditorStyles.helpBox);
                    EditorGUILayout.Space(5f);

                    EditorGUILayout.PropertyField(modProp, true);

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("◀ 페이즈 목록으로 돌아가기", GUILayout.Height(30f)))
                    {
                        selectedModuleIndex = -1;
                        GUI.FocusControl(null);
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        so.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ==========================================
    // 💡 [핵심] 에러가 없는 100% 커스텀 수동 레이아웃 리스트
    // ==========================================
    // ==========================================
    // 💡 [핵심] 에러가 없는 100% 커스텀 수동 레이아웃 리스트
    // ==========================================
    private void DrawCustomModuleList(SerializedObject so, SerializedProperty modulesProp)
    {
        EditorGUILayout.LabelField("Phase Modules", EditorStyles.boldLabel);

        if (modulesProp == null || !modulesProp.isArray) return;

        string currentGroup = "";

        for (int m = 0; m < modulesProp.arraySize; m++)
        {
            SerializedProperty modProp = modulesProp.GetArrayElementAtIndex(m);
            string triggerGroup = GetTriggerGroupName(modProp);

            // 1. 트리거 타임 별로 그룹 헤더 그리기
            if (m == 0 || triggerGroup != currentGroup)
            {
                currentGroup = triggerGroup;
                EditorGUILayout.Space(5f);

                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                EditorGUILayout.BeginHorizontal("toolbar");
                EditorGUILayout.LabelField($"▼ [{currentGroup}]", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                GUI.color = Color.white;
            }

            EditorGUILayout.BeginHorizontal("box");

            // 2. 모듈 이름 (여백 클릭 시 Detail View 진입)
            if (GUILayout.Button($"[{m}] {GetModuleSummary(modProp)}", EditorStyles.label, GUILayout.ExpandWidth(true)))
            {
                selectedModuleIndex = m;
                GUI.FocusControl(null);
            }

            // 3. 순서 조정 버튼 (▲ 위로 / ▼ 아래로)
            if (GUILayout.Button("▲", GUILayout.Width(25)) && m > 0)
            {
                modulesProp.MoveArrayElement(m, m - 1);
                so.ApplyModifiedProperties();
                EditorGUILayout.EndHorizontal(); // 💡 레이아웃 깨짐 방지!
                break;
            }
            if (GUILayout.Button("▼", GUILayout.Width(25)) && m < modulesProp.arraySize - 1)
            {
                modulesProp.MoveArrayElement(m, m + 1);
                so.ApplyModifiedProperties();
                EditorGUILayout.EndHorizontal(); // 💡 레이아웃 깨짐 방지!
                break;
            }

            // 4. Edit 버튼
            if (GUILayout.Button("Edit", GUILayout.Width(45)))
            {
                selectedModuleIndex = m;
                GUI.FocusControl(null);
            }

            // 5. 삭제 버튼
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                modulesProp.DeleteArrayElementAtIndex(m);
                so.ApplyModifiedProperties();
                EditorGUILayout.EndHorizontal(); // 💡 레이아웃 깨짐 방지!
                break;
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10f);

        // 카테고리별 모듈 추가 버튼
        if (GUILayout.Button("+ 모듈 추가 (카테고리별)", GUILayout.Height(30f)))
        {
            ShowCategorizedModuleMenu(modulesProp);
        }
    }

    // 💡 트리거 타임 속성을 찾아서 문자열로 반환하는 헬퍼 함수
    private string GetTriggerGroupName(SerializedProperty modProp)
    {
        if (modProp == null) return "Unknown";

        var tProp = modProp.FindPropertyRelative("triggerTime") ??
                    modProp.FindPropertyRelative("TriggerTime") ??
                    modProp.FindPropertyRelative("triggerType");

        if (tProp != null && tProp.propertyType == SerializedPropertyType.Enum)
        {
            return tProp.enumDisplayNames[tProp.enumValueIndex];
        }
        return "On Execute"; // 속성을 못 찾았을 때 기본값
    }

    private string GetModuleSummary(SerializedProperty modProp)
    {
        if (modProp == null) return "Null";
        string typeName = modProp.managedReferenceFullTypename;
        if (string.IsNullOrEmpty(typeName)) return "Empty Module";

        string name = typeName.Split(' ').Last().Replace("Module_", "");
        List<string> details = new List<string>();

        var timeProp = modProp.FindPropertyRelative("castingTime") ?? modProp.FindPropertyRelative("duration") ?? modProp.FindPropertyRelative("time");
        if (timeProp != null && timeProp.propertyType == SerializedPropertyType.Float) details.Add($"Time: {timeProp.floatValue}s");

        var overProp = modProp.FindPropertyRelative("overrideCasting");
        if (overProp != null && overProp.propertyType == SerializedPropertyType.Boolean) details.Add($"Override: {overProp.boolValue}");

        if (details.Count > 0) return $"{name} ({string.Join(", ", details)})";
        return name;
    }

    private void ShowCategorizedModuleMenu(SerializedProperty listProp)
    {
        GenericMenu menu = new GenericMenu();
        Type phaseType = typeof(PhaseSkill);
        FieldInfo modulesField = phaseType.GetField("modules", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (modulesField == null) return;

        Type baseModuleType = modulesField.FieldType.GetGenericArguments()[0];
        var derivedTypes = TypeCache.GetTypesDerivedFrom(baseModuleType).Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType);

        foreach (var type in derivedTypes)
        {
            string typeName = type.Name.Replace("Module_", "");
            string category = "Etc";
            string lowerName = typeName.ToLower();

            if (lowerName.Contains("spawn") || lowerName.Contains("create")) category = "Spawn";
            else if (lowerName.Contains("damage") || lowerName.Contains("attack")) category = "Combat";
            else if (lowerName.Contains("move") || lowerName.Contains("teleport") || lowerName.Contains("target") || lowerName.Contains("dash")) category = "Movement";
            else if (lowerName.Contains("sound") || lowerName.Contains("camera") || lowerName.Contains("vfx")) category = "Visual & Audio";
            else if (lowerName.Contains("charge") || lowerName.Contains("wait")) category = "Timing";

            menu.AddItem(new GUIContent($"{category}/{typeName}"), false, () =>
            {
                listProp.serializedObject.Update();
                listProp.arraySize++;
                SerializedProperty element = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                element.managedReferenceValue = Activator.CreateInstance(type);
                listProp.serializedObject.ApplyModifiedProperties();
            });
        }
        menu.ShowAsContext();
    }

    // ==========================================
    // 3. 오른쪽 패널 (노드 에디터 캔버스)
    // ==========================================
    private void DrawRightNodeCanvasPanel()
    {
        EditorGUILayout.BeginVertical("window", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("🔗 Phase Flow Canvas (우클릭 드래그로 뷰 이동)", EditorStyles.boldLabel);
        if (selectedSkill != null && GUILayout.Button("+ 페이즈 추가", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            AddPhaseToSelectedSkill();
        EditorGUILayout.EndHorizontal();

        Rect canvasRect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUI.Box(canvasRect, "", EditorStyles.helpBox);

        if (selectedSkill == null)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        HandleCanvasPan(canvasRect);

        GUI.BeginGroup(canvasRect);
        DrawPhaseNodes();
        GUI.EndGroup();

        EditorGUILayout.EndVertical();
    }

    private void HandleCanvasPan(Rect rect)
    {
        Event e = Event.current;
        if (rect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDown && e.button == 1) isDraggingCanvas = true;
            else if (e.type == EventType.MouseDrag && isDraggingCanvas) { nodePanOffset += e.delta; Repaint(); }
            else if (e.type == EventType.MouseUp && e.button == 1) isDraggingCanvas = false;
        }
    }

    private void DrawPhaseNodes()
    {
        SerializedObject so = new SerializedObject(selectedSkill);
        SerializedProperty phaseListProp = so.FindProperty("phaseList");
        if (phaseListProp == null) return;
        so.Update();

        float startX = 50f + nodePanOffset.x;
        float startY = 80f + nodePanOffset.y;
        float nodeWidth = 180f;
        float spacingX = 60f;

        for (int i = 0; i < phaseListProp.arraySize - 1; i++)
        {
            float posX1 = startX + (i * (nodeWidth + spacingX));
            float posX2 = startX + ((i + 1) * (nodeWidth + spacingX));
            DrawNodeCurve(new Vector2(posX1 + nodeWidth, startY + 20f), new Vector2(posX2, startY + 20f));
        }

        for (int i = 0; i < phaseListProp.arraySize; i++)
        {
            SerializedProperty phaseProp = phaseListProp.GetArrayElementAtIndex(i);
            SerializedProperty modulesProp = phaseProp.FindPropertyRelative("modules");
            int modCount = modulesProp != null ? modulesProp.arraySize : 0;

            int headerCount = 0;
            for (int m = 0; m < modCount; m++)
            {
                string tGroup = GetTriggerGroupName(modulesProp.GetArrayElementAtIndex(m));
                string pGroup = m > 0 ? GetTriggerGroupName(modulesProp.GetArrayElementAtIndex(m - 1)) : "";
                if (m == 0 || tGroup != pGroup) headerCount++;
            }

            float nodeHeight = 45f + (modCount * 22f) + (headerCount * 18f);
            float posX = startX + (i * (nodeWidth + spacingX));
            Rect nodeRect = new Rect(posX, startY, nodeWidth, nodeHeight);

            Color oldBg = GUI.backgroundColor;
            if (selectedPhaseIndex == i && selectedModuleIndex == -1) GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            GUI.Box(nodeRect, "", "window");
            GUI.backgroundColor = oldBg;

            if (GUI.Button(new Rect(nodeRect.x, nodeRect.y, nodeRect.width, 25f), $"Phase [{i}]", EditorStyles.boldLabel))
            {
                selectedPhaseIndex = i; selectedModuleIndex = -1; GUI.FocusControl(null);
            }

            if (GUI.Button(new Rect(nodeRect.xMax - 25, nodeRect.y + 2, 20, 20), "X"))
            {
                phaseListProp.DeleteArrayElementAtIndex(i);
                if (selectedPhaseIndex == i) selectedPhaseIndex = -1;
                break;
            }

            float currentY = nodeRect.y + 30f;
            for (int m = 0; m < modCount; m++)
            {
                SerializedProperty modProp = modulesProp.GetArrayElementAtIndex(m);
                string triggerGroup = GetTriggerGroupName(modProp);
                string prevGroup = m > 0 ? GetTriggerGroupName(modulesProp.GetArrayElementAtIndex(m - 1)) : "";

                if (m == 0 || triggerGroup != prevGroup)
                {
                    GUI.Label(new Rect(nodeRect.x + 5, currentY, nodeWidth - 10, 15f), $"- {triggerGroup} -", EditorStyles.centeredGreyMiniLabel);
                    currentY += 18f;
                }

                Rect modRect = new Rect(nodeRect.x + 5, currentY, nodeWidth - 10, 20f);

                oldBg = GUI.backgroundColor;
                if (selectedPhaseIndex == i && selectedModuleIndex == m) GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);

                if (GUI.Button(modRect, GetModuleSummary(modProp), EditorStyles.miniButton))
                {
                    selectedPhaseIndex = i; selectedModuleIndex = m; GUI.FocusControl(null);
                }
                GUI.backgroundColor = oldBg;
                currentY += 22f;
            }
        }
        so.ApplyModifiedProperties();
    }

    private void DrawPropertyIfExists(SerializedObject so, string propName)
    {
        var prop = so.FindProperty(propName);
        if (prop != null) EditorGUILayout.PropertyField(prop, true);
    }

    private void DrawNodeCurve(Vector2 start, Vector2 end)
    {
        Handles.BeginGUI();
        Handles.DrawBezier(start, end, start + Vector2.right * 40f, end + Vector2.left * 40f, Color.cyan, null, 3f);
        Handles.EndGUI();
    }

    private void CreateNewActiveSkill()
    {
        // 1. 유저에게 저장 경로를 묻는 팝업 띄우기 (기본 시작 폴더: SkillFolder)
        string defaultName = $"NewActiveSkill_{1000 + activeSkills.Count}";
        string path = EditorUtility.SaveFilePanelInProject(
            "새 액티브 스킬 저장",
            defaultName,
            "asset",
            "스킬 데이터(SO)를 저장할 위치와 이름을 지정하세요.",
            SkillFolder);

        // 2. 유저가 취소(Cancel)를 눌렀거나 창을 닫았으면 생성 중단
        if (string.IsNullOrEmpty(path)) return;

        // 3. 인스턴스 생성 (문자열 기반으로 생성하여 클래스 의존성 분리)
        // 🚨 만약 프로젝트 내 액티브 스킬 클래스 이름이 다르면 "SO_ActiveSkillData"를 알맞게 수정하세요!
        ScriptableObject newSkill = ScriptableObject.CreateInstance("SO_ActiveSkillData");

        if (newSkill == null)
        {
            Debug.LogError("SO_ActiveSkillData 클래스를 찾을 수 없습니다. 클래스명이 맞는지 확인해주세요.");
            return;
        }

        // 4. 기본값 세팅 (생성되자마자 ID와 이름을 임시로 채워줌)
        SerializedObject so = new SerializedObject(newSkill);
        var nameProp = so.FindProperty("skillName") ?? so.FindProperty("SkillName");
        if (nameProp != null) nameProp.stringValue = "New Active Skill";

        var idProp = so.FindProperty("skillID") ?? so.FindProperty("id");
        if (idProp != null) idProp.intValue = 1000 + activeSkills.Count;

        so.ApplyModifiedPropertiesWithoutUndo();

        // 5. 실제 에셋으로 저장 및 리스트 갱신
        AssetDatabase.CreateAsset(newSkill, path);
        AssetDatabase.SaveAssets();

        RefreshSkillList();

        // 6. 새로 만든 스킬을 윈도우에서 바로 선택 상태로 만들어줌
        selectedSkill = newSkill;
        selectedPhaseIndex = -1;
        selectedModuleIndex = -1;
        GUI.FocusControl(null);
    }

    private void AddPhaseToSelectedSkill()
    {
        if (selectedSkill == null) return;
        SerializedObject so = new SerializedObject(selectedSkill);
        SerializedProperty prop = so.FindProperty("phaseList");
        if (prop != null)
        {
            so.Update(); prop.arraySize++; so.ApplyModifiedProperties(); EditorUtility.SetDirty(selectedSkill);
            selectedPhaseIndex = prop.arraySize - 1;
        }
    }
}