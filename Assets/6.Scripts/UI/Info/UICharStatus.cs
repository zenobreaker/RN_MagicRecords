using UnityEngine;

public class UICharStatus : UiBase
{
    [SerializeField] UIStatRow[] statusRows;


    public void OnDrawCharStatus(CharStatusData status, CharEquipmentData equipment)
    {
        if (status == null) return;

        // 💡 [핵심] 중복되는 코드를 줄이고 번역까지 처리해 주는 내부 헬퍼 함수
        void UpdateStatRow(int rowIndex, StatusType type, string textKey)
        {
            // 1. 최종 스탯 계산
            float finalValue = StatusCalculator.GetFinalStatus(status, equipment, type);

            // 2. 키값을 통해 번역된 이름 가져오기 (예: "stat_health" -> "체력")
            string localizedName = LocalizationManager.Instance.GetText(textKey);

            // 3. 값 포맷팅 (크리티컬 확률 같은 것들은 뒤에 %를 붙여주면 훨씬 보기 좋습니다)
            string valueStr = finalValue.ToString("F0");
            if (type == StatusType.CRIT_RATIO || type == StatusType.CRIT_DMG)
            {
                valueStr += "%";
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
