using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SkillRuntimeData
{
    public SO_SkillData template; // 어떤 스킬의 템플릿인지 참조
    public int currentLevel;    // 플레이어가 배운 현재 레벨
    public bool isUnlocked;     // 해금 여부
    public bool IsDevelopmentLocked => template != null && template.isDevelopmentLocked;

    public Action<SkillRuntimeData> OnDataChanged;

    public int GetSkillID()
    {
        return template.id;
    }

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

    public void OpenSkill()
    {
        if (IsDevelopmentLocked) return;
        isUnlocked = true;

        OnDataChanged?.Invoke(this);
    }

    public void IncreaseSkillLevel()
    {
        if (IsDevelopmentLocked) return;
        currentLevel = Mathf.Clamp(currentLevel + 1, 0, GetMaxSkillLevel());

        OnDataChanged?.Invoke(this);
    }

    public void DecreaseSKillLevel()
    {
        currentLevel = Mathf.Clamp(currentLevel - 1, 0, currentLevel);

        OnDataChanged?.Invoke(this);
    }

    public void SetMaxSkillLevel()
    {
        if (IsDevelopmentLocked) return;
        currentLevel = GetMaxSkillLevel();

        OnDataChanged?.Invoke(this);
    }

    public override string ToString()
    {
        return GetSkillName();
    }
}


// 선택된 캐릭터에서 스킬트리를 선택해서 넘어오면
// 캐릭터 ID와 캐릭터의 직업군 ID를 받아내고 그 정보값들로 스킬트리UI에게 전달하여 
// 화면을 그리게 한다. 

public class SkillTreeManager
    : Singleton<SkillTreeManager>
{
    [SerializeField] private List<SkillTree> classSkillTreeList;
    [SerializeField] private SkillTree commonSkillTree;
    [SerializeField] private List<SkillTree> onwerSkillTreeList;

    private int selectedCharacterID = -1;  // 스킬트리를 결정한 캐릭터 ID 
    //private int selectedClassId = 1;          // 스킬트리를 결정한 직업 ID 

    // 선택한 스킬 정보 
    private SkillRuntimeData selectedSkillData;
    public SkillRuntimeData SelectedSkillData { get { return selectedSkillData; } set { selectedSkillData = value; } }

    public enum SkillTreeCategory
    {
        Theme,      // 직업군 전용 
        Common,     // 공용
        Personal,   // 캐릭터 전용
    }
    private Dictionary<int, SkillTree> skillByClassIdTable = new();
    private Dictionary<int, SkillTree> skillByOwnerIDTable = new();
    private Dictionary<SkillTreeCategory, SkillTree> skillTreeByCategoryTable = new();

    private bool isDirty = false;
    public bool IsDirty => isDirty;
    public Action OnDataChanged;

    protected override void Awake()
    {
        Debug.Log($"Awake {GetInstanceID()} Dirty:{isDirty}");
        base.Awake();

        if (IsDuplicate) return;

        InitializeClassSkillTable();

        var loadData = SaveManager.LoadSkillData();
        if (loadData != null)
            ApplySavedSkills(loadData);
    }

    protected override void Start()
    {
        selectedSkillData = null;

        base.Start();
    }

    protected override void SyncDataFromSingleton()
    {
        if (Instance != this)
        {
            isDirty = Instance.isDirty;
            classSkillTreeList = Instance.classSkillTreeList;
            commonSkillTree = Instance.commonSkillTree;
            onwerSkillTreeList = Instance.onwerSkillTreeList;
        }
    }

    public SkillTree GetSkillTableByClassId(int classId)
    {
        return skillByClassIdTable[classId];
    }

    public SkillTree GetSkillTree(SkillTreeCategory category, int classID)
    {
        switch (category)
        {
            case SkillTreeCategory.Theme:
                return skillByClassIdTable[classID];

            case SkillTreeCategory.Common:
                return commonSkillTree;

            case SkillTreeCategory.Personal:
                return skillByOwnerIDTable[selectedCharacterID];
        }


        return null;
    }


    public IEnumerable<SkillRuntimeData> GetAvailableSkills(int classID)
    {
        var trees = new List<SkillTree>();
        if (skillByClassIdTable.TryGetValue(classID, out var tree)) trees.Add(tree);
        if (commonSkillTree != null) trees.Add(commonSkillTree);
        return trees.Where(t => t.allSkills != null).SelectMany(t => t.allSkills
            .Where(s => s != null).Select(s => t.GetSkillRuntimeDataByID(s.id)))
            .Where(s => s != null && !s.IsDevelopmentLocked).GroupBy(s => s.GetSkillID()).Select(g => g.First());
    }

    public void SkillUnlock(SkillRuntimeData data)
    {
        if (data == null || data.IsDevelopmentLocked) return;
        data.isUnlocked = true;
    }

    private IEnumerable<SkillTree> GetAllSkillTrees()
        => (classSkillTreeList ?? new List<SkillTree>())
            .Concat(onwerSkillTreeList ?? new List<SkillTree>())
            .Append(commonSkillTree).Where(tree => tree?.allSkills != null).Distinct();

    public SO_SkillData FindSkillTemplate(int skillID)
        => GetAllSkillTrees().SelectMany(tree => tree.allSkills)
            .FirstOrDefault(skill => skill != null && skill.id == skillID);

    public int LearnAllSkillsForCheat()
    {
        int changed = 0;
        foreach (var tree in GetAllSkillTrees())
        {
            // Common/personal trees may not have been opened in the lobby yet.
            if (tree.allSkills.Count > 0 && tree.allSkills.All(skill => skill != null && tree.GetSkillRuntimeDataByID(skill.id) == null))
                tree.Initialize(OnSkillRuntimeDataChanged);
            foreach (var template in tree.allSkills)
            {
                if (template == null || template.isDevelopmentLocked || template.maxLevel < 1) continue;
                var data = tree.GetSkillRuntimeDataByID(template.id);
                if (data == null || (data.isUnlocked && data.currentLevel > 0)) continue;
                data.currentLevel = Mathf.Clamp(data.currentLevel, 1, template.maxLevel);
                data.OpenSkill();
                changed++;
            }
        }
        return changed;
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
            skillTree.Initialize(OnSkillRuntimeDataChanged);
            skillByClassIdTable.Add(skillTree.id, skillTree);
        }
    }

    public void ApplySavedSkills(CharacterSkillData savedData)
    {
        if (savedData == null) return;
        SkillTree tree = GetSkillTableByClassId(savedData.classID);
        if (tree == null) return;

        foreach (var skillSave in savedData.skillSaveDatas)
        {
            var runtimeSkill = tree.GetSkillRuntimeDataByID(skillSave.skillID);
            if (runtimeSkill == null) continue;

            runtimeSkill.currentLevel = skillSave.skillLevel;
            runtimeSkill.isUnlocked = skillSave.unlocked;

            if (runtimeSkill.template is SO_PassiveSkillData so_passive)
            {
                //앱매니저에게 해당 스킬이 레벨업되었다고 알림
                AppManager.Instance.SafeInvoke(v => v.OnChangedLevelPassiveSkill(so_passive.jobID, runtimeSkill));
            }
        }
    }

    public SkillRuntimeData GetSkillRuntimeData(int classID, int skillID)
    {
        if (skillByClassIdTable.TryGetValue(classID, out SkillTree tree))
            return tree.GetSkillRuntimeDataByID(skillID);
        return null;
    }

    public void OnSkillRuntimeDataChanged(SkillRuntimeData data)
    {
        isDirty = true;
        OnDataChanged?.Invoke();

        Debug.Log(
            $"Dirty TRUE {GetInstanceID()}"
        );
        if (data.template is SO_PassiveSkillData so_passive)
        {
            //앱매니저에게 해당 스킬이 레벨업되었다고 알림
            AppManager.Instance.SafeInvoke(v => v.OnChangedLevelPassiveSkill(so_passive.jobID, data));
        }
    }

    private void OnDestroy()
    {
        Debug.Log(
            $"Destroy {GetInstanceID()} Dirty:{isDirty}"
        );
    }

    public void SaveIfDirty()
    {
        if (isDirty == false)
            return;

        // 스킬 정보 저장 
        CharacterSkillData charSkillData = new CharacterSkillData { charID = 1, classID = 1 };

        //TODO : id 지정해야할 듯 
        SkillTree skillTree = GetSkillTableByClassId(1);
        if (skillTree == null) return;

        List<SkillSaveData> saveList = new();
        for (int i = 0; i <= 50; i++)
        {
            List<SkillRuntimeData> rdList = skillTree.GetSkillRuntimeDatasByLevel(i);
            foreach (SkillRuntimeData rd in rdList)
            {
                SkillSaveData saveData = new SkillSaveData();
                saveData.skillID = rd.template.id;
                saveData.skillLevel = rd.currentLevel;
                saveData.unlocked = rd.isUnlocked;
                saveList.Add(saveData);
            }
        }
        charSkillData.skillSaveDatas = saveList;

        SaveManager.SaveSkillData(charSkillData);
        isDirty = false;
    }
}

