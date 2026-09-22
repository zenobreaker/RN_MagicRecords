using System.Collections.Generic;
using UnityEngine;

public class EffectGroupUI : MonoBehaviour
{
    [Header("HUD Handler")]
    [SerializeField] private SO_HUDHandler handler;
    private readonly string path = "SO_HUDHandler";

    [Header("Effect Icon")]
    [SerializeField] private EffectIconUI effectIconUI;

    private readonly Dictionary<string, EffectIconUI> icons = new();
    private readonly Dictionary<string, BaseEffect> displayedEffects = new();
    private SO_HUDHandler subscribedHandler;

    protected virtual void OnEnable()
    {
        if (handler == null)
            handler = Resources.Load<SO_HUDHandler>(path);

        DetachHUDHandler();
        subscribedHandler = handler;
        SetHUDHandler(subscribedHandler);
    }

    protected virtual void OnDisable() => DetachHUDHandler();

    protected virtual void OnDestroy()
    {
        DetachHUDHandler();
        foreach (var effect in displayedEffects.Values)
            effect.OnRemovedUI -= RemoveIcon;
        displayedEffects.Clear();
        foreach (var icon in icons.Values)
            if (icon != null) Destroy(icon.gameObject);
        icons.Clear();
    }

    private void DetachHUDHandler()
    {
        RemoveHUDHandler(subscribedHandler);
        subscribedHandler = null;
    }

    protected virtual void SetHUDHandler(SO_HUDHandler handler)
    {
        if (handler != null) handler.OnEffect += OnEffect;
    }

    protected virtual void RemoveHUDHandler(SO_HUDHandler handler)
    {
        if (handler != null) handler.OnEffect -= OnEffect;
    }

    protected virtual void OnEffect(BaseEffect baseEffect) => UpdateEffectIcon(baseEffect);

    protected void UpdateEffectIcon(BaseEffect baseEffect)
    {
        if (this == null || !isActiveAndEnabled || baseEffect == null ||
            baseEffect.FxIcon == null || effectIconUI == null)
            return;

        if (!icons.TryGetValue(baseEffect.ID, out var icon) || icon == null)
        {
            icon = Instantiate(effectIconUI, transform);
            if (icon == null) return;
            icon.gameObject.SetActive(false);
            icon.OnApply(baseEffect);
            icons[baseEffect.ID] = icon;
            icon.gameObject.SetActive(true);
        }
        else
        {
            icon.OnApply(baseEffect);
        }

        // An ID can be reused by a new effect; its old instance must not remove the new icon.
        if (displayedEffects.TryGetValue(baseEffect.ID, out var previous))
            previous.OnRemovedUI -= RemoveIcon;
        displayedEffects[baseEffect.ID] = baseEffect;
        baseEffect.OnRemovedUI += RemoveIcon;
    }

    protected void RemoveIcon(BaseEffect baseEffect)
    {
        if (baseEffect == null) return;
        baseEffect.OnRemovedUI -= RemoveIcon;
        if (!displayedEffects.TryGetValue(baseEffect.ID, out var current) ||
            !ReferenceEquals(current, baseEffect)) return;
        displayedEffects.Remove(baseEffect.ID);

        // Track removal while hidden too, so expired icons cannot return on enable.
        if (icons.TryGetValue(baseEffect.ID, out var icon) && icon != null)
            Destroy(icon.gameObject);
        icons.Remove(baseEffect.ID);
    }
}
