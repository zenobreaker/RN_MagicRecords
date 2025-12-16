using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SkillTestWindow : EditorWindow
{
    private SO_SkillData selectedSkill;

    private SkillSlot selectedSlot = SkillSlot.Slot1;
    private string[] slotNames;
    private int testSkilLevel = 1; 

    [MenuItem("Tools/Skill Tester")]
    public static void ShowWindow()
    {
        GetWindow<SkillTestWindow>("Skill Tester");
    }

    private void OnEnable()
    {
        slotNames = System.Enum.GetNames(typeof(SkillSlot));
    }
    

    private Vector2 scrollPos;

    private void DrawSkillGrid()
    {
        string[] guids = AssetDatabase.FindAssets("t:SO_SkillData",
            new[] { "Assets/10.ScriptableObjects/Resources/Skills" });

        List<SO_SkillData> skills = new List<SO_SkillData>();
        foreach(string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SO_SkillData skill = AssetDatabase.LoadAssetAtPath<SO_SkillData>(path);
            if(skill != null)
                skills.Add(skill);
        }

        skills.Sort((a,b)=> a.id.CompareTo(b.id));  


        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));

        int columns = 4;
        int count = 0;

        EditorGUILayout.BeginHorizontal();

        foreach(var skill in skills)
        {
            if (count % columns == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }

            GUILayout.BeginVertical(GUILayout.Width(80));

            Texture2D icon = skill.skillImage != null ? skill.skillImage.texture : null;

            if (GUILayout.Button(icon, GUILayout.Width(64), GUILayout.Height(64)))
            {
                selectedSkill = skill;
            }

            GUILayout.Label(skill.skillName, GUILayout.Width(80));

            GUILayout.EndVertical();

            count++;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }


    private void OnGUI()
    {
        GUILayout.Label("스킬 테스터", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 슬롯 선택
        GUILayout.Label("스킬 슬롯 선택");
        //selectedSlot = (SkillSlot)EditorGUILayout.EnumPopup("스킬 슬롯", selectedSlot); 
        selectedSlot = (SkillSlot)EditorGUILayout.Popup((int)selectedSlot, slotNames); 
        GUILayout.Space(10);

        GUILayout.Label("스킬 선택");
        // --- 선택한 스킬 요약 표시 영역 ---
        if (selectedSkill != null)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("선택된 스킬 미리보기", EditorStyles.boldLabel);

            // 아이콘 표시
            Texture2D icon = selectedSkill.skillImage != null ? selectedSkill.skillImage.texture : null;
            GUILayout.Label(icon, GUILayout.Width(64), GUILayout.Height(64));

            // 스킬 기본 정보
            GUILayout.Label($"이름 : {selectedSkill.skillName}");
            GUILayout.Label($"ID : {selectedSkill.id}");
            

            // 스킬 레벨 선택 
            GUILayout.Space(5);

            GUILayout.Label("테스트용 스킬 레벨");
            testSkilLevel = EditorGUILayout.IntSlider(testSkilLevel, 1, selectedSkill.maxLevel);


            // 스킬 해제 버튼 
            GUILayout.Space(5);
            EditorGUI.BeginDisabledGroup(Application.isPlaying == false);
            if (GUILayout.Button("스킬 해제"))
            {
                SkillTestInvoker.UndoSkill(selectedSlot, selectedSkill); 
            }
            EditorGUI.EndDisabledGroup();

            // 선택 취소 버튼
            GUILayout.Space(5);

            if (GUILayout.Button("선택 해제"))
                selectedSkill = null;

            GUILayout.EndVertical();
        }

        GUILayout.Space(5);
        EditorGUI.BeginDisabledGroup(selectedSkill == null);
        if (GUILayout.Button("스킬 장착"))
        {
            if (Application.isPlaying == false)
            {
                Debug.Log($"Application이 실행 중이 아닙니다");
                return;
            }

            //var skill = skillList[selectedSkillIndex];
            SendSkillTestCommand(selectedSkill);
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);
        DrawSkillGrid();
    }

    private void SendSkillTestCommand(SO_SkillData skill)
    {
        SkillTestInvoker.ReceiveSkillForTest(selectedSlot, skill, testSkilLevel);
        Debug.Log($"Skill Test : {skill.skillName} 실행 요청 전송");
    }
}
