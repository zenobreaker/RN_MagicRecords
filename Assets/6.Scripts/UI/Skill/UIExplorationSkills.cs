using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Embedded preparation view. Equipment and persistence remain owned by the existing managers.
public sealed class UIExplorationSkills : UiBase
{
    [SerializeField] private Transform skillContent;
    [SerializeField] private Button skillButtonTemplate;
    [SerializeField] private UISkillOnlyReplaceSlots equippedSlots;
    [SerializeField] private Button unequipButton;
    [SerializeField] private TMP_Text skillNameText, detailText, statusText;
    [SerializeField] private ScrollRect skillScroll;

    private readonly List<Button> entries = new();
    private readonly List<SkillRuntimeData> availableSkills = new();
    private ExplorationSetupData context;
    private SkillRuntimeData selectedSkill;
    private int selectedSlot = -1;
    private bool skillChosen, slotChosen;

    public bool IsReady => context != null && context.HasCharacter && context.HasClass &&
        AppManager.Instance?.GetEquippedActiveSkillListByCharID(context.SelectedCharacterId) != null;

    protected override void Awake()
    {
        base.Awake();
        equippedSlots.SetSelectionOnly(true);
        equippedSlots.ClickedSlot += SelectSlot;
        unequipButton.onClick.AddListener(Unequip);
    }

    public void SetContext(ExplorationSetupData setup)
    {
        context = setup;
        selectedSlot = -1;
        selectedSkill = null;
        skillChosen = slotChosen = false;
        availableSkills.Clear();
        if (context != null && SkillTreeManager.Instance != null)
            availableSkills.AddRange(SkillTreeManager.Instance.GetAvailableSkills(context.SelectedClassId)
                .Where(IsLearnedActive).OrderBy(s => s.GetSkillID()));
        BuildEntries();
        var group = GetComponent<CanvasGroup>();
        if (group != null) { group.alpha = 1; group.blocksRaycasts = true; group.interactable = true; }
        RefreshUI();
        Canvas.ForceUpdateCanvases();
        skillScroll.verticalNormalizedPosition = 1;
    }

    private static bool IsLearnedActive(SkillRuntimeData data)
        => data?.template is SO_ActiveSkillData && data.currentLevel >= 1;

    private void BuildEntries()
    {
        while (entries.Count < availableSkills.Count)
        {
            var entry = Instantiate(skillButtonTemplate, skillContent);
            entries.Add(entry);
        }
        for (int i = 0; i < entries.Count; i++)
        {
            var button = entries[i];
            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(i < availableSkills.Count);
            if (i >= availableSkills.Count) continue;
            var data = availableSkills[i];
            button.name = "LearnedSkill_" + data.GetSkillID();
            button.onClick.AddListener(() => SelectSkill(data));
            var icon = button.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null) { icon.sprite = data.template.skillImage; icon.enabled = icon.sprite != null; }
        }
    }

    private static string Local(string key) => UIRecordSkillUpPopUp.Text(key, key);

    private void SelectSlot(int slot)
    {
        selectedSlot = slot;
        slotChosen = true;
        if (!skillChosen)
        {
            var slots = AppManager.Instance?.GetEquippedActiveSkillListByCharID(context.SelectedCharacterId);
            selectedSkill = slots != null && slot >= 0 && slot < slots.Count ? slots[slot] : null;
        }
        TryEquipSelection();
    }

    private void SelectSkill(SkillRuntimeData data)
    {
        selectedSkill = data;
        skillChosen = true;
        TryEquipSelection();
    }

    private void TryEquipSelection()
    {
        if (slotChosen && skillChosen && IsReady && IsLearnedActive(selectedSkill))
        {
            var app = AppManager.Instance;
            var slots = app.GetEquippedActiveSkillListByCharID(context.SelectedCharacterId);
            // Recheck eligibility at the moment of equipping; no draft or currency transaction.
            if (selectedSlot >= 0 && selectedSlot < slots.Count &&
                SkillTreeManager.Instance.GetAvailableSkills(context.SelectedClassId).Contains(selectedSkill))
            {
                if (slots[selectedSlot] != selectedSkill)
                {
                    app.EquipActiveSkill(context.SelectedCharacterId, selectedSlot, selectedSkill);
                    app.SaveIfDirty();
                }
                slotChosen = skillChosen = false;
            }
        }
        RefreshUI();
    }

    private void Unequip()
    {
        if (!IsReady) return;
        var app = AppManager.Instance;
        var slots = app.GetEquippedActiveSkillListByCharID(context.SelectedCharacterId);
        if (selectedSlot < 0 || selectedSlot >= slots.Count || slots[selectedSlot] == null) return;
        app.UnequipActiveSkill(context.SelectedCharacterId, selectedSlot);
        app.SaveIfDirty();
        selectedSkill = null;
        slotChosen = skillChosen = false;
        RefreshUI();
    }

    public override void RefreshUI()
    {
        var slots = IsReady ? AppManager.Instance.GetEquippedActiveSkillListByCharID(context.SelectedCharacterId) : null;
        if (context != null) equippedSlots.DrawSlots(context.SelectedCharacterId);
        equippedSlots.SetSelectedSlot(selectedSlot);
        unequipButton.interactable = slots != null && selectedSlot >= 0 && selectedSlot < slots.Count && slots[selectedSlot] != null;
        skillNameText.text = selectedSkill == null ? "" : Local(selectedSkill.GetSkillName());
        detailText.text = selectedSkill == null ? "스킬을 선택하세요." :
            $"Lv.{selectedSkill.currentLevel}\n\n{Local(selectedSkill.GetSkillDesc())}";
        for (int i = 0; i < availableSkills.Count; i++)
        {
            int equippedSlot = slots?.FindIndex(s => s != null && s.GetSkillID() == availableSkills[i].GetSkillID()) ?? -1;
            entries[i].GetComponentInChildren<TMP_Text>().text = UIRecordSkillUpPopUp.SkillEntryText(availableSkills[i], equippedSlot);
            entries[i].image.color = availableSkills[i] == selectedSkill ? new Color(.6f, .9f, 1f) : Color.white;
        }
        statusText.text = !IsReady ? "캐릭터와 직업을 선택해주세요." : availableSkills.Count == 0 ?
            "배운 액티브 스킬이 없습니다. 스킬 화면에서 레벨 1 이상을 배우세요." :
            skillChosen ? "장착할 슬롯을 선택하세요. 변경 즉시 저장됩니다." :
            slotChosen ? $"{selectedSlot + 1}번 슬롯 선택됨 · 장착할 스킬을 고르세요." :
            "슬롯과 스킬을 고르면 장착·이동됩니다. 변경 즉시 저장됩니다.";
    }
}
