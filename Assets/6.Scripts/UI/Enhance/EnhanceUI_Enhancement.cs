using System;
using TMPro;
using UnityEngine;

public class EnhanceUI_Enhancement : MonoBehaviour
{
    public event Action OnEnhanced;

    public void TryEnhnace(EquipmentItem equipment)
    {
        if(equipment == null)
            return;

        if (!EnhanceSystem.CanEnhance(equipment, out string reason))
        {
            Debug.LogWarning(reason);
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(reason);
            return;
        }

        // 강화 처리 
        EnhanceSystem.ApplyEnhance(equipment);
        OnEnhanced?.Invoke();
    }
}
