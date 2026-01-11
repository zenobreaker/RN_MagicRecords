using TMPro;
using UnityEngine;

public class BossGauageUI : GaugeUI
{
    [SerializeField] private TextMeshProUGUI bossNameTxt;
    protected override void SetHUDHandler(SO_HUDHandler handler)
    {
        if (handler == null) return;

        handler.OnInitHP += OnDrawInitGauge;
    }

    public void SetName(string name)
    {
        //TODO: 이름이 있다면 교체하도록 
        if (bossNameTxt != null)
        {
            bossNameTxt.text = name;
        }
    }

    public void OnDrawBossGauge(Character boss, float value1, float value2)
    {
        OnDrawGauge(value1, value2); 
    }
}

