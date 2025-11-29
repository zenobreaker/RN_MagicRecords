using System.Collections.Generic;
using UnityEngine;

public class EffectGroupUI : MonoBehaviour
{
    [Header("HUD Handler")]
    [SerializeField] private SO_HUDHandler handler;
    private readonly string path = "SO_HUDHandler";

    [Header("Effect Icon")]
    [SerializeField] private EffectIconUI effectIconUI;

    private Dictionary<string, EffectIconUI> icons = new();

    private void Awake()
    {

        if (handler == null)
            handler = Resources.Load<SO_HUDHandler>(path);

        SetHUDHandler(handler);
    }

    protected void SetHUDHandler(SO_HUDHandler handler)
    {
        if (handler == null) return;

        handler.OnEffect += OnEffect;
    }

    protected virtual void OnEffect(BaseEffect baseEffect)
    {
        if (baseEffect.FxIcon == null)
            return; 

        if (icons.ContainsKey(baseEffect.ID) == false)
        {
            EffectIconUI icon = Instantiate<EffectIconUI>(effectIconUI, this.transform);
            if (icon == null) return;

            icon.gameObject.SetActive(false);
            icon.OnApply(baseEffect);

            // 삭제 이벤트 등록
            baseEffect.OnRemovedUI += RemoveIcon;

            icons.Add(baseEffect.ID, icon);
            icon.gameObject.SetActive(true);
        }
        else
        {
            icons[baseEffect.ID].OnApply(baseEffect);
        }
    }

    public void RemoveIcon(BaseEffect baseEffect)
    {
        if(!icons.ContainsKey(baseEffect.ID)) return;

        Destroy(icons[baseEffect.ID].gameObject);
        icons.Remove(baseEffect.ID);
    }
}
