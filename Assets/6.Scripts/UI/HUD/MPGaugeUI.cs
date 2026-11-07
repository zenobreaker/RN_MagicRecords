using UnityEngine;

public class MPGaugeUI : GaugeUI
{
    private SO_HUDHandler subscribedHandler;

    protected override void SetHUDHandler(SO_HUDHandler handler)
    {
        RemoveHUDHandler();
        subscribedHandler = handler;
        if (subscribedHandler != null)
        {
            subscribedHandler.OnInitMP += OnDrawInitGauge;
            subscribedHandler.OnChangeMP += OnDrawGauge;
            subscribedHandler.OnChangeMP_TwoParam += OnDrawGauge;
        }
    }

    private void OnEnable()
    {
        SetHUDHandler(subscribedHandler);
        if (subscribedHandler != null && subscribedHandler.HasManaValue)
        {
            OnDrawGauge(subscribedHandler.CurrentMana, subscribedHandler.MaxMana);
        }
    }

    private void OnDisable()
    {
        RemoveHUDHandler();
    }

    /// <summary>HUD 비활성화와 재연결 시 이전 마나 이벤트 구독을 해제합니다.</summary>
    private void RemoveHUDHandler()
    {
        if (subscribedHandler != null)
        {
            subscribedHandler.OnInitMP -= OnDrawInitGauge;
            subscribedHandler.OnChangeMP -= OnDrawGauge;
            subscribedHandler.OnChangeMP_TwoParam -= OnDrawGauge;
        }
    }
}
