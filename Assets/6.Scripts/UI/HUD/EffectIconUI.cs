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

        if (cooldownImage != null && baseEffect.IsExpired == false)
        {
            cooldownImage.fillAmount = baseEffect.RemainingTime / baseEffect.Duration;
        }

        if (stackCount != null)
        {
            stackCount.text = baseEffect.StackCount == 1 ? "" : baseEffect.StackCount.ToString();
        }
    }

    private void Update()
    {
        if (baseEffect == null) return;

        if(baseEffect.IsExpired == false)
         cooldownImage.fillAmount = baseEffect.RemainingTime / baseEffect.Duration;
    }
}
