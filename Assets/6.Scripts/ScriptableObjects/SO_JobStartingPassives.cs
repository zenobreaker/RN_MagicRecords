using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class JobStartingPassiveSet
{
    public int jobID;
    public SO_PassiveSkillData firstPassive;
    public SO_PassiveSkillData secondPassive;
}

[CreateAssetMenu(fileName = "JobStartingPassives", menuName = "Scriptable Objects/Job Starting Passives")]
public sealed class SO_JobStartingPassives : ScriptableObject
{
    public List<JobStartingPassiveSet> jobs = new();
    [Tooltip("기본 지급 구성을 변경해도 이전 탐사 저장을 복원할 수 있도록 이전 스킬 에셋을 유지하세요.")]
    public List<SO_PassiveSkillData> passiveCatalog = new();

    public bool TryGetDefaults(int jobID, out List<ExplorePassiveSaveData> result, out string error)
    {
        result = new();
        error = null;
        JobStartingPassiveSet found = null;
        foreach (var entry in jobs ?? new())
        {
            if (entry == null || entry.jobID != jobID) continue;
            if (found != null) { error = $"직업 {jobID}의 기본 패시브 설정이 중복되었습니다."; return false; }
            found = entry;
        }
        if (found?.firstPassive == null || found.secondPassive == null)
        { error = $"직업 {jobID}의 기본 패시브 2종을 지정해주세요."; return false; }
        if (found.firstPassive.id <= 0 || found.secondPassive.id <= 0 || found.firstPassive.id == found.secondPassive.id)
        { error = "기본 패시브는 서로 다른 양수 스킬 ID를 사용해야 합니다."; return false; }
        result.Add(new ExplorePassiveSaveData { jobID = jobID, skillID = found.firstPassive.id, level = 1 });
        result.Add(new ExplorePassiveSaveData { jobID = jobID, skillID = found.secondPassive.id, level = 1 });
        return true;
    }

    public bool TryResolve(int skillID, out SO_PassiveSkillData result, out bool ambiguous)
    {
        result = null;
        ambiguous = false;
        foreach (var candidate in GetTemplates())
        {
            if (candidate == null || candidate.id != skillID) continue;
            if (result != null && result != candidate) { result = null; ambiguous = true; return false; }
            result = candidate;
        }
        return result != null;
    }

    private IEnumerable<SO_PassiveSkillData> GetTemplates()
    {
        foreach (var entry in jobs ?? new())
        {
            if (entry == null) continue;
            yield return entry.firstPassive;
            yield return entry.secondPassive;
        }
        foreach (var template in passiveCatalog ?? new()) yield return template;
    }
}
