using System;
using System.Collections.Generic;
using UnityEngine;


public class EventExecutionResult
{
    public bool IsSuccess;

    public EventChoice Choice;

    public bool NeedCombat;
}


public static class EventChoiceExecutor
{
    public static EventExecutionResult Execute(EventChoice choice)
    {
        if (!EventCostChecker.CanAfford(choice))
            return null;

        EventCostProcessor.PayCost(choice, null);

        bool isSuccess =
            UnityEngine.Random.Range(0, 100)
            < choice.Probability;

        return new EventExecutionResult
        {
            IsSuccess = isSuccess,
            Choice = choice,
            NeedCombat =
                isSuccess &&
                choice.ActionType == EventActionType.STAGE_COMBAT
        };
    }
}
public static class EventCostChecker
{
    public static bool CanAfford(EventChoice choice)
    {
        switch (choice.CostType)
        {
            case EventCostType.NONE:
                return true;

            case EventCostType.CURRENCY:
                CurrencyType ct = (choice.CostParam == EventCostParam.EXPLORE_COIN)
                                  ? CurrencyType.EXPOLORE_GOLD : CurrencyType.GOLD;
                return CurrencyManager.Instance.GetCurrency(ct) >= choice.CostValue;

            case EventCostType.RECORD:
                var rm = AppManager.Instance.GetRecordManager();
                if (rm == null) return false;

                if (choice.CostParam == EventCostParam.SAVED)
                    return rm.GetTransferedRecordIDs().Count >= choice.CostValue;
                else
                    return rm.GetPossesRecord().Count >= choice.CostValue;

            case EventCostType.HP:
                // 체력 검사 로직
                return true;

            case EventCostType.ITEM:
                // 아이템 검사 로직
                return true;

            default:
                return true;
        }
    }
}

public static class EventCostProcessor
{
    public static void PayCost(EventChoice choice, Action onCostPaid)
    {
        switch (choice.CostType)
        {
            case EventCostType.NONE:
                onCostPaid?.Invoke();
                break;

            case EventCostType.CURRENCY:
                CurrencyType ct = (choice.CostParam == EventCostParam.EXPLORE_COIN)
                                  ? CurrencyType.EXPOLORE_GOLD : CurrencyType.GOLD;
                CurrencyManager.Instance.SpendCurrency(ct, choice.CostValue);
                onCostPaid?.Invoke(); // 즉시 지불 완료
                break;

            case EventCostType.RECORD:
                var rm = AppManager.Instance.GetRecordManager();
                if (rm == null) return;

                // 💡 레코드를 비용으로 쓸 때는 팝업을 열어 유저가 선택하게 만듭니다.
                var records = choice.CostParam == EventCostParam.SAVED ? rm.GetTransferedRecordIDs() : rm.GetPossesRecord();

                // 1회용 이벤트 구독으로, 레코드를 버린 직후(OnCostPaidSuccess)에 콜백(onCostPaid)을 실행하게 연결합니다.
                Action costPaidCallback = null;
                costPaidCallback = () =>
                {
                    rm.OnCostPaidSuccess -= costPaidCallback;
                    onCostPaid?.Invoke();
                };

                rm.OnCostPaidSuccess += costPaidCallback;
                UIManager.Instance.OpenRecordSelectPopUp(records, false, RecordUIMode.DELETE);
                break;

            case EventCostType.HP:
                // var player = AppManager.Instance.GetPlayerManager();
                // player?.Damage(choice.CostValue);
                onCostPaid?.Invoke();
                break;

            default:
                onCostPaid?.Invoke();
                break;
        }
    }
}


public static class EventActionProcessor
{
    // 💡 파라미터로 EventChoice 전체를 받도록 변경합니다.
    public static void Execute(EventChoice choice)
    {
        if (choice == null || choice.ActionType == EventActionType.NONE) return;

        switch (choice.ActionType)
        {
            case EventActionType.STAGE_COMBAT: // (또는 ELITE_COMBAT / BOSS_COMBAT)
                Debug.Log("전투 스테이지로 전환합니다!");
                // 💡 ExploreManager에게 choice 전체를 넘겨서 승리 보상을 기억하게 합니다.
                AppManager.Instance.GetExploreManager().StartEventCombat(choice);
                break;

            case EventActionType.RECORD_DRAFT:
                Debug.Log("레코드 드래프트(선택) 창을 엽니다.");
                var rm = AppManager.Instance.GetRecordManager();
                rm?.GenerateEventRecords(choice.ActionValue > 0 ? choice.ActionValue : 3, false);
                break;

            case EventActionType.RECORD_SKILL_UP:
                Debug.Log("스킬 강화 창을 엽니다.");
                break;

            case EventActionType.ARCHIVE_SAVE:
                Debug.Log("현재 소지한 레코드를 다음 회차로 보관(Save)하는 창을 엽니다.");
                // UIManager.Instance.OpenRecordSelectPopUp(rm.GetPossesRecord(), false, RecordUIMode.SELECT_OWNED);
                break;

            case EventActionType.ARCHIVE_LOAD:
                Debug.Log("이전 회차에서 보관했던 레코드를 꺼내오는(Load) 창을 엽니다.");
                // UIManager.Instance.OpenRecordSelectPopUp(rm.GetTransferedRecordIDs(), false, RecordUIMode.SELECT_SAVED);
                break;

            case EventActionType.APPLY_BUFF:
                Debug.Log($"버프 적용: {choice.ActionParam} / 수치: {choice.ActionParam}");
                break;

            case EventActionType.APPLY_DEBUFF:
                Debug.Log($"디버프 적용: {choice.ActionParam} / 수치: {choice.ActionParam}");
                break;
        }
    }
}


public static class EventRewardProcessor
{
    // 💡 참고: RewardParam은 JSON에서 string으로 넘어오므로 매개변수를 string으로 받습니다.
    public static void GiveReward(EventChoice choice)
    {
        if (choice == null || choice.RewardType == EventRewardType.NONE) return;

        switch (choice.RewardType)
        {
            case EventRewardType.HP_HEAL:
                Debug.Log($"체력 회복: {choice.ActionParam}");
                // PlayerManager.Instance.Heal(rewardValue);
                break;

            case EventRewardType.HP_DAMAGE:
                Debug.Log($"체력 감소 패널티: {choice.ActionParam}");
                // PlayerManager.Instance.Damage(rewardValue);
                break;

            case EventRewardType.RECORD:
                GiveRecordReward(choice.RewardParam, choice.RewardValue);
                break;

            case EventRewardType.ITEM:
                Debug.Log($"아이템 지급: {choice.ActionParam} / 개수: {choice.ActionParam}");
                break;

            case EventRewardType.BUFF:
                Debug.Log($"보상 버프 지급: {choice.ActionParam}");
                break;
        }
    }

    private static void GiveRecordReward(string param, int value)
    {
        var rm = AppManager.Instance.GetRecordManager();
        if (rm == null) return;

        if (string.IsNullOrEmpty(param) || param == "NONE" || param == "RANDOM")
        {
            // 특정 등급 명시가 없으면 기본 랜덤 드롭
            rm.GenerateEventRecords(value > 0 ? value : 3, false);
        }
        else if (Enum.TryParse(param, out RecordRarity targetRarity))
        {
            // "RARE", "LEGEND" 등의 문자열이 들어오면 해당 등급 레코드 창 띄우기
            List<RecordData> records = new List<RecordData>();
            switch (targetRarity)
            {
                case RecordRarity.NORMAL: records = rm.GetNormalRecordDatas(); break;
                case RecordRarity.RARE: records = rm.GetRareRecordDatas(); break;
                case RecordRarity.LEGENDARY: records = rm.GetLengdaryRecordDatas(); break;
            }

            //TODO : 레코드 보상이라면 리워드 매니저에게 처리해달라고 해야할듯
            if (records.Count == 0) records.Add(rm.GetEmptyRecord());
            UIManager.Instance.OpenRecordSelectPopUp(records, false, RecordUIMode.DRAFT);
        }
    }
}