using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIRecordSkillUpPopUp : UIPopUp
{
    [Header("Event balance")]
    [Tooltip("Lv.1에서 Lv.2로 성장할 때 사용하는 탐사 재화")]
    [SerializeField, Min(0)] private int baseUpgradeCost = 50;
    [Tooltip("현재 레벨이 1 증가할 때마다 추가되는 비용: 기본 비용 + (현재 레벨 - 1) x 증가 비용")]
    [SerializeField, Min(0)] private int costPerLevel = 25;
    [SerializeField, Min(1)] private int replacementLimit = 1;
    [Header("SkillTreeGroup")]
    [SerializeField] private Transform skillContent;
    [SerializeField] private Button skillButtonTemplate;
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private Button upgradeButton, replaceButton, closeButton;
    [SerializeField] private TMP_Text currencyText, detailText, statusText;
    private readonly List<Button> entries = new();
    private HashSet<int> candidates = new();
    private SkillEventSession session;
    private int selectedSkill, selectedSlot;
    private bool draftMode;
    private bool shopMode;
    private bool slotChosen, skillChosen;
    private string shopFeedback;
    private System.Action shopCommitted;
    private CurrencyManager currencyManager;
    private static bool skipUpgradeConfirmation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionPreference() => skipUpgradeConfirmation = false;

    public static string Text(string key, string fallback)
    {
        string value = LocalizationManager.Instance?.GetText(key);
        return string.IsNullOrEmpty(value) || value == key ? fallback : value;
    }

    public static string SkillEntryText(SkillRuntimeData data, int equippedSlot)
    {
        string label = $"{Text(data.GetSkillName(), data.GetSkillName())}\nLv.{data.currentLevel}";
        if (equippedSlot >= 0)
        {
            string equipped = string.Format(Text("ui_skill_equipped_slot", "장착 중 · {0}번"), equippedSlot + 1);
            label += $"\n<size=80%><color=#8FF0B0>{equipped}</color></size>";
        }
        return label;
    }
    protected override void Awake()
    {
        base.Awake();
        upgradeButton.onClick.AddListener(RequestUpgrade);
        replaceButton.onClick.AddListener(() => { if (shopMode) Unequip(); else Replace(); });
        closeButton.onClick.AddListener(RequestApplyOrClose);
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i;
            slotButtons[i].onClick.AddListener(() => SelectSlot(slot));
        }
    }
    public void SetData(EventChoice choice)
        => InitializeSession(choice, 0, null, null);

    public void SetShopData(EventChoice choice, int price, System.Func<int, bool> pay, System.Action onCommitted)
        => InitializeSession(choice, price, pay, onCommitted);

    private void InitializeSession(EventChoice choice, int price, System.Func<int, bool> pay, System.Action onCommitted)
    {
        shopMode = pay != null;
        shopCommitted = onCommitted;

        var app = AppManager.Instance;
        var setup = app?.GetExploreManager()?.CurrentSetupData;
        var manager = SkillTreeManager.Instance;
        var currency = CurrencyManager.Instance;
        var slots = setup == null ? null : app.GetEquippedActiveSkillListByCharID(setup.SelectedCharacterId);

        if (setup == null || manager == null || currency == null || slots == null)
        {
            UIManager.Instance.SafeInvoke(v => v.ShowToast(Text("ui_skill_event_unavailable", "현재 탐사 스킬 정보를 불러올 수 없습니다.")));
            base.CloseUI(); return;
        }

        int character = setup.SelectedCharacterId;
        if (currencyManager != null)
            currencyManager.OnUpdatedCurrency -= RefreshUI;

        currencyManager = currency;
        currencyManager.OnUpdatedCurrency += RefreshUI;

        var skills = shopMode
            ? SkillManager.Instance.GetRunSkills(character, setup.SelectedClassId)
            : manager.GetAvailableSkills(setup.SelectedClassId);
        session = new SkillEventSession(skills, slots,
            baseUpgradeCost, costPerLevel,
            () => currency.GetCurrency(CurrencyType.EXPOLORE_COIN),
            amount => pay != null ? pay(amount) : currency.SpendCurrency(CurrencyType.EXPOLORE_COIN, amount),
            (slot, data) => app.EquipActiveSkill(character, slot, data));

        if (shopMode)
            session.SetReplacementCost(price);

        draftMode = choice?.ActionParam == EventActionParam.DRAFT_3;
        candidates = session.Skills.Where(s => s.template is SO_ActiveSkillData && !session.Slots.Contains(s.GetSkillID()) &&
            (draftMode || s.isUnlocked || s.currentLevel > 0))
            .OrderBy(_ => Random.value).Take(draftMode ? (choice.ActionValue > 0 ? choice.ActionValue : 3) : int.MaxValue)
            .Select(s => s.GetSkillID()).ToHashSet();

        if (shopMode)
        {
            foreach (int id in candidates)
                session.PrepareCandidate(id);
        }

        selectedSlot = shopMode ? -1 : System.Array.FindIndex(session.Slots, id => id != 0);

        if (!shopMode && selectedSlot < 0)
            selectedSlot = 0;

        selectedSkill = shopMode ? 0 : session.Slots.FirstOrDefault(id => id != 0);
        slotChosen = skillChosen = false;
        shopFeedback = null;

        BuildEntries();
        ShowPopUp();
    }


    private void BuildEntries()
    {
        foreach (var entry in entries) { if (entry != null) Destroy(entry.gameObject); }
        entries.Clear();
        foreach (var data in session.Skills.Where(s => candidates.Contains(s.GetSkillID()) || session.Slots.Contains(s.GetSkillID())).OrderBy(s => s.GetSkillID()))
        {
            int id = data.GetSkillID();
            var button = Instantiate(skillButtonTemplate, skillContent);
            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectSkill(id));
            var icon = button.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = data.template.skillImage;
                icon.enabled = icon.sprite != null;
            }
            entries.Add(button);
            button.name = id.ToString();
        }
    }
    protected override void DrawPopUp() => RefreshUI();
    private void SelectSlot(int slot)
    {
        selectedSlot = slot;
        slotChosen = true;
        shopFeedback = null;
        if (shopMode)
            Replace();
        else
            RefreshUI();
    }

    private void SelectSkill(int id)
    {
        selectedSkill = id;
        skillChosen = true;
        shopFeedback = null;

        if (shopMode)
            Replace();
        else
            RefreshUI();
    }

    public override void RefreshUI()
    {
        if (session == null)
            return;

        currencyText.text = $"{Text("ui_skill_event_currency", "탐사 재화")} : {session.AvailableCurrency}" +
            (session.PendingCost > 0 ? $"  (-{session.PendingCost})" : "");
        
        foreach (var button in entries)
        {
            var data = session.GetSkill(int.Parse(button.name));
            // Preview the current draft loadout, including moves/removals before applying.
            int equippedSlot = System.Array.IndexOf(session.Slots, data.GetSkillID());
            button.GetComponentInChildren<TMP_Text>().text = SkillEntryText(data, equippedSlot);
            button.image.color = data.GetSkillID() == selectedSkill ? new Color(.6f, .9f, 1f) : Color.white;
        }
        
        for (int i = 0; i < slotButtons.Length; i++)
        {
            var data = i < session.Slots.Length ? session.GetSkill(session.Slots[i]) : null;
            slotButtons[i].GetComponentInChildren<TMP_Text>().text = $"{(shopMode && i == selectedSlot ? "선택 " : "")}{i + 1}\n{(data == null ? "-" : Text(data.GetSkillName(), data.GetSkillName()))}";
            slotButtons[i].image.color = i == selectedSlot ? new Color(.6f, .9f, 1f) : Color.white;
            slotButtons[i].interactable = i < session.Slots.Length;
        }
        
        var selected = session.GetSkill(selectedSkill);
        detailText.text = selected == null ? Text("ui_skill_event_select", "스킬을 선택하세요.") :
            $"{Text(selected.GetSkillName(), selected.GetSkillName())}  Lv.{selected.currentLevel} (Max {selected.GetMaxSkillLevel()})\n\n{Text(selected.GetSkillDesc(), selected.GetSkillDesc())}";
        
        upgradeButton.GetComponentInChildren<TMP_Text>().text = $"{Text("ui_skill_event_upgrade", "레벨 업")} ({session.UpgradeCost(selectedSkill)})";
        upgradeButton.gameObject.SetActive(true);
        upgradeButton.interactable = session.CanUpgrade(selectedSkill);

        replaceButton.gameObject.SetActive(true);
        replaceButton.GetComponentInChildren<TMP_Text>().text = shopMode ? "선택 슬롯 스킬 해제" : "선택 슬롯에 교체";
        replaceButton.interactable = shopMode ? selectedSlot >= 0 && selectedSlot < session.Slots.Length && session.Slots[selectedSlot] != 0 :
            selected != null && candidates.Contains(selectedSkill) &&
            selectedSlot >= 0 && selectedSlot < session.Slots.Length && !session.Slots.Contains(selectedSkill) &&
            (!draftMode || (session.Slots[selectedSlot] != 0 && session.ReplacementCount < replacementLimit));
        
        statusText.text = session.HasChanges ? Text("ui_skill_event_pending", "변경사항은 적용 후 닫기를 눌러 확정합니다.") :
            Text("ui_skill_event_hint", "교체할 슬롯과 스킬을 선택하세요. 재화로 스킬을 성장시킬 수 있습니다.");
     
        if (shopMode)
        {
            if (!string.IsNullOrEmpty(shopFeedback)) statusText.text = shopFeedback;
            else if (!skillChosen && !slotChosen)
                statusText.text = "슬롯과 스킬을 고르면 장착·이동됩니다. 선택 슬롯의 스킬 해제도 가능합니다.";
            else if (!skillChosen)
                statusText.text = $"{selectedSlot + 1}번 슬롯 선택됨 · 장착할 스킬을 고르세요.";
            else if (!slotChosen)
                statusText.text = "스킬 선택됨 · 아래 장착할 슬롯을 고르세요. 레벨 업도 가능합니다.";
        }
    }

    private void RequestUpgrade()
    {
        if (session == null || !session.CanUpgrade(selectedSkill)) return;
        int id = selectedSkill; var current = session;
        if (skipUpgradeConfirmation) { session.TryUpgrade(id); RefreshUI(); return; }
        UIManager.Instance.SafeInvoke(v=>v.OpenSkillEventConfirmation(Text("ui_skill_event_upgrade", "레벨 업"),
            string.Format(Text("ui_skill_event_upgrade_confirm", "탐사 재화 {0}을 사용해 레벨을 올리시겠습니까?"), session.UpgradeCost(id)), true,
            skip =>
            {
                if (!isActiveAndEnabled || session != current)
                    return;
                if (session.TryUpgrade(id)) 
                    skipUpgradeConfirmation = skip;
                RefreshUI();
            }));
    }

    private void Replace()
    {
        if (shopMode)
        {
            if (session != null && slotChosen && skillChosen && selectedSlot >= 0 && selectedSlot < session.Slots.Length)
            {
                bool equipped = TryEquipShopSkill();
                if (equipped || session.Slots[selectedSlot] == selectedSkill)
                {
                    slotChosen = skillChosen = false;
                    shopFeedback = $"{selectedSlot + 1}번 슬롯에 장착됨 · 이동·해제 후 ‘적용 후 닫기’로 확정하세요.";
                }
                else shopFeedback = "장착에 필요한 탐사 재화가 부족합니다.";
            }
            RefreshUI();
            return;
        }

        if (session == null || selectedSlot < 0 || selectedSlot >= session.Slots.Length ||
            !candidates.Contains(selectedSkill) || (draftMode && session.ReplacementCount >= replacementLimit)) return;
       
        if (draftMode && session.Slots[selectedSlot] == 0) return;
        session.TryReplace(selectedSlot, selectedSkill, draftMode);
        RefreshUI();
    }

    private bool TryEquipShopSkill() => session.CanRearrangeSkill(selectedSkill)
        ? session.TryEquipOwned(selectedSlot, selectedSkill)
        : candidates.Contains(selectedSkill) && session.TryReplace(selectedSlot, selectedSkill, true);

    private void Unequip()
    {
        if (session == null || !session.TryUnequip(selectedSlot)) return;
        // Consume the prior pair so choosing a slot for removal cannot move a stale skill.
        slotChosen = skillChosen = false;
        shopFeedback = $"{selectedSlot + 1}번 슬롯 해제됨 · ‘적용 후 닫기’로 확정하세요.";
        RefreshUI();
    }

    public override void CloseUI()
    {
        // Esc / backdrop cancels a shop draft without spending or equipping.
        if (shopMode) { base.CloseUI(); return; }
        RequestApplyOrClose();
    }

    private void RequestApplyOrClose()
    {
        if (session == null)
        { 
            base.CloseUI(); 
            return; 
        }

        if (shopMode && !session.HasChanges) 
        {
            base.CloseUI(); return; 
        }

        var current = session;
        UIManager.Instance.SafeInvoke(v=>v.OpenSkillEventConfirmation(Text("ui_skill_event_apply", "적용 후 닫기"),
            Text("ui_skill_event_close_confirm", "변경된 스킬과 재화 사용 내역을 적용하고 닫으시겠습니까?"), false,
            _ =>
            {
                if (!isActiveAndEnabled || session != current) return;
                if (!session.Commit()) { statusText.text = Text("ui_skill_event_commit_failed", "재화 또는 스킬 정보가 변경됐습니다. 다시 확인해주세요."); return; }
                if (session.PendingCost > 0 || session.ReplacementCount > 0) shopCommitted?.Invoke();
                AppManager.Instance?.SaveIfDirty();
                session = null; base.CloseUI();
            }));
    }

    protected override void OnDisable()
    {
        if (currencyManager != null) currencyManager.OnUpdatedCurrency -= RefreshUI;
        currencyManager = null;
        session = null;
        shopCommitted = null;
        shopMode = false;
        base.OnDisable();
    }
}
