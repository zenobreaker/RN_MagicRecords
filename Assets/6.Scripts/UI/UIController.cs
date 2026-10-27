using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour, IUIContainer
{
    private Transform popUpParent;
    public Transform PopUpParent
    {
        get
        {
            return popUpParent;
        }
    }

    [SerializeField] private List<UICurrency> currencies;

    protected bool bIsAwaked = false;

    protected virtual void OnEnable()
    {
        popUpParent = this.transform;
        UIRegistry.Register<IUIContainer>(this);

        // Use RegisterManagerEvent helper to auto manage subscription lifecycle
        ManagerWaiter.RegisterManagerEvent<CurrencyManager>(this,
            onRegister: manager =>
            {
                manager.OnUpdatedCurrency += UpdateCurrencies;
                UpdateCurrencies();
            },
            onUnregister: manager =>
            {
                manager.OnUpdatedCurrency -= UpdateCurrencies;
            });
    }

    public void SetActiveTarget(GameObject target)
    {
        target.SetActive(target.activeInHierarchy == false);
    }

    public void UpdateCurrencies()
    {
        if (CurrencyManager.Instance == null)
        {
            return;
        }

        foreach (var currency in currencies)
        {
            int v = CurrencyManager.Instance.GetCurrency(currency.Type);
            currency.SetValue(v);
        }
    }
}
