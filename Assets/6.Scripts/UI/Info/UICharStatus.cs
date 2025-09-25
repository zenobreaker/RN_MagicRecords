using UnityEngine;

public class UICharStatus : UiBase
{
    [SerializeField] UIStatRow[] statusRows;

    public void OnDrawCharStatus(CharStatusData status, CharEquipmentData equipment )
    {
        if (status == null) return;

        float finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.HEALTH);
        statusRows[0].SetStat(StatusType.HEALTH.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.ATTACK);
        statusRows[1].SetStat(StatusType.ATTACK.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.DEFENSE);
        statusRows[2].SetStat(StatusType.DEFENSE.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.ATTACKSPEED);
        statusRows[3].SetStat(StatusType.ATTACKSPEED.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.MOVESPEED);
        statusRows[4].SetStat(StatusType.MOVESPEED.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.CRIT_RATIO);
        statusRows[5].SetStat(StatusType.CRIT_RATIO.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.CRIT_DMG);
        statusRows[6].SetStat(StatusType.CRIT_DMG.ToString(), finalValue.ToString("F0"));
    }
}
