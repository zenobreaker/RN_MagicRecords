using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class GaugeUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Image img_ForegroundGauge;
    [SerializeField] private Image img_BackgroundGauge;
    [SerializeField] private TextMeshProUGUI txt_GaugeText;

    [Header("HUD Handler")]
    [SerializeField] private SO_HUDHandler handler;
    private readonly string path = "SO_HUDHandler";

    protected abstract void SetHUDHandler(SO_HUDHandler handler);

    protected virtual void Awake()
    {
        if(handler == null)
            handler = Resources.Load<SO_HUDHandler>(path);

        SetHUDHandler(handler);
    }

    protected void OnDrawInitGauge(float value)
    {
        if (img_ForegroundGauge != null)
        {
            img_ForegroundGauge.fillAmount = 1.0f;
        }

        if (txt_GaugeText != null)
        {
            txt_GaugeText.text = value.ToString("f0") + "/" + value.ToString("f0");
        }
    }

    protected void OnDrawGauge(float gaugeValue)
    {
        if (img_ForegroundGauge != null)
        {
            img_ForegroundGauge.fillAmount = 1.0f; 
        }

        if(txt_GaugeText != null)
        {
            txt_GaugeText.text = gaugeValue.ToString("f0");
        }
    }

    protected void OnDrawGauge(float currentValue, float maxValue)
    {
        if (img_ForegroundGauge != null)
        {
            img_ForegroundGauge.fillAmount = currentValue / maxValue;
        }

        if (txt_GaugeText != null)
        {
            txt_GaugeText.text = currentValue.ToString("f0") + "/" + maxValue.ToString("f0");
        }
    }
}
