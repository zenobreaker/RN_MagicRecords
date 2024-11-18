using UnityEngine;

public class MPGaugeUI : GaugeUI
{
    protected override void SetHUDHandler(SO_HUDHandler handler)
    {
        if (handler != null)
        {
            handler.OnInitMP += OnDrawInitGauge;
            handler.OnChangeMP += OnDrawGauge;
            handler.OnChangeMP_TwoParam += OnDrawGauge;
        }
    }
}
