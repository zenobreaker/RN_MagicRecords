using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PassiveSystem
{
    // key : Job ID 
    protected Dictionary<int, List<PassiveSkill>> passiveSkillList = new();
    private List<PassiveSkill> updatablePassive = new List<PassiveSkill>();

    private int runJobID = -1;
    private readonly List<PassiveSkill> runPassives = new();
    private readonly Dictionary<int, GameObject> activeOwners = new();

    public PassiveSkill GetRunPassive(int jobID, int skillID) =>
        runJobID == jobID ? runPassives.Find(s => s.SkillID == skillID) : null;

    // UI용 목록 스냅샷. 실제 효과 적용과 동일한 중복 제거 규칙을 사용합니다.
    public IReadOnlyList<PassiveSkill> GetPassives(int jobID) =>
        EffectivePassives(jobID).Where(skill => skill != null).ToList().AsReadOnly();

    public void SetStartingPassives(int jobID, List<PassiveSkill> skills)
    {
        if (skills == null || skills.Any(s => s == null || s.SkillID <= 0) ||
            skills.Select(s => s.SkillID).Distinct().Count() != skills.Count)
            throw new ArgumentException("Invalid or duplicate run passive IDs.");
        ResetStartingPassives();
        runJobID = jobID;
        foreach (var skill in skills)
        {
            if (passiveSkillList.TryGetValue(jobID, out var learned))
            {
                var previous = learned.Find(s => s.SkillID == skill.SkillID);
                previous?.OnLose();
                updatablePassive.Remove(previous);
            }
            runPassives.Add(skill);
        }
    }

    public List<ExplorePassiveSaveData> CaptureStartingPassives() => runPassives.Select(s =>
        new ExplorePassiveSaveData { jobID = runJobID, skillID = s.SkillID, level = s.SkillLevel }).ToList();

    public void ResetStartingPassives()
    {
        if (activeOwners.TryGetValue(runJobID, out var owner) && owner != null)
            OnLose(runJobID, owner);
        else
            foreach (var skill in runPassives) { skill.OnLose(); updatablePassive.Remove(skill); }
        runPassives.Clear();
        activeOwners.Remove(runJobID);
        runJobID = -1;
    }

    public bool GrantRunPassive(int jobID, SO_PassiveSkillData template)
    {
        if (template == null || template.id <= 0 || runJobID != jobID) return false;
        var skill = GetRunPassive(jobID, template.id);
        if (skill != null)
        {
            if (skill.SkillLevel >= Math.Max(1, template.maxLevel)) return false;
            int level = skill.SkillLevel + 1;
            skill.SetLevel(level);
            skill.OnChangedLevel(level);
        }
        else
        {
            skill = template.CreateSkill() as PassiveSkill;
            if (skill == null) return false;
            skill.SetLevel(1);
            if (passiveSkillList.TryGetValue(jobID, out var learned))
            {
                var previous = learned.Find(s => s.SkillID == skill.SkillID);
                previous?.OnLose();
                updatablePassive.Remove(previous);
            }
            runPassives.Add(skill);
            if (activeOwners.TryGetValue(jobID, out var owner) && owner != null)
                skill.OnAcquire(owner);
            if (IsOverriden(skill.GetType(), "OnUpdate")) updatablePassive.Unique(skill);
        }
        if (activeOwners.TryGetValue(jobID, out var currentOwner) && currentOwner != null &&
            currentOwner.TryGetComponent<StatusComponent>(out var status)) skill.OnApplyStaticEffect(status);
        return true;
    }

    private IEnumerable<PassiveSkill> EffectivePassives(int jobID)
    {
        if (jobID == runJobID)
            foreach (var skill in runPassives) yield return skill;
        if (passiveSkillList.TryGetValue(jobID, out var regular))
            foreach (var skill in regular)
                if (jobID != runJobID || !runPassives.Any(s => s.SkillID == skill.SkillID)) yield return skill;
    }

    private IEnumerable<PassiveSkill> AllEffectivePassives() =>
        passiveSkillList.Keys.Append(runJobID).Where(id => id >= 0).Distinct().SelectMany(EffectivePassives);

    public void Add(int jobID, PassiveSkill skill)
    {
        if (skill == null) return;
        if (!passiveSkillList.TryGetValue(jobID, out var skills))
            passiveSkillList[jobID] = skills = new List<PassiveSkill>();
        // Legacy stat records have no skill ID (0); keep their reference-based behavior.
        if (skills.Any(s => ReferenceEquals(s, skill) || (skill.SkillID > 0 && s.SkillID == skill.SkillID))) return;
        skills.Add(skill);
    }

    public void Remove(int jobID, PassiveSkill skill)
    {
        if (skill == null || !passiveSkillList.TryGetValue(jobID, out var skills)) return;
        var existing = skills.Find(s => ReferenceEquals(s, skill) || (skill.SkillID > 0 && s.SkillID == skill.SkillID));
        if (existing == null) return;
        existing.OnLose();
        skills.Remove(existing);
        updatablePassive.Remove(existing);
    }
    public void OnInit()
    {
        foreach (var skill in passiveSkillList.Values.SelectMany(skills => skills)) skill.OnLose();
        ResetStartingPassives();
        updatablePassive.Clear();
        activeOwners.Clear();
        passiveSkillList.Clear();

        //TODO : 직업이 추가되면 리스트 추가
        passiveSkillList[Constants.GLOBAL_SHOTER_JOB_ID] = new();
        passiveSkillList[Constants.GLOBAL_RECORD_JOB_ID] = new(); // 레코드 ID 
    }


    public void OnChangedLevel(int jobID, SkillRuntimeData data)
    {
        if (!passiveSkillList.ContainsKey(jobID)) passiveSkillList[jobID] = new();

        int skillID = data.GetSkillID();
        int newLevel = data.currentLevel;

        // 1. list 에서 해당 패시브 객체를 찾는다.
        PassiveSkill passive = passiveSkillList[jobID].Find(s => s.SkillID == skillID);

        // 2. 최소 승급 체크 : 1레벨 이상인데 아직 리스트에 없는 경우
        if (newLevel > 0 && passive == null)
        {
            // a. 템플릿 정보를 이용해 PassiveSkill 객체 생성
            passive = (PassiveSkill)data.template.CreateSkill();

            // b. PassiveSystem에 리스트 추가
            Add(jobID, passive);

            Debug.Log($"[PassiveSystem] ID {skillID} 스킬이 1레벨로 습득");
        }

        // 3. 레벨 동기화 및 정적 효과 재적용
        if (passive != null)
        {
            // 레벨 동기화 
            passive.SetLevel(newLevel);

            //TODO : 어딘가로부터 플레이어 정보를 전달해서 능력치 할당 등의 이벤트 적용 
            //passive.OnApplyStaticEffect()

        }
    }

   private bool IsOverriden(Type type, string methodName)
    {
        System.Reflection.MethodInfo methodInfo = type.GetMethod(methodName); 

        if(methodInfo == null ) return false;   

        System.Reflection.MethodInfo baseDefinition = methodInfo.GetBaseDefinition();

        return methodInfo.DeclaringType != baseDefinition.DeclaringType;
    }

    //private bool IsOverridenAlternative(Type type, string methodName)
    //{
    //    System.Reflection.MethodInfo methodInfo = type.GetMethod(methodName);

    //    if (methodInfo == null) return false;

    //    // IsFinal, IsVirtual, IsAbstract 속성을 함께 확인하면 정확도 상승
    //    // 간단하게 NewSlot이 설정되지 않았는지 확인
    //    return !methodInfo.Attributes.HasFlag(System.Reflection.MethodAttributes.NewSlot);
    //}

    // 패시브 추가 스탯 적용
    public void OnApplyStaticEffect(int jobID, Character owner)
    {
        if (owner == null) return;

        foreach (PassiveSkill skill in EffectivePassives(jobID))
        {
            skill.OnApplyStaticEffect(owner.Status);
        }
    }

    // 패시브 소지 효과 
    public void OnAcquire(int jobID, GameObject ownerObj)
    {
        if (ownerObj == null) return;
        if (activeOwners.TryGetValue(jobID, out var previousOwner) && previousOwner != null && previousOwner != ownerObj)
            OnLose(jobID, previousOwner);
        activeOwners[jobID] = ownerObj;

        foreach (PassiveSkill skill in EffectivePassives(jobID))
        {
            skill.OnAcquire(ownerObj);

            // 해당 스킬이 OnUpdate 메서드를 오버라이드 한 전적이 있는지 확인
            Type skillType = skill.GetType();
            bool needsUpdate = IsOverriden(skillType, "OnUpdate");

            if (needsUpdate)
            {
                updatablePassive.Unique(skill);
            }
        }
    }

    public void OnLose(int jobID, GameObject ownerObj)
    {
        if (ownerObj == null) return;
        if (!activeOwners.TryGetValue(jobID, out var currentOwner) || currentOwner != ownerObj) return;
        
        foreach (PassiveSkill skill in EffectivePassives(jobID))
        {
            skill.OnLose();
            updatablePassive.Remove(skill);
        }

        activeOwners.Remove(jobID);
    }

    public void OnUpdate(float dt)
    {
        foreach (PassiveSkill skill in updatablePassive)
        {
            skill.OnUpdate(dt);
        }
    }

    public void RefreshPartyEffects(List<GameObject> partyMembers)
    {
        foreach (var member in partyMembers)
        {
            StatusComponent status = member.GetComponent<StatusComponent>();
            if (status == null) continue;

            foreach (var record in EffectivePassives(Constants.GLOBAL_RECORD_JOB_ID))
            {
                record.OnApplyStaticEffect(status);
            }
        }
    }

    // 💡 1. 누군가 스킬을 시전했을 때 (Context 조작용)
    public void BroadcastOnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        // 등록된 모든 패시브를 순회하며 OnSkillCast 실행
        foreach (var passive in AllEffectivePassives()) passive.OnSkillCast(evt, context);
    }

    //  2. 누군가 투사체를 생성했을 때 (관통 제거, 분열탄 적용용)
    public void BroadcastOnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        // 액티브 스킬 모듈이 투사체를 만들었다고 알려오면, 모든 패시브에게 전달!
        foreach (var passive in AllEffectivePassives())
            if (passive is GenericPassiveSkill gps) gps.OnSpawnObject(spawnedObject, casterSkill);
    }

    // 어시스트 드론의 일반탄 전용 알림입니다. 레이저 계열 발사체는 이 경로를 호출하지 않습니다.
    public void BroadcastOnAssistDroneNormalProjectile(ISkillEffect spawnedObject, Character owner)
    {
        foreach (var passive in AllEffectivePassives())
            if (passive is GenericPassiveSkill gps) gps.OnAssistDroneNormalProjectile(spawnedObject, owner);
    }

    public void ResetRecordPassives()
    {
        if (!passiveSkillList.TryGetValue(Constants.GLOBAL_RECORD_JOB_ID, out var records)) return;
        foreach (var skill in records) { skill.OnLose(); updatablePassive.Remove(skill); }
        records.Clear();
        activeOwners.Remove(Constants.GLOBAL_RECORD_JOB_ID);
    }

    public void ResetExplorePassives()
    {
        ResetStartingPassives();
        ResetRecordPassives();
    }
}
