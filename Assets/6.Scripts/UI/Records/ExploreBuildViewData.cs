using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// UI 전용 복사본이며 게임 데이터나 저장 데이터로 등록하지 않습니다.
public sealed class BuildEntryViewData
{
    public string Name { get; }
    public string Description { get; }
    public Sprite Icon { get; }
    public string Category { get; }
    public int Level { get; }
    public bool IsJobPassive { get; }

    public BuildEntryViewData(string name, string description, Sprite icon, string category,
        int level = 0, bool isJobPassive = false)
    {
        Name = ExploreBuildViewData.Localize(name);
        Description = ExploreBuildViewData.Localize(description);
        Icon = icon;
        Category = category;
        Level = level;
        IsJobPassive = isJobPassive;
    }

    public string Label => Level > 0 ? $"{Category} · Lv.{Level}" : Category;
}

public static class ExploreBuildViewData
{
    public static string Localize(string value) =>
        LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(value) : value ?? "";

    public static List<BuildEntryViewData> ReadRecords(RecordManager manager = null)
    {
        if (manager == null) manager = AppManager.Instance.SafeInvoke(app => app.GetRecordManager());
        return (manager.SafeInvoke(value => value.GetPossesRecord()) ?? new List<RecordData>())
            .Where(record => record != null)
            .Select(record => new BuildEntryViewData(record.recordName, record.description, record.icon, "획득 레코드"))
            .ToList();
    }

    public static List<BuildEntryViewData> ReadPassives()
    {
        var result = new List<BuildEntryViewData>();
        var app = AppManager.Instance;
        var explore = app.SafeInvoke(value => value.GetExploreManager());
        var setup = explore.SafeInvoke(value => value.CurrentSetupData);
        var system = app.SafeInvoke(value => value.GetPassiveSystem());
        if (setup == null || !setup.HasClass || system == null) return result;
        foreach (var passive in system.GetPassives(setup.SelectedClassId))
        {
            bool starting = explore.IsJobStartingPassive(setup.SelectedClassId, passive.SkillID);
            result.Add(new BuildEntryViewData(
                string.IsNullOrEmpty(passive.Name) ? "패시브 효과" : passive.Name,
                passive.Description, passive.Icon, starting ? "직업 패시브" : "패시브",
                passive.SkillLevel, starting));
        }
        return result;
    }

    public static List<BuildEntryViewData> ReadInventory(RecordManager manager = null)
    {
        var entries = ReadPassives().Where(entry => entry.IsJobPassive).ToList();
        entries.AddRange(ReadRecords(manager));
        return entries;
    }

    // The current run has one participant; the popup only depends on this ordered list.
    public static List<int> ReadCharacterIds()
    {
        var setup = AppManager.Instance.SafeInvoke(app => app.GetExploreManager())
            .SafeInvoke(explore => explore.CurrentSetupData);
        return setup != null && setup.HasCharacter ? new List<int> { setup.SelectedCharacterId } : new List<int>();
    }

    public static string ReadCharacterStatus(int characterId = -1)
    {
        var explore = AppManager.Instance.SafeInvoke(app => app.GetExploreManager());
        var setup = explore.SafeInvoke(value => value.CurrentSetupData);
        if (setup == null || !setup.HasCharacter || !setup.HasClass)
            return "탐사 캐릭터를 선택한 뒤 확인할 수 있습니다.";
        if (characterId < 0) characterId = setup.SelectedCharacterId;

        var players = PlayerManager.Instance;
        var player = players.SafeInvoke(value => value.GetCurrentPlayer(characterId));
        bool inBattle = player != null && player.gameObject.activeInHierarchy && player.gameObject.scene.name == "Stage";
        var status = inBattle ? player.Status : null;
        var growth = players.SafeInvoke(value => value.GetRunCharacterStatus(characterId));
        var text = new StringBuilder();
        text.AppendLine("<size=36>캐릭터 상태</size>");
        text.AppendLine($"캐릭터  {Localize(players.SafeInvoke(value => value.GetCharacterInfo(characterId))?.name)}");
        text.AppendLine($"직업  {Localize(players.SafeInvoke(value => value.GetJobInfo(setup.SelectedClassId))?.jobName)}");
        text.AppendLine($"Level  {growth.SafeInvoke(value => value.level, 1)}");
        text.AppendLine();
        text.AppendLine("<color=#9CD9FF>기본 스탯</color>");
        if (status == null)
        {
            if (explore.TryGetRunHealth(characterId, out float hp, out float maxHP))
                text.AppendLine($"HP  {hp:0} / {maxHP:0}");
            else if (growth != null)
                text.AppendLine($"HP  {growth.GetStatusValue(StatusType.HEALTH):0} / {growth.GetStatusValue(StatusType.HEALTH):0}");
            text.AppendLine("기본·장비 스탯 (전투 효과는 전투 중 반영)");
        }
        else
            text.AppendLine($"HP  {status.GetCurrentHP():0} / {status.GetMaxHP():0}");

        var equipment = players.SafeInvoke(value => value.GetCharEquipmentData(characterId));
        float Stat(StatusType type) => status != null ? status.GetStatusValue(type) :
            growth != null ? StatusCalculator.GetFinalStatus(growth, equipment, type) : 0f;
        text.AppendLine($"공격력  {Stat(StatusType.ATTACK):0}    방어력  {Stat(StatusType.DEFENSE):0}");
        text.AppendLine($"이동속도  {Stat(StatusType.MOVESPEED):0}    공격속도  {Stat(StatusType.ATTACKSPEED):0}");
        text.AppendLine($"치명타 확률  {Stat(StatusType.CRIT_RATIO) * 100f:0}%    치명타 피해  {Stat(StatusType.CRIT_DMG) * 100f:0}%");
        text.AppendLine($"체력 재생  {Stat(StatusType.HEALTH_REGEN):0}");

        text.AppendLine("\n<color=#9CD9FF>장착 액티브 스킬</color>");
        var component = inBattle ? player.GetComponent<SkillComponent>() : null;
        var equipped = component == null
            ? SkillManager.Instance.SafeInvoke(value => value.GetActiveSkillList(characterId)) : null;
        for (int index = 0; index < 4; index++)
        {
            if (component != null)
            {
                bool found = component.TryGetRegisteredActiveSkill((SkillSlot)((int)SkillSlot.SLOT1 + index), out var skill);
                text.AppendLine(found ? $"{index + 1}. {Localize(skill.Name)}  Lv.{skill.SkillLevel}" : $"{index + 1}. 빈 슬롯");
            }
            else
            {
                var skill = equipped != null && index < equipped.Count ? equipped[index] : null;
                text.AppendLine(skill?.template != null ? $"{index + 1}. {Localize(skill.GetSkillName())}  Lv.{skill.currentLevel}" : $"{index + 1}. 빈 슬롯");
            }
        }
        AppendEntries(text, "적용 중 패시브", ReadPassives());
        AppendEntries(text, "획득 레코드", ReadRecords());
        return text.ToString();
    }

    private static void AppendEntries(StringBuilder text, string title, List<BuildEntryViewData> entries)
    {
        text.AppendLine($"\n<color=#9CD9FF>{title}</color>");
        if (entries.Count == 0) text.AppendLine("없음");
        foreach (var entry in entries)
            text.AppendLine($"• {entry.Name}  [{entry.Label}]");
    }
}
