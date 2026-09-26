using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// 로비에서는 스킬을 1레벨로 배우기만 하며, 강화는 탐사 상점에서 처리합니다.
public class UISkillDetail : UiBase
{
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillLevelText;
    [SerializeField] private TextMeshProUGUI skillDescText;
    [FormerlySerializedAs("upButton")]
    [SerializeField] private Button learnButton;
    [SerializeField] private Button equipButton; 

    private SkillRuntimeData selectedSkillData;

    public event Action<SkillRuntimeData> OnSelectedSkillRunTimeData;
    public event Action OnDrawEquipUI;

    protected override void Awake()
    {
        base.Awake();
        learnButton?.onClick.AddListener(OnLearnSkill);
        RefreshLearnButton();
    }

    private void OnDestroy() => learnButton?.onClick.RemoveListener(OnLearnSkill);

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

        DrawSkillAcquisition(data);

        DrawSkillName(data);

        DrawSkillDesc(data);
        RefreshLearnButton();

        if (data.template is SO_PassiveSkillData)
            equipButton?.gameObject.SetActive(false);
        else if(data.template is SO_ActiveSkillData)
            equipButton?.gameObject.SetActive(true);

    }

    private void DrawSkillAcquisition(SkillRuntimeData data)
    {
        if (data == null || skillLevelText == null) return;

        skillLevelText.text = data.currentLevel > 0 ? "습득됨" : "습득 안됨";
    }

    private void DrawSkillDesc(SkillRuntimeData data)
    {
        if (data == null || skillDescText == null) return;
        Debug.Assert(LocalizationManager.Instance != null);

        skillDescText.text = LocalizationManager.Instance.GetText(data?.GetSkillDesc());
    }

    private void DrawSkillName(SkillRuntimeData data)
    {
        if (data == null || skillNameText == null) return;
        Debug.Assert(LocalizationManager.Instance != null); 

        skillNameText.text = LocalizationManager.Instance.GetText(data?.GetSkillName());
    }

    public void OnLearnSkill()
    {
        var data = selectedSkillData;
        if (data?.template == null || data.template.maxLevel < 1 || data.currentLevel > 0) return;
        data.currentLevel = 1;
        data.isUnlocked = true;
        data.OnDataChanged?.Invoke(data);
        SkillTreeManager.Instance?.SaveIfDirty();
        OnDrawSkillDetail(data);
    }

    private void RefreshLearnButton()
    {
        if (learnButton == null) return;
        bool learned = selectedSkillData != null && selectedSkillData.currentLevel > 0;
        learnButton.interactable = selectedSkillData?.template != null &&
            selectedSkillData.template.maxLevel >= 1 && !learned;
        var label = learnButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            string text = learned ? $"ui_text_learn" : $"ui_text_learned";
            label.text  = LocalizationManager.Instance.SafeInvoke(v => v.GetText(text));
        }
    }

    public void OnEquipSkill()
    {
        if (selectedSkillData == null)
        {
            UIManager.Instance.SafeInvoke(v => v.ShowToast($"장착할 스킬을 선택해주세요."));
            return;
        }

        if (selectedSkillData.currentLevel < 1)
        {
            UIManager.Instance.SafeInvoke(v => v.ShowToast($"스킬 레벨이 1이상이어야 장착할 수 있습니다."));
            return;
        }

        OnDrawEquipUI?.Invoke();
    }
}
