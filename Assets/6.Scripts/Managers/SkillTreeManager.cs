using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SkillRuntimeData
{
    public SO_SkillData template; // 어떤 스킬의 템플릿인지 참조
    public int currentLevel;    // 플레이어가 배운 현재 레벨
    public bool isUnlocked;     // 해금 여부

    public string GetSkillName()
    {
        if (template == null) return "";

        return template.skillName;
    }

    public string GetSkillDesc()
    {
        if (template == null) return "";

        return template.skillDescription; 
    }


    public int GetMaxSkillLevel()
    {
        if (template == null) return 0;

        return template.maxLevel;
    }

    public override string ToString()
    {
        return GetSkillName();
    }
}


// 선택된 캐릭터에서 스킬트리를 선택해서 넘어오면
// 캐릭터 ID와 캐릭터의 직업군 ID를 받아내고 그 정보값들로 스킬트리UI에게 전달하여 
// 화면을 그리게 한다. 

public class SkillTreeManager : MonoBehaviour
{
    [SerializeField] private List<SkillTree> classSkillTreeList;
    [SerializeField] private List<SkillTree> onwerSkillTreeList;

    private int selectedCharacterId = -1;  // 스킬트리를 결정한 캐릭터 ID 
    private int selectedClassId = 1;          // 스킬트리를 결정한 직업 ID 

    // 선택한 스킬 정보 
    private SkillRuntimeData selectedSkillData; 
    public SkillRuntimeData SelectedSkillData { get {  return selectedSkillData; }  set { selectedSkillData = value; } }

    public enum SkillTreeCategory
    {
        Theme,      // 직업군 전용 
        Common,     // 공용
        Personal,   // 캐릭터 전용
    }
    Dictionary<int, SkillTree> skillByClassIdTable = new();
    Dictionary<SkillTreeCategory, SkillTree> skillTreeByCategoryTable = new();

    private void Awake()
    {
        InitializeClassSkillTable();
    }

    private void Start()
    {
        selectedSkillData = null;
    }

    public SkillTree GetSkillTableByClassId(int classId)
    {
        return skillByClassIdTable[classId];
    }


    public void SkillUnlock(SkillRuntimeData data)
    {
        data.isUnlocked = true;
    }

    public void SetSkillTree(int ownerId, int classId)
    {
        skillTreeByCategoryTable.Clear();

        skillTreeByCategoryTable[SkillTreeCategory.Theme] = skillByClassIdTable[classId];
    }

    // 직업 스킬트리 
    public void InitializeClassSkillTable()
    {
        skillByClassIdTable.Clear();

        foreach (SkillTree skillTree in classSkillTreeList)
        {
            skillTree.Initialize();
            skillByClassIdTable.Add(skillTree.id, skillTree);
        }
    }
}

