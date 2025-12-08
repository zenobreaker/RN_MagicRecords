using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// SkillTree : 스킬 설명 및 강화 등 UI 
public class UISkillDetail : UiBase
{
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillLevelText;
    [SerializeField] private TextMeshProUGUI skillDescText;
    [SerializeField] private Button minimumButton;
    [SerializeField] private Button maximumButton;
    [SerializeField] private Button downButton;
    [SerializeField] private Button upButton;
    [SerializeField] private Button equipButton; 

    private SkillRuntimeData selectedSkillData;

    public event Action<SkillRuntimeData> OnSelectedSkillRunTimeData;
    public event Action OnDrawEquipUI;

    public void HideDetail()
    {
        gameObject.SetActive(false);
    }

    public void OnDrawSkillDetail(SkillRuntimeData data)
    {
        if (data == null) return;
        selectedSkillData = data;
        OnSelectedSkillRunTimeData?.Invoke(data);
        
        gameObject.SetActive(true);

        DrawSkillLevel(data);

        DrawSkillName(data);

        DrawSkillDesc(data);

        if (data.template is SO_PassiveSkillData)
            equipButton?.gameObject.SetActive(false);
        else if(data.template is SO_ActiveSkillData)
            equipButton?.gameObject.SetActive(true);

    }

    private void DrawSkillLevel(SkillRuntimeData data)
    {
        if (data == null || skillLevelText == null) return;

        skillLevelText.text = $"Lv." + data?.currentLevel.ToString();
    }

    private void DrawSkillDesc(SkillRuntimeData data)
    {
        if (data == null || skillDescText == null) return;

        skillDescText.text = data?.GetSkillDesc();
    }

    private void DrawSkillName(SkillRuntimeData data)
    {
        if (data == null || skillNameText == null) return;

        skillNameText.text = data?.GetSkillName();
    }

    public void OnMinimizeSkill()
    {
        if (selectedSkillData == null) return;

        selectedSkillData.currentLevel = 0;

        DrawSkillLevel(selectedSkillData);
    }

    public void OnMaximizeSkill()
    {
        if (selectedSkillData == null) return;

        selectedSkillData.SetMaxSkillLevel();
        DrawSkillLevel(selectedSkillData);
    }

    public void OnDownSkillLevel()
    {
        if (selectedSkillData == null) return;

        selectedSkillData.DecreaseSKillLevel();
        DrawSkillLevel(selectedSkillData);
    }

    public void OnUpSkillLevel()
    {
        if (selectedSkillData == null) return;

        selectedSkillData.IncreaseSkillLevel();
        DrawSkillLevel(selectedSkillData);
    }

    public void OnEquipSkill()
    {
        if (selectedSkillData == null) return;

        if (selectedSkillData.currentLevel == 0 && selectedSkillData.isUnlocked == false)
            return; 

        OnDrawEquipUI?.Invoke();
    }
}
