using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EffectIconUI : MonoBehaviour
{
    [SerializeField] private Image effectImage;
    [SerializeField] private Image cooldownImage;
    [SerializeField] private TextMeshProUGUI stackCount; 

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
        if (baseEffect == null) return;
        if (cooldownImage != null)
            cooldownImage.fillAmount = baseEffect.Duration > 0f
                ? Mathf.Clamp01(baseEffect.RemainingTime / baseEffect.Duration) : 0f;
        if (stackCount != null)
            stackCount.text = baseEffect.StackCount > 1 || baseEffect.ShowSingleStack
                ? baseEffect.StackCount.ToString() : string.Empty;
    }
}
