using UnityEngine;

public class UICharStatus : UiBase
{
    [SerializeField] UIStatRow[] statusRows;

    public void OnDrawCharStatus(CharStatusData status, CharEquipmentData equipment )
    {
        if (status == null) return;

        float finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.Health);
        statusRows[0].SetStat(StatusType.Health.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.Attack);
        statusRows[1].SetStat(StatusType.Attack.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.Defense);
        statusRows[2].SetStat(StatusType.Defense.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.AttackSpeed);
        statusRows[3].SetStat(StatusType.AttackSpeed.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.MoveSpeed);
        statusRows[4].SetStat(StatusType.MoveSpeed.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.Crit_Ratio);
        statusRows[5].SetStat(StatusType.Crit_Ratio.ToString(), finalValue.ToString("F0"));

        finalValue = StatusCalculator.GetFinalStatus(status, equipment, StatusType.Crit_Dmg);
        statusRows[6].SetStat(StatusType.Crit_Dmg.ToString(), finalValue.ToString("F0"));
    }
}
