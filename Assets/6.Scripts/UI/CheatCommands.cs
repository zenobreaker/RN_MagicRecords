using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheatCommands
{
    public const string HelpText =
        "heal : 체력 회복    |    kill_all : 현재 적 처치    |    unlock_boss : 보스 노드 해금\n" +
        "learn_all_skills : 전체 스킬 배우기 (로비)\n" +
        "gain_skill 스킬ID 슬롯 [레벨] : 스킬 장착 (인게임, 슬롯 1~4, 기본 Lv.1)\n" +
        "gain_record 레코드ID : 특정 레코드 지급    |    gain_all_records : 미보유 레코드 전체 지급\n" +
        "give_coin [수량] / give_money [수량] : 일반 코인 추가 (기본 10000)\n" +
        "give_explore_coin [수량] : 탐사 코인 추가 (기본 10000)\n" +
        "help [명령어] : 사용법    |    [ ]는 생략 가능한 인자, 입력할 때는 괄호 없이";

    public static string Execute(string command)
    {
        var args = (command ?? "").Trim().ToLowerInvariant().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (args.Length == 0)
        {
            return "명령어를 입력하세요. help로 사용법을 확인할 수 있습니다.";
        }

        string name = CanonicalName(args[0]);
        switch (name)
        {
            case "help":
                if (args.Length == 1)
                {
                    return HelpText;
                }

                return args.Length == 2 ? Usage(CanonicalName(args[1])) : "사용법: help [명령어]";
            case "heal":
                return args.Length == 1 ? Heal() : Usage(name);
            case "kill_all":
                if (args.Length != 1)
                {
                    return Usage(name);
                }

                int count = BattleManager.Instance != null ? BattleManager.Instance.KillCurrentEnemiesForCheat() : 0;
                return count > 0 ? $"현재 필드의 적 {count}명을 처치했습니다." : "현재 필드에 처치할 적이 없습니다.";
            case "unlock_boss":
                return args.Length == 1 ? UnlockBoss() : Usage(name);
            case "learn_all_skills":
                return args.Length == 1 ? LearnAllSkills() : Usage(name);
            case "gain_skill":
                return GainSkill(args);
            case "gain_record":
                return GainRecord(args);
            case "gain_all_records":
                return args.Length == 1 ? GainAllRecords() : Usage(name);
            case "give_coin":
                return GiveCurrency(args, CurrencyType.GOLD);
            case "give_explore_coin":
                return GiveCurrency(args, CurrencyType.EXPOLORE_COIN);
            default:
                return "알 수 없는 명령입니다. help로 명령어를 확인하세요.";
        }
    }

    private static string CanonicalName(string name)
    {
        return name switch
        {
            "1" => "heal",
            "2" => "kill_all",
            "3" => "unlock_boss",
            "killall" => "kill_all",
            "unlockboss" => "unlock_boss",
            "give_money" => "give_coin",
            "도움말" => "help",
            _ => name
        };
    }

    private static string Usage(string name)
    {
        return name switch
        {
            "heal" => "사용법: heal — 살아 있는 캐릭터의 체력을 최대로 회복합니다.",
            "kill_all" => "사용법: kill_all — 현재 필드에 등록된 적을 처치합니다.",
            "unlock_boss" => "사용법: unlock_boss — 현재 탐사 챕터의 마지막 보스 노드를 해금합니다.",
            "learn_all_skills" => "사용법: learn_all_skills — 로비에서 전체 스킬을 최소 1레벨로 배웁니다.",
            "gain_skill" => "사용법: gain_skill 스킬ID 슬롯 [레벨] (인게임, 슬롯 1~4, 기본 1, 최대레벨로 제한)",
            "gain_record" => "사용법: gain_record 레코드ID (예: gain_record 1)",
            "gain_all_records" => "사용법: gain_all_records — 획득 가능한 미보유 레코드를 종류별로 한 번씩 지급합니다.",
            "give_coin" => "사용법: give_coin [수량] 또는 give_money [수량] (기본 10000, 양의 정수)",
            "give_explore_coin" => "사용법: give_explore_coin [수량] (기본 10000, 양의 정수)",
            "help" => "사용법: help [명령어]",
            _ => "알 수 없는 명령입니다. help로 명령어를 확인하세요."
        };
    }

    private static bool PositiveInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result) && result > 0;
    }

    private static Player CurrentPlayer()
    {
        var player = PlayerManager.Instance != null ? PlayerManager.Instance.GetCurrentPlayer() : null;
        return player != null && player.isActiveAndEnabled ? player : UnityEngine.Object.FindAnyObjectByType<Player>();
    }

    private static string Heal()
    {
        var player = CurrentPlayer();
        if (player != null && player.isActiveAndEnabled && player.TryGetComponent<HealthPointComponent>(out var hp))
        {
            if (hp.Dead)
            {
                return "사망한 캐릭터는 회복할 수 없습니다.";
            }

            hp.Heal(hp.GetMaxHP);
            return $"체력 회복 완료: {hp.GetCurrentHP:0}/{hp.GetMaxHP:0}";
        }
        var party = AppManager.Instance != null ? AppManager.Instance.GetExploreManager() : null;
        return party != null && party.HealPartyForCheat() ? "탐사 파티 체력 회복 완료." : "회복할 캐릭터가 없습니다.";
    }

    private static string UnlockBoss()
    {
        var explore = AppManager.Instance != null ? AppManager.Instance.GetExploreManager() : null;
        if (explore == null || explore.RunStatus != RunStatus.MidRun || explore.StageReplacer == null ||
            explore.StageReplacer.UnlockFinalBossForCheat() == false)
        {
            return "진행 중인 탐사에 해금할 보스 노드가 없습니다.";
        }

        foreach (var node in UnityEngine.Object.FindObjectsByType<UIStageMapNode>(FindObjectsSortMode.None))
        {
            if (node.Node != null)
            {
                node.SetState(explore.GetNodeState(node.Node.id));
            }
        }

        return "현재 챕터의 마지막 보스 노드를 해금했습니다.";
    }

    private static string LearnAllSkills()
    {
        if (SceneManager.GetActiveScene().name != "Lobby")
        {
            return "learn_all_skills는 로비에서만 사용할 수 있습니다.";
        }

        var tree = SkillTreeManager.Instance;
        if (tree == null)
        {
            return "스킬 트리가 준비되지 않았습니다.";
        }

        int count = tree.LearnAllSkillsForCheat();
        tree.SaveIfDirty();
        return $"전체 스킬 학습 완료: {count}개 갱신. 이미 배운 스킬의 레벨은 유지합니다.";
    }

    private static string GainSkill(string[] args)
    {
        int level = 1;
        if (args.Length < 3 || args.Length > 4 || PositiveInt(args[1], out int id) == false ||
            PositiveInt(args[2], out int slot) == false || slot > 4 || (args.Length == 4 && PositiveInt(args[3], out level) == false))
        {
            return Usage("gain_skill");
        }

        string scene = SceneManager.GetActiveScene().name;
        if (scene != "Stage" && scene != "UnitTest")
        {
            return "gain_skill은 스테이지 등의 인게임에서만 사용할 수 있습니다.";
        }

        var player = CurrentPlayer();
        if (player == null || player.isActiveAndEnabled == false ||
            (player.TryGetComponent<HealthPointComponent>(out var hp) && hp.Dead))
        {
            return "살아 있는 플레이어가 없습니다.";
        }

        var template = SkillTreeManager.Instance != null ? SkillTreeManager.Instance.FindSkillTemplate(id) : null;
        if (template is not SO_ActiveSkillData active)
        {
            return $"장착 가능한 액티브 스킬 ID가 아닙니다: {id}";
        }

        if (active.maxLevel < 1 || active.levelDatas == null || active.levelDatas.Count == 0)
        {
            return $"스킬의 레벨 데이터가 유효하지 않습니다: {id}";
        }

        var manager = SkillManager.Instance;
        if (manager == null || manager.GainSkillForCheat(player, active, slot - 1, level) == false)
        {
            return "스킬을 장착할 수 없습니다. 캐릭터·직업·스킬 컴포넌트를 확인하세요.";
        }

        AppManager.Instance?.GetExploreManager()?.SaveExploreMap();
        return $"스킬 {id} 장착 완료: {slot}번 슬롯, Lv.{Mathf.Min(level, active.maxLevel)}";
    }

    private static string GainRecord(string[] args)
    {
        if (args.Length != 2 || PositiveInt(args[1], out int id) == false)
        {
            return Usage("gain_record");
        }

        var manager = AppManager.Instance.SafeInvoke(app => app.GetRecordManager());
        if (manager == null)
        {
            return "레코드 관리자가 준비되지 않았습니다.";
        }

        if (manager.GetImplementedRecordIds().Contains(id) == false)
        {
            return $"존재하지 않거나 미구현인 레코드 ID입니다: {id}";
        }

        var granted = manager.GrantRecord(manager.GetShopRecord(id));
        if (granted == null)
        {
            return $"레코드 {id} 지급 실패: 상호 배제, 최대레벨 또는 탐사 상태를 확인하세요.";
        }

        return granted.id == id ? $"레코드 {id} 지급 완료." : $"레코드 {id}는 이미 보유 중이므로 기존 중복 지급 규칙에 따라 레코드 {granted.id}로 지급했습니다.";
    }

    /// <summary>이미 보유한 레코드의 레벨이나 중복 보상을 바꾸지 않고 누락된 종류만 지급합니다.</summary>
    private static string GainAllRecords()
    {
        var manager = AppManager.Instance.SafeInvoke(app => app.GetRecordManager());
        if (manager == null)
        {
            return "레코드 관리자가 준비되지 않았습니다.";
        }

        var ids = manager.GetImplementedRecordIds().OrderBy(id => id).ToList();
        if (ids.Count == 0)
        {
            return "지급할 레코드가 등록되지 않았습니다.";
        }

        var owned = new HashSet<int>(manager.GetPossesRecord().Select(record => record.id));
        var blocked = new List<int>();
        int alreadyOwned = 0;
        int granted = 0;
        foreach (int id in ids)
        {
            if (owned.Contains(id))
            {
                alreadyOwned++;
                continue;
            }

            var record = manager.GetShopRecord(id);
            if (manager.IsRewardCandidate(record) == false || manager.GrantRecord(record)?.id != id)
            {
                blocked.Add(id);
                continue;
            }
            granted++;
        }

        string result = $"레코드 전체 지급: 신규 {granted}개, 이미 보유 {alreadyOwned}개, 지급 불가 {blocked.Count}개.";
        if (blocked.Count > 0)
        {
            result += $"\n지급 불가 ID (상호 배제·탐사 상태·패시브 레벨 확인): {string.Join(", ", blocked)}";
        }
        return result;
    }

    private static string GiveCurrency(string[] args, CurrencyType type)
    {
        int amount = 10000;
        string name = type == CurrencyType.GOLD ? "give_coin" : "give_explore_coin";
        if (args.Length > 2 || (args.Length == 2 && PositiveInt(args[1], out amount) == false))
        {
            return Usage(name);
        }

        var manager = CurrencyManager.Instance;
        if (manager == null || AppManager.Instance == null || AppManager.Instance.GetCurrencyItemByType(type) == null)
        {
            return "재화 데이터가 준비되지 않았습니다.";
        }

        if ((long)manager.GetCurrency(type) + amount > int.MaxValue)
        {
            return "추가 후 재화 수량이 최대 보유 범위(2147483647)를 초과합니다.";
        }

        manager.AddCurrency(type, amount);
        InventoryManager.Instance?.SaveIfDirty();
        return $"{(type == CurrencyType.GOLD ? "일반 코인" : "탐사 코인")} {amount}개 추가. 현재 {manager.GetCurrency(type)}개";
    }
}
