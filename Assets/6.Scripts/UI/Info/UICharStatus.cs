using UnityEngine;

public class UICharStatus : UiBase
{
    [SerializeField] UIStatRow[] statusRows;


    public void OnDrawCharStatus(CharStatusData status, CharEquipmentData equipment)
    {
        if (status == null) return;

        // 중복되는 코드를 줄이고 번역까지 처리해 주는 내부 헬퍼 함수
        void UpdateStatRow(int rowIndex, StatusType type, string textKey)
        {
            // 1. 최종 스탯 계산
            float finalValue = StatusCalculator.GetFinalStatus(status, equipment, type);

            // 2. 키값을 통해 번역된 이름 가져오기 (예: "stat_health" -> "체력")
            string localizedName = LocalizationManager.Instance.GetText(textKey);

            // 3. 크리티컬 수치는 배율(1.0 = 100%)로 저장하므로 UI에서는 백분율로 변환합니다.
            string valueStr;
            if (type == StatusType.CRIT_RATIO || type == StatusType.CRIT_DMG)
            {
                valueStr = (finalValue * 100f).ToString("F0") + "%";
            }
            else
            {
                valueStr = finalValue.ToString("F0");
            }

            // 4. UI 갱신
            statusRows[rowIndex].SetStat(localizedName, valueStr);
        }

        // 위 함수를 이용해 인덱스, 스탯 타입, 번역 키값만 딱딱 넣어줍니다.
        UpdateStatRow(0, StatusType.HEALTH, "stat_health");
        UpdateStatRow(1, StatusType.ATTACK, "stat_attack");
        UpdateStatRow(2, StatusType.DEFENSE, "stat_defense");
        UpdateStatRow(3, StatusType.ATTACKSPEED, "stat_attack_speed");
        UpdateStatRow(4, StatusType.MOVESPEED, "stat_move_speed");
        UpdateStatRow(5, StatusType.CRIT_RATIO, "stat_crit_ratio");
        UpdateStatRow(6, StatusType.CRIT_DMG, "stat_crit_dmg");
    }
}
