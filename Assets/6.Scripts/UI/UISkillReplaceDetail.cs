using System;
using UnityEngine;
using UnityEngine.UI;

public class UISkillReplaceDetail : UiBase
{
    [SerializeField] private GameObject pcSkillReplaceGroup;
    [SerializeField] private GameObject mobileSkillReplaceGroup;

    [SerializeField] private Button unequipButton;
    [SerializeField] private Button closeButton;

    private SkillTreeManager manager;

    private int selectedSlot = -1;

    private void Awake()
    {
        GameObject currentObject = null;
        if (SystemInfo.deviceType == DeviceType.Desktop)
            currentObject = pcSkillReplaceGroup;
        else
            currentObject = mobileSkillReplaceGroup;

        if (currentObject == null) return;

        if (currentObject.TryGetComponent<UISkillOnlyReplaceSlots>(out var slots))
        {
            slots.SetSkillTreeManager(manager);
            slots.ClickedSlot += OnClickedSlot;
        }
    }

    public void SetSkillTreeManager(SkillTreeManager manager)
    {
        this.manager = manager;
    }

    public void HideUI()
    {
        gameObject.SetActive(false);
    }

    public void ShowUI()
    {
        selectedSlot = -1; 
        gameObject.SetActive(true);
        
        DrawUnequipButton();
        
        GameObject currentObject = null;
        if (SystemInfo.deviceType == DeviceType.Desktop)
            currentObject = pcSkillReplaceGroup;
        else
            currentObject = mobileSkillReplaceGroup;

        DrawSlots(currentObject); 
    }

    private void OnClickedSlot(int slot)
    {
        selectedSlot = slot;
        DrawUnequipButton();
    }

    private void DrawSlots(GameObject obj)
    {
        if (obj == null) return;

        if (obj.TryGetComponent<UISkillOnlyReplaceSlots>(out var slots))
        {
            //TODO : id 지정하기
            slots.DrawSlots(1);
        }

        obj.SetActive(true);
    }

    private void DrawUnequipButton()
    {
        if (unequipButton == null) return;

        if (selectedSlot == -1)
            unequipButton.enabled = false;
        else
            unequipButton.enabled = true; 
    }

    public void OnUnequip()
    {
        AppManager.Instance.UnequipActiveSkill(1, selectedSlot);
        ShowUI();
    }
}
