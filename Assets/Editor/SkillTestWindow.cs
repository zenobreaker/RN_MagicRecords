using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SkillTestWindow : EditorWindow
{
    private const string SkillFolder = "Assets/10.ScriptableObjects/Resources/Skills";
    private const float LeftPanelWidth = 270f;
    private const float GridItemWidth = 84f;

    private readonly List<SO_SkillData> skills = new();
    private SO_SkillData selectedSkill;
    private SkillSlot selectedSlot = SkillSlot.SLOT1;
    private int testSkillLevel = 1;
    private Vector2 scrollPos;

    [MenuItem("Tools/Skill Tester")]
    public static void ShowWindow()
    {
        SkillTestWindow window = GetWindow<SkillTestWindow>("Skill Tester");
        window.minSize = new Vector2(860f, 520f);
        window.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(860f, 520f);
        RefreshSkills();
    }

    private void RefreshSkills()
    {
        skills.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:SO_SkillData", new[] { SkillFolder }))
        {
            SO_SkillData skill = AssetDatabase.LoadAssetAtPath<SO_SkillData>(AssetDatabase.GUIDToAssetPath(guid));
            if (skill != null)
                skills.Add(skill);
        }
        skills.Sort((left, right) => left.id.CompareTo(right.id));
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("스킬 테스터", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        EditorGUILayout.Space(6f);
        DrawSkillList();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(LeftPanelWidth), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("선택한 스킬", EditorStyles.boldLabel);
        if (selectedSkill == null)
        {
            EditorGUILayout.HelpBox("오른쪽 목록에서 테스트할 스킬을 선택하세요.", MessageType.Info);
        }
        else
        {
            Texture2D icon = selectedSkill.skillImage != null ? selectedSkill.skillImage.texture : null;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(icon, GUILayout.Width(64f), GUILayout.Height(64f));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(selectedSkill.skillName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"ID : {selectedSkill.id}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("테스트용 스킬 레벨");
            testSkillLevel = EditorGUILayout.IntSlider(testSkillLevel, 1, Mathf.Max(1, selectedSkill.maxLevel));
            if (GUILayout.Button("스킬 선택 해제"))
                selectedSkill = null;
        }

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("스킬 슬롯", EditorStyles.boldLabel);
        DrawSlotButtons();
        EditorGUILayout.Space(8f);
        EditorGUI.BeginDisabledGroup(selectedSkill == null || !Application.isPlaying);
        if (GUILayout.Button("스킬 장착", GUILayout.Height(28f)))
            SendSkillTestCommand(selectedSkill);
        if (GUILayout.Button("선택 스킬 해제"))
            SkillTestInvoker.UndoSkill(selectedSlot, selectedSkill);
        EditorGUI.EndDisabledGroup();
        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("스킬 장착과 해제는 플레이 모드에서만 가능합니다.", MessageType.Warning);
        EditorGUILayout.EndVertical();
    }

    private void DrawSlotButtons()
    {
        const int columns = 2;
        int buttonIndex = 0;
        foreach (SkillSlot slot in Enum.GetValues(typeof(SkillSlot)))
        {
            if (slot == SkillSlot.MAX)
                continue;
            if (buttonIndex % columns == 0)
                EditorGUILayout.BeginHorizontal();
            Color previousColor = GUI.backgroundColor;
            if (slot == selectedSlot)
                GUI.backgroundColor = new Color(0.45f, 0.75f, 1f);
            if (GUILayout.Button(slot.ToString(), GUILayout.Height(28f)))
                selectedSlot = slot;
            GUI.backgroundColor = previousColor;
            buttonIndex++;
            if (buttonIndex % columns == 0)
                EditorGUILayout.EndHorizontal();
        }
        if (buttonIndex % columns != 0)
            EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillList()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"스킬 목록 ({skills.Count})", EditorStyles.boldLabel);
        if (GUILayout.Button("새로고침", GUILayout.Width(70f)))
            RefreshSkills();
        EditorGUILayout.EndHorizontal();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - LeftPanelWidth - 40f) / GridItemWidth));
        int index = 0;
        foreach (SO_SkillData skill in skills)
        {
            if (index % columns == 0)
                EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(GridItemWidth - 4f));
            Texture2D icon = skill.skillImage != null ? skill.skillImage.texture : null;
            Color previousColor = GUI.backgroundColor;
            if (skill == selectedSkill)
                GUI.backgroundColor = new Color(0.45f, 0.75f, 1f);
            if (GUILayout.Button(icon, GUILayout.Width(64f), GUILayout.Height(64f)))
            {
                selectedSkill = skill;
                testSkillLevel = Mathf.Clamp(testSkillLevel, 1, Mathf.Max(1, skill.maxLevel));
            }
            GUI.backgroundColor = previousColor;
            EditorGUILayout.LabelField(skill.skillName, EditorStyles.miniLabel, GUILayout.Width(GridItemWidth - 4f));
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

    private void SendSkillTestCommand(SO_SkillData skill)
    {
        SkillTestInvoker.ReceiveSkillForTest(selectedSlot, skill, testSkillLevel);
        Debug.Log($"Skill Test : {skill.skillName} 실행 요청 전송");
    }
}
