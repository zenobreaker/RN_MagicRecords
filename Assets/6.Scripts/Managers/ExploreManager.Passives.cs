using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class ExplorePassiveSaveData
{
    public int jobID;
    public int skillID;
    public int level = 1;
}

public sealed partial class ExploreManager
{
    [Header("Job Starting Passives")]
    [SerializeField] private SO_JobStartingPassives startingPassiveSettings;
    private List<ExplorePassiveSaveData> startingPassiveState = new();
    private bool startingPassivesInitialized;
    private bool startingPassivesRestored;

    private bool TryGrantStartingPassives(int jobID, out string error)
    {
        error = null;
        if (startingPassiveSettings == null)
        {
            error = "직업별 기본 패시브 설정이 연결되지 않았습니다.";
            return false;
        }
        if (startingPassiveSettings.TryGetDefaults(jobID, out var state, out error) == false)
        {
            return false;
        }

        return TryInstallStartingPassives(jobID, state, out error);
    }

    // 매 스테이지에 호출해도 이미 생성된 런타임 객체/레벨은 그대로 유지합니다.
    public bool EnsureStartingPassives(out string error)
    {
        error = null;
        if (startingPassivesRestored)
        {
            return true;
        }

        if (CurrentSetupData == null || CurrentSetupData.HasClass == false || CurrentSetupData.HasCharacter == false ||
                    (RunStatus != RunStatus.MidRun && RunStatus != RunStatus.ChapterCleared))
        {
            error = "탐사 준비를 먼저 완료해주세요.";
            return false;
        }
        if (startingPassivesInitialized == false)
        {
            return TryGrantStartingPassives(CurrentSetupData.SelectedClassId, out error);
        }

        return TryInstallStartingPassives(CurrentSetupData.SelectedClassId, startingPassiveState, out error);
    }

    private bool TryInstallStartingPassives(int jobID, List<ExplorePassiveSaveData> state, out string error)
    {
        error = null;
        var system = AppManager.Instance.SafeInvoke(app => app.GetPassiveSystem());
        if (system == null || startingPassiveSettings == null)
        {
            error = "탐사 패시브 시스템을 준비하지 못했습니다.";
            return false;
        }
        if (state == null || state.Count < 2)
        {
            error = "탐사 기본 패시브 저장 목록이 올바르지 않습니다.";
            return false;
        }
        var ids = new HashSet<int>();
        var skills = new List<PassiveSkill>();
        foreach (var saved in state)
        {
            if (saved == null || saved.jobID != jobID || saved.skillID <= 0 || saved.level < 1 || ids.Add(saved.skillID) == false)
            {
                error = "탐사 기본 패시브의 직업, ID 또는 레벨이 올바르지 않습니다.";
                return false;
            }
            var template = ResolvePassiveTemplate(saved.skillID, jobID);
            if (template == null)
            {
                error = $"기본 패시브 {saved.skillID}의 에셋이 없거나 ID가 중복되었습니다.";
                return false;
            }
            var skill = template.CreateSkill() as PassiveSkill;
            if (skill == null)
            {
                error = $"패시브 {saved.skillID}를 생성할 수 없습니다.";
                return false;
            }
            skill.SetLevel(Mathf.Clamp(saved.level, 1, Mathf.Max(1, template.maxLevel)));
            skills.Add(skill);
        }
        // 모든 데이터를 검증한 뒤 교체하며 스킬트리 학습 데이터는 수정하지 않습니다.
        system.SetStartingPassives(jobID, skills);
        startingPassiveState = system.CaptureStartingPassives();
        startingPassivesInitialized = true;
        startingPassivesRestored = true;
        return true;
    }

    public SO_PassiveSkillData ResolvePassiveTemplate(int skillID, int jobID = -1)
    {
        if (skillID <= 0)
        {
            return null;
        }

        if (startingPassiveSettings != null)
        {
            if (startingPassiveSettings.TryResolve(skillID, out var template, out var ambiguous))
            {
                return template;
            }

            if (ambiguous)
            {
                return null;
            }
        }
        return SkillTreeManager.Instance.SafeInvoke(manager => manager.GetSkillRuntimeData(
            jobID >= 0 ? jobID : CurrentSetupData.SelectedClassId, skillID))?.template as SO_PassiveSkillData;
    }

    public bool IsJobStartingPassive(int jobID, int skillID)
    {
        var jobs = startingPassiveSettings.SafeInvoke(settings => settings.jobs);
        if (jobs == null)
        {
            return false;
        }

        foreach (var entry in jobs)
        {
            if (entry != null && entry.jobID == jobID &&
            (entry.firstPassive.SafeInvoke(passive => passive.id, -1) == skillID ||
             entry.secondPassive.SafeInvoke(passive => passive.id, -1) == skillID))
            {
                return true;
            }
        }

        return false;
    }

    public bool CanUpgradePassive(SO_PassiveSkillData template)
    {
        if (template == null || RunStatus != RunStatus.MidRun)
        {
            return false;
        }

        var skill = AppManager.Instance.SafeInvoke(app => app.GetPassiveSystem())?.GetRunPassive(CurrentSetupData.SelectedClassId, template.id);
        return (skill?.SkillLevel ?? 0) < Mathf.Max(1, template.maxLevel);
    }

    public bool GrantPassiveRecord(SO_PassiveSkillData template)
    {
        if (EnsureStartingPassives(out var error) == false)
        {
            Debug.LogError(error);
            return false;
        }
        if (CanUpgradePassive(template) == false)
        {
            return false;
        }

        return AppManager.Instance.GetPassiveSystem().GrantRunPassive(CurrentSetupData.SelectedClassId, template);
    }

    private bool RestoreStartingPassives(ExploreRunSaveData save)
    {
        startingPassiveState = save.startingPassives ?? new();
        // 이전 레코드 전용 저주(1200)의 성장을 기본 금단의 저주(12)로 합칩니다.
        foreach (var legacy in startingPassiveState.FindAll(entry => entry != null && entry.skillID == 1200))
        {
            var canonical = startingPassiveState.Find(entry => entry != null && entry.jobID == legacy.jobID && entry.skillID == 12);
            if (canonical == null)
            {
                legacy.skillID = 12;
                legacy.level = Mathf.Clamp(legacy.level, 1, 3);
            }
            else
            {
                canonical.level = Mathf.Clamp(canonical.level + legacy.level, 1, 3);
                startingPassiveState.Remove(legacy);
            }
        }
        startingPassivesInitialized = save.startingPassiveVersion >= 1;
        startingPassivesRestored = false;
        if (RunStatus != RunStatus.MidRun && RunStatus != RunStatus.ChapterCleared)
        {
            return true;
        }

        // 기존 능력치 레코드로 보유했던 에임 부스터도 사수 패시브로 복원합니다.
        var ownedRecords = AppManager.Instance.SafeInvoke(app => app.GetRecordManager())
            .SafeInvoke(manager => manager.GetPossesRecord());
        if (CurrentSetupData != null && CurrentSetupData.SelectedClassId == 1 &&
            ownedRecords != null && ownedRecords.Exists(record => record != null && record.id == 2108) &&
            startingPassiveState.Exists(entry => entry != null && entry.jobID == 1 && entry.skillID == 38) == false)
        {
            startingPassiveState.Add(new ExplorePassiveSaveData
            {
                jobID = 1,
                skillID = 38,
                level = 1
            });
        }

        if (EnsureStartingPassives(out var error))
        {
            return true;
        }

        Debug.LogError(error);
        return false;
    }

    private void ResetStartingPassiveState()
    {
        AppManager.Instance.SafeInvoke(app => app.GetPassiveSystem())?.ResetStartingPassives();
        startingPassiveState.Clear();
        startingPassivesInitialized = false;
        startingPassivesRestored = false;
    }

    private List<ExplorePassiveSaveData> CaptureStartingPassiveState()
    {
        if (startingPassivesRestored)
        {
            startingPassiveState = AppManager.Instance.GetPassiveSystem().CaptureStartingPassives();
        }

        return startingPassiveState.ConvertAll(s => new ExplorePassiveSaveData
        {
            jobID = s.jobID,
            skillID = s.skillID,
            level = s.level
        });
    }
}
