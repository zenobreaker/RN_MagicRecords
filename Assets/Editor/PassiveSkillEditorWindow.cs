using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PassiveSkillEditorWindow : EditorWindow
{
    private const string SkillFolder = "Assets/10.ScriptableObjects";
    private readonly List<SO_PassiveSkillData> skills = new();
    [SerializeField] private SO_PassiveSkillData selectedSkill;
    [SerializeField] private int selectedModuleIndex = -1;
    [SerializeField] private Vector2 nodePanOffset;
    private SerializedObject serializedSkill;
    private Vector2 leftScroll, centerScroll;
    private string search = "";
    private bool draggingCanvas;

    [MenuItem("Tools/Passive Skill Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<PassiveSkillEditorWindow>("Passive Skill Editor");
        window.minSize = new Vector2(1200f, 700f);
        window.Show();
    }

    public static void Open(SO_PassiveSkillData skill)
    {
        ShowWindow();
        GetWindow<PassiveSkillEditorWindow>().SelectSkill(skill);
    }

    private void OnEnable()
    {
        minSize = new Vector2(1200f, 700f);
        RefreshSkillList();
        Undo.undoRedoPerformed += OnUndoRedo;
        EditorApplication.projectChanged += OnProjectChanged;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
        EditorApplication.projectChanged -= OnProjectChanged;
        serializedSkill?.Dispose();
        serializedSkill = null;
    }

    private void OnLostFocus() => draggingCanvas = false;
    private void OnProjectChanged() { RefreshSkillList(); Repaint(); }
    private void OnUndoRedo()
    {
        // Undo can replace or reorder managed references; return to the complete list.
        selectedModuleIndex = -1;
        serializedSkill?.Update();
        Repaint();
    }

    private void RefreshSkillList()
    {
        skills.Clear();
        skills.AddRange(AssetDatabase.FindAssets("t:SO_PassiveSkillData", new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<SO_PassiveSkillData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(skill => skill != null).OrderBy(skill => skill.id).ThenBy(skill => skill.name));
    }

    private void SelectSkill(SO_PassiveSkillData skill)
    {
        serializedSkill?.Dispose();
        selectedSkill = skill;
        serializedSkill = skill != null ? new SerializedObject(skill) : null;
        selectedModuleIndex = -1;
        nodePanOffset = Vector2.zero;
        centerScroll = Vector2.zero;
        draggingCanvas = false;
        GUI.FocusControl(null);
        Repaint();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSkillList();
            if (selectedSkill != null)
            {
                if (serializedSkill == null || serializedSkill.targetObject != selectedSkill)
                {
                    serializedSkill?.Dispose();
                    serializedSkill = new SerializedObject(selectedSkill);
                }
                serializedSkill.Update();
                if (selectedModuleIndex >= serializedSkill.FindProperty("Modules").arraySize)
                    selectedModuleIndex = -1;
            }
            DrawDetails();
            DrawCanvas();
            if (selectedSkill != null) serializedSkill.ApplyModifiedProperties();
        }
    }

    private void DrawSkillList()
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(280f), GUILayout.ExpandHeight(true)))
        {
            EditorGUILayout.LabelField("Passive Skills", EditorStyles.boldLabel);
            if (GUILayout.Button("새로고침", EditorStyles.miniButton)) RefreshSkillList();
            if (GUILayout.Button("+ 새 Passive Skill 만들기", GUILayout.Height(30f))) CreateNewSkill();
            search = EditorGUILayout.TextField("검색 (이름 / ID)", search);
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
            var style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft, fixedHeight = 40f, fontSize = 11,
                clipping = TextClipping.Clip
            };
            foreach (var skill in skills)
            {
                if (skill == null) continue;
                string title = $"[{skill.id}] {(string.IsNullOrEmpty(skill.skillName) ? skill.name : skill.skillName)}";
                if (!string.IsNullOrEmpty(search) && title.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                Color previous = GUI.backgroundColor;
                if (skill == selectedSkill) GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
                var icon = skill.skillImage != null ? AssetPreview.GetAssetPreview(skill.skillImage) : null;
                if (GUILayout.Button(new GUIContent(title, icon, AssetDatabase.GetAssetPath(skill)), style))
                    SelectSkill(skill);
                GUI.backgroundColor = previous;
            }
            EditorGUILayout.EndScrollView();
            if (skills.Count == 0) EditorGUILayout.HelpBox("패시브 스킬 에셋을 만들어 모듈을 추가하세요.", MessageType.Info);
        }
    }

    private void DrawDetails()
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(370f), GUILayout.ExpandHeight(true)))
        {
            if (selectedSkill == null)
            {
                EditorGUILayout.HelpBox("왼쪽 목록에서 스킬을 선택해주세요.", MessageType.Info);
                return;
            }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("기본 정보 / 모듈 목록", EditorStyles.toolbarButton)) selectedModuleIndex = -1;
                if (GUILayout.Button("에셋 찾기", EditorStyles.toolbarButton)) EditorGUIUtility.PingObject(selectedSkill);
                if (GUILayout.Button("저장", EditorStyles.toolbarButton))
                {
                    serializedSkill.ApplyModifiedProperties();
                    AssetDatabase.SaveAssetIfDirty(selectedSkill);
                }
            }
            centerScroll = EditorGUILayout.BeginScrollView(centerScroll);
            var modules = serializedSkill.FindProperty("Modules");
            if (selectedModuleIndex < 0)
            {
                EditorGUILayout.LabelField("Base Properties", EditorStyles.boldLabel);
                // Include inherited and future skill fields, but edit Modules with our own list.
                var property = serializedSkill.GetIterator();
                bool enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (property.name == "m_Script" || property.name == "Modules") continue;
                    EditorGUILayout.PropertyField(property, true);
                }
                EditorGUILayout.Space(10f);
                DrawModules(modules);
            }
            else
            {
                var module = modules.GetArrayElementAtIndex(selectedModuleIndex);
                EditorGUILayout.LabelField("Module Detail", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(GetModuleName(module), EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space(8f);
                if (module.managedReferenceValue == null)
                    EditorGUILayout.HelpBox("비어 있거나 타입을 찾을 수 없는 모듈입니다. 삭제 후 모듈을 추가해주세요.", MessageType.Warning);
                else
                {
                    // Draw children directly so the existing PassiveModule drawer is not re-entered.
                    var child = module.Copy();
                    var end = child.GetEndProperty();
                    if (child.NextVisible(true))
                        do
                        {
                            if (SerializedProperty.EqualContents(child, end)) break;
                            EditorGUILayout.PropertyField(child, true);
                        } while (child.NextVisible(false));
                    DrawTriggerNotice(module);
                }
                EditorGUILayout.Space(10f);
                if (GUILayout.Button("모듈 삭제")) DeleteModule(modules, selectedModuleIndex);
                if (GUILayout.Button("◀ 기본 정보 / 모듈 목록으로 돌아가기", GUILayout.Height(30f))) selectedModuleIndex = -1;
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawModules(SerializedProperty modules)
    {
        EditorGUILayout.LabelField($"Passive Modules ({modules.arraySize})", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("같은 발동 시점의 모듈은 목록 순서대로 실행됩니다.", MessageType.Info);
        for (int i = 0; i < modules.arraySize; i++)
        {
            var module = modules.GetArrayElementAtIndex(i);
            bool changed = false;
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(GetTriggerName(module), EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent($"[{i}] {GetModuleName(module)}", GetModuleName(module)), EditorStyles.miniButton))
                        SelectModule(i);
                    using (new EditorGUI.DisabledScope(i == 0))
                        if (GUILayout.Button("▲", GUILayout.Width(25))) { MoveModule(modules, i, i - 1); changed = true; }
                    using (new EditorGUI.DisabledScope(i == modules.arraySize - 1))
                        if (GUILayout.Button("▼", GUILayout.Width(25))) { MoveModule(modules, i, i + 1); changed = true; }
                    if (GUILayout.Button("X", GUILayout.Width(25))) { DeleteModule(modules, i); changed = true; }
                }
            }
            if (changed) break;
        }
        if (GUILayout.Button("+ 모듈 추가 (카테고리별)", GUILayout.Height(30f))) ShowModuleMenu();
    }

    private void SelectModule(int index)
    {
        selectedModuleIndex = index;
        centerScroll = Vector2.zero;
        GUI.FocusControl(null);
        Repaint();
    }

    private void MoveModule(SerializedProperty modules, int from, int to)
    {
        modules.MoveArrayElement(from, to);
        if (selectedModuleIndex == from) selectedModuleIndex = to;
        else if (selectedModuleIndex == to) selectedModuleIndex = from;
        GUI.FocusControl(null);
    }

    private void DeleteModule(SerializedProperty modules, int index)
    {
        modules.DeleteArrayElementAtIndex(index);
        if (selectedModuleIndex == index) selectedModuleIndex = -1;
        else if (selectedModuleIndex > index) selectedModuleIndex--;
        GUI.FocusControl(null);
    }

    private void ShowModuleMenu()
    {
        serializedSkill.ApplyModifiedProperties();
        // Capture the asset, not a SerializedProperty that becomes stale after Undo or selection changes.
        var target = selectedSkill;
        var menu = new GenericMenu();
        PassiveModuleMenu.AddItems(menu, instance =>
        {
            if (target == null) return;
            using (var data = new SerializedObject(target))
            {
                data.Update();
                var modules = data.FindProperty("Modules");
                int index = modules.arraySize++;
                modules.GetArrayElementAtIndex(index).managedReferenceValue = instance;
                data.ApplyModifiedProperties();
                if (selectedSkill == target) SelectModule(index);
            }
            Repaint();
        });
        menu.ShowAsContext();
    }

    private static string GetModuleName(SerializedProperty module)
    {
        if (module.managedReferenceValue == null) return "Empty / Missing Module";
        if (module.managedReferenceValue is Module_Passive_StatBonus)
        {
            var stat = module.FindPropertyRelative("targetStat");
            var value = module.FindPropertyRelative("value");
            return $"스탯 증가 ({(StatusType)stat.intValue}: {value.floatValue:g})";
        }
        string path = PassiveModuleMenu.GetCategoryPath(module.managedReferenceValue.GetType());
        return path.Substring(path.LastIndexOf('/') + 1).Replace("Module_Passive_", "");
    }

    private static string GetTriggerName(SerializedProperty module)
    {
        var trigger = module.FindPropertyRelative("triggerTime");
        return trigger != null ? ((PassiveTriggerTime)trigger.intValue).ToString() : "Empty / Missing";
    }

    private static void DrawTriggerNotice(SerializedProperty module)
    {
        var trigger = module.FindPropertyRelative("triggerTime");
        if (trigger == null) return;
        var timing = (PassiveTriggerTime)trigger.intValue;
        if (timing == PassiveTriggerTime.OnDamaged)
            EditorGUILayout.HelpBox("현재 OnDamaged는 GenericPassiveSkill의 모듈 호출에 연결되어 있지 않습니다.", MessageType.Warning);
    }

    private void DrawCanvas()
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Trigger Canvas (우클릭 드래그로 이동)", EditorStyles.boldLabel);
                if (GUILayout.Button("뷰 초기화", EditorStyles.toolbarButton, GUILayout.Width(75))) nodePanOffset = Vector2.zero;
                using (new EditorGUI.DisabledScope(selectedSkill == null))
                    if (GUILayout.Button("+ 모듈 추가", EditorStyles.toolbarButton, GUILayout.Width(90))) ShowModuleMenu();
            }
            EditorGUILayout.LabelField("발동 시점별 그룹 · 각 그룹은 해당 이벤트가 발생할 때 실행", EditorStyles.miniLabel);
            Rect canvas = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.Box(canvas, GUIContent.none, EditorStyles.helpBox);
            if (selectedSkill == null) return;
            HandleCanvasPan(canvas);
            GUI.BeginGroup(canvas);
            DrawTriggerNodes(serializedSkill.FindProperty("Modules"));
            GUI.EndGroup();
        }
    }

    private void HandleCanvasPan(Rect canvas)
    {
        var evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 1 && canvas.Contains(evt.mousePosition))
        { draggingCanvas = true; evt.Use(); }
        else if (evt.type == EventType.MouseDrag && draggingCanvas)
        { nodePanOffset += evt.delta; evt.Use(); Repaint(); }
        else if (evt.rawType == EventType.MouseUp && draggingCanvas)
        { draggingCanvas = false; Repaint(); }
        if (evt.type == EventType.ContextClick && canvas.Contains(evt.mousePosition)) evt.Use();
    }

    private void DrawTriggerNodes(SerializedProperty modules)
    {
        if (modules.arraySize == 0)
        {
            GUI.Label(new Rect(30, 35, 400, 40), "모듈을 추가하면 발동 시점별로 표시됩니다.", EditorStyles.wordWrappedLabel);
            return;
        }
        var groups = Enumerable.Range(0, modules.arraySize)
            .GroupBy(i => GetTriggerName(modules.GetArrayElementAtIndex(i))).ToArray();
        float y = 35 + nodePanOffset.y;
        for (int g = 0; g < groups.Length; g += 2)
        {
            float rowHeight = 0;
            for (int column = 0; column < 2 && g + column < groups.Length; column++)
            {
                var group = groups[g + column];
                float height = 58 + group.Count() * 29;
                rowHeight = Mathf.Max(rowHeight, height);
                Rect node = new Rect(25 + nodePanOffset.x + column * 270, y, 250, height);
                GUI.Box(node, GUIContent.none, "window");
                GUI.Label(new Rect(node.x + 10, node.y + 8, 230, 22), group.Key, EditorStyles.boldLabel);
                float moduleY = node.y + 35;
                foreach (int index in group)
                {
                    var module = modules.GetArrayElementAtIndex(index);
                    Color previous = GUI.backgroundColor;
                    if (selectedModuleIndex == index) GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
                    string title = $"[{index}] {GetModuleName(module)}";
                    if (GUI.Button(new Rect(node.x + 8, moduleY, 234, 24), new GUIContent(title, title), EditorStyles.miniButton))
                        SelectModule(index);
                    GUI.backgroundColor = previous;
                    moduleY += 29;
                }
            }
            y += rowHeight + 30;
        }
    }

    private void CreateNewSkill()
    {
        var usedIds = new HashSet<int>(AssetDatabase.FindAssets("t:SO_SkillData", new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<SO_SkillData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(skill => skill != null).Select(skill => skill.id));
        int id = 2000;
        while (usedIds.Contains(id)) id++;
        string path = EditorUtility.SaveFilePanelInProject("새 패시브 스킬 저장", $"NewPassiveSkill_{id}", "asset",
            "패시브 스킬 데이터를 저장할 위치와 이름을 지정하세요.", AssetDatabase.IsValidFolder(SkillFolder) ? SkillFolder : "Assets");
        if (string.IsNullOrEmpty(path)) return;
        var skill = CreateInstance<SO_PassiveSkillData>();
        skill.id = id;
        skill.skillName = "New Passive Skill";
        skill.maxLevel = 1;
        skill.skillUpgradeCost = Array.Empty<int>();
        skill.leadingSkillList = new List<int>();
        skill.levelDatas = new List<SkillLevelData> { new SkillLevelData() };
        AssetDatabase.CreateAsset(skill, AssetDatabase.GenerateUniqueAssetPath(path));
        AssetDatabase.SaveAssetIfDirty(skill);
        RefreshSkillList();
        SelectSkill(skill);
        EditorGUIUtility.PingObject(skill);
    }
}
