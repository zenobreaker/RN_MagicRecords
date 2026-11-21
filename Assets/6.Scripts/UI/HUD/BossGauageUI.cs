using TMPro;
using UnityEngine;

public class BossGauageUI : GaugeUI
{
    [SerializeField] private TextMeshProUGUI bossNameTxt;
    protected override void SetHUDHandler(SO_HUDHandler handler)
    {
        if (handler == null)
        {
            return;
        }

        handler.OnInitHP += OnDrawInitGauge;
    }

    /// <summary>
    /// 보스의 이름 키를 현재 언어의 표시 이름으로 변환합니다.
    /// </summary>
    public void SetName(string name)
    {
        if (bossNameTxt != null)
        {
            bossNameTxt.text = LocalizationManager.Instance.SafeInvoke(
                localization => localization.GetText(name), name);
        }
    }

    public void OnDrawBossGauge(Character boss, float value1, float value2)
    {
        OnDrawGauge(value1, value2);
    }
}

