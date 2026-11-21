using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EffectIconUI : MonoBehaviour
{
    [SerializeField] private Image effectImage;
    [SerializeField] private Image cooldownImage;
    [SerializeField] private TextMeshProUGUI stackCount;
    [SerializeField] private TextMeshProUGUI remainingTime;

    private BaseEffect baseEffect;

    public void OnApply(BaseEffect effect)
    {
        baseEffect = effect;

        if (effectImage != null)
        {
            effectImage.sprite = baseEffect.FxIcon;
        }

        RefreshDisplay();
    }

    private void Update()
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (baseEffect == null)
        {
            return;
        }

        if (cooldownImage != null)
        {
            cooldownImage.fillAmount = baseEffect.Duration > 0f
            ? Mathf.Clamp01(baseEffect.RemainingTime / baseEffect.Duration) : 0f;
        }

        if (stackCount != null)
        {
            int displayCount = baseEffect.DisplayStackCount;
            stackCount.text = displayCount > 1 || baseEffect.ShowSingleStack
                ? displayCount.ToString() : string.Empty;
        }

        remainingTime.SafeInvoke(text =>
        {
            bool visible = baseEffect.ShowRemainingTime && baseEffect.Duration > 0f;
            text.text = visible ? Mathf.CeilToInt(Mathf.Max(0f, baseEffect.RemainingTime)).ToString() : string.Empty;
            text.gameObject.SetActive(visible);
        });
    }
}
