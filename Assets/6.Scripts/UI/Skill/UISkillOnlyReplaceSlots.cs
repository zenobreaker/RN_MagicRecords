using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 오로직 배치용으로만 사용하는 스킬 슬롯 그룹 
public class UISkillOnlyReplaceSlots : MonoBehaviour
{
    [SerializeField] private Button[] slots;
    [SerializeField] private Sprite emptySlotSprite;

    private SkillTreeManager stManager;
    public Action<int> ClickedSlot;

    private void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            slots[i].onClick.AddListener( (()=>OnClickSlot(index)));
        }   
    }

    public void SetSkillTreeManager(SkillTreeManager stManager)
    {
        this.stManager = stManager;
    }

    private void OnClickSlot(int index)
    {
        ClickedSlot?.Invoke(index);
        ReplaceSlot(index);
    }

    public void DrawSlots(int charid)
    {
        // 매니저로부터 선택한 대상의 스킬 리스트를 가져와서 그림 

        List<SkillRuntimeData> list = AppManager.Instance.GetEquippedActiveSkillListByCharID(charid);

        for (int i = 0; i < list.Count; i++)
        {
            DrawSkillIcon(i, list[i]);
        }
    }

    public void ReplaceSlot(int slotIndex)
    {
        if (stManager == null) return;

        SkillRuntimeData srd = stManager.SelectedSkillData; 
        int charid = 1; 

        AppManager.Instance.EquipActiveSkill(charid, slotIndex, srd);
        DrawSlots(charid);
    }

    private void DrawSkillIcon(int slot, SkillRuntimeData data)
    {
        if(slots[slot].TryGetComponent<Image>(out var icon))
        {
            if (data != null)
                icon.sprite = data.template.skillImage;
            else
                icon.sprite = emptySlotSprite;
        }
    }
}
