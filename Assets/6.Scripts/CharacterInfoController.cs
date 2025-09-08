using System;
using UnityEngine;

public class CharacterInfoController : MonoBehaviour
{
    private UICharEquipment uiCharEquipment;
    private UICharStatus uiCharStatus; 

    private CharStatusData selectedCharStatus; 
    private CharEquipmentData selectedCharEquipment;

    private Action<CharStatusData, CharEquipmentData> DrawnCharInfo; 
    private Action<CharEquipmentData> DrawenCharEquipment;

    private void Awake()
    {
        uiCharEquipment = FindAnyObjectByType<UICharEquipment>();
        if (uiCharEquipment != null)
            DrawenCharEquipment += uiCharEquipment.OnDrawCharEquipment;

        uiCharStatus = FindAnyObjectByType<UICharStatus>();
        if (uiCharStatus != null)
            DrawnCharInfo += uiCharStatus.OnDrawCharStatus;
    }

    private void Start()
    {
        SetCharEquipmentDataData();

        DrawnCharInfo?.Invoke(selectedCharStatus, selectedCharEquipment);
        DrawenCharEquipment?.Invoke(selectedCharEquipment);
    }


    private void SetCharEquipmentDataData()
    {
        if (PlayerManager.Instance == null) return;

        selectedCharStatus = PlayerManager.Instance.GetCharacterStatus(1);
        selectedCharEquipment = PlayerManager.Instance.GetCharEquipmentData(1);
    }
}
