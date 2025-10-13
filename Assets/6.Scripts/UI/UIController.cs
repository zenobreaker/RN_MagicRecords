using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour, IUIContainer
{
    private Transform popUpParent;
    public Transform PopUpParent => popUpParent;

    [SerializeField] private List<UICurrency> currencies; 

    protected virtual void OnEnable()
    {
        popUpParent = this.transform;
        UIRegistry.Register<IUIContainer>(this);
    }

    public void SetActiveTarget(GameObject target)
    {
        target.SetActive(!target.activeInHierarchy);
    }

    public void UpdateCurrencies()
    {
        foreach(var currency in currencies)
        {
            int v = CurrencyManager.Instance.GetCurrency(currency.Type);
            currency.SetValue(v); 
        }
    }
}
