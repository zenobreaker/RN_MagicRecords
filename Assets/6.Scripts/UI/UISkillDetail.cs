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

        selectedSkillData.currentLevel = selectedSkillData.GetMaxSkillLevel();
        DrawSkillLevel(selectedSkillData);
    }

    public void OnDownSkillLevel()
    {
        if (selectedSkillData == null) return;

        selectedSkillData.currentLevel = Mathf.Clamp(
            selectedSkillData.currentLevel-1, 0, selectedSkillData.currentLevel);
        DrawSkillLevel(selectedSkillData);
    }

    public void OnUpSkillLevel()
    {
        if (selectedSkillData == null) return;

        selectedSkillData.currentLevel = Mathf.Clamp(
            selectedSkillData.currentLevel + 1, 0, selectedSkillData.GetMaxSkillLevel());
        DrawSkillLevel(selectedSkillData);
    }

    public void OnEquipSkill()
    {
        if (selectedSkillData == null) return;

        OnDrawEquipUI?.Invoke();
    }
}
