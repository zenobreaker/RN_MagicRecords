using UnityEngine;

public class HPGaugeUI : GaugeUI
{
    protected override void SetHUDHandler(SO_HUDHandler handler)
    {
        if(handler != null)
        {
            handler.OnInitHP += OnDrawInitGauge;
            handler.OnChangeHP += OnDrawGauge;
            handler.OnChangeHP_TwoParam += OnDrawGauge;
        }
    }
}
