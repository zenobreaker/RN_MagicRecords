using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SkillSlotUI : MonoBehaviour
{
    [Tooltip("이 UI 슬롯이 표시할 실제 스킬 슬롯입니다. Default는 표시 대상이 아닙니다.")]
    [SerializeField] private SkillSlot mySlot;

    [Header("UI Settings")]
    [SerializeField] private Image img_Skill;
    [SerializeField] private Image img_Cooldown;
    [SerializeField] private TextMeshProUGUI txt_Cooldown;
    [SerializeField] private Sprite emptySlot;

    [Header("PC key hint")]
    [SerializeField] private TextMeshProUGUI txt_Key;
    [SerializeField] private InputActionAsset inputActions;

    public void RefreshKeyLabel()
    {
        if (txt_Key == null) return;
        int index = GetSlotIndex();
        txt_Key.text = string.Empty;
        if (index < 0 || inputActions == null) return;
        var action = inputActions.FindAction($"Player/SkillAction{index + 1}", false);
        if (action == null) return;
        foreach (var player in PlayerInput.all)
        {
            var liveAction = player.actions?.FindAction(action.id);
            if (liveAction != null) { action = liveAction; break; }
        }
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (!binding.isComposite && !string.IsNullOrEmpty(binding.effectivePath) &&
                binding.effectivePath.StartsWith("<Keyboard>/", System.StringComparison.OrdinalIgnoreCase))
            {
                txt_Key.text = action.GetBindingDisplayString(i).ToUpperInvariant();
                return;
            }
        }
    }

    private void OnInputActionChange(object source, InputActionChange change)
    {
        if (change == InputActionChange.BoundControlsChanged) RefreshKeyLabel();
    }

    private void OnValidate() => RefreshKeyLabel();

    [Header("Character")]
    [SerializeField] private int characterId = 1;

    private float currCooldown;
    private SkillManager skillManager;
    private SO_SkillEventHandler skillEventHandler;
    private System.IDisposable managerWaitRegistration;

    private void OnEnable()
    {
        InputSystem.onActionChange += OnInputActionChange;
        RefreshKeyLabel();
        BindSkillManager(SkillManager.Instance);

        if (skillManager == null)
        {
            managerWaitRegistration = ManagerWaiter.WaitForManagerDisposable<SkillManager>(
                OnSkillManagerReady);
        }

        RefreshSkillUI();
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= OnInputActionChange;
        managerWaitRegistration?.Dispose();
        managerWaitRegistration = null;
        UnbindSkillManager();
    }

    private void OnSkillManagerReady(SkillManager manager)
    {
        if (!isActiveAndEnabled)
            return;

        BindSkillManager(manager);
        RefreshSkillUI();
    }

    private void BindSkillManager(SkillManager manager)
    {
        if (skillManager == manager)
            return;

        UnbindSkillManager();
        skillManager = manager;

        if (skillManager == null)
        {
            ClearSkill();
            return;
        }

        skillManager.OnDataChanged += RefreshSkillUI;
        skillEventHandler = skillManager.SkillEventHandler;

        if (skillEventHandler == null)
        {
            Debug.LogWarning("[SkillSlotUI] SkillManager에 SO_SkillEventHandler가 연결되지 않았습니다.", this);
            return;
        }

        skillEventHandler.OnSetActiveSkill += OnDrawSkill;
        skillEventHandler.OnInSkillCooldown += OnIsCooldown;
        skillEventHandler.OnSkillCooldown += OnSkillCoolDown;
    }

    private void UnbindSkillManager()
    {
        if (skillManager != null)
            skillManager.OnDataChanged -= RefreshSkillUI;

        if (skillEventHandler != null)
        {
            skillEventHandler.OnSetActiveSkill -= OnDrawSkill;
            skillEventHandler.OnInSkillCooldown -= OnIsCooldown;
            skillEventHandler.OnSkillCooldown -= OnSkillCoolDown;
        }

        skillManager = null;
        skillEventHandler = null;
    }

    /// <summary>
    /// SkillManager의 현재 장착 스킬 정보를 기반으로
    /// 슬롯 UI를 갱신합니다.
    /// </summary>
    private void RefreshSkillUI()
    {
        int slotIndex = GetSlotIndex();

        if (slotIndex < 0)
        {
            ClearSkill();
            return;
        }

        if (skillManager == null)
        {
            ClearSkill();
            return;
        }

        SkillRuntimeData skillData =
            skillManager.GetActiveSkillData(
                characterId,
                slotIndex);

        if (skillData?.template is not SO_ActiveSkillData skillTemplate)
        {
            ClearSkill();
            return;
        }

        // 장착 목록 변경 시에는 SO의 아이콘만 갱신합니다.
        // 실제 쿨다운과 런타임 인스턴스는 SO_SkillEventHandler 이벤트로 갱신됩니다.
        SetSkillIcon(skillTemplate.skillImage);

        // UI가 핸들러 구독보다 늦게 열려도 현재 전투 중인 런타임 스킬 상태를 복원합니다.
        int enumIndex = (int)mySlot;
        if (skillEventHandler != null &&
            enumIndex >= 0 &&
            enumIndex < skillEventHandler.CurrentActiveSkills.Length)
        {
            ActiveSkill activeSkill = skillEventHandler.CurrentActiveSkills[enumIndex];
            if (activeSkill != null)
                OnDrawSkill(mySlot, activeSkill);
        }
    }

    /// <summary>
    /// SkillSlot enum을 실제 List index로 변환합니다.
    /// SLOT1 = 0
    /// SLOT2 = 1
    /// SLOT3 = 2
    /// SLOT4 = 3
    /// </summary>
    private int GetSlotIndex()
    {
        int slotIndex =
            (int)mySlot - (int)SkillSlot.SLOT1;

        if (slotIndex < 0 || slotIndex >= 4)
            return -1;

        return slotIndex;
    }

    private void OnDrawSkill(
        SkillSlot slot,
        ActiveSkill activeSkill)
    {
        if (slot != mySlot)
            return;

        if (activeSkill == null)
        {
            ClearSkill();
            return;
        }

        SetSkillIcon(activeSkill.Icon);

        OnIsCooldown(
            mySlot,
            activeSkill.IsOnCooldown);
    }

    private void SetSkillIcon(Sprite sprite)
    {
        if (img_Skill == null) return;
        img_Skill.sprite = sprite;
        img_Skill.enabled = sprite != null;
    }

    private void ClearSkill()
    {
        SetSkillIcon(emptySlot);

        currCooldown = 0f;

        if (img_Cooldown != null)
        {
            img_Cooldown.fillAmount = 0f;
            img_Cooldown.gameObject.SetActive(false);
        }

        if (txt_Cooldown != null)
        {
            txt_Cooldown.text = string.Empty;
            txt_Cooldown.gameObject.SetActive(false);
        }

    }

    /// <summary>
    /// 스킬 쿨타임 진행 상황을 표시합니다.
    /// SkillEventHandler에서 쿨타임 이벤트를 전달받는 구조라면
    /// 해당 이벤트에서 이 함수를 호출하면 됩니다.
    /// </summary>
    private void OnSkillCoolDown(
        SkillSlot slot,
        float cooldown,
        float maxCooldown)
    {
        if (slot != mySlot)
            return;

        currCooldown = cooldown;

        if (img_Cooldown != null)
        {
            if (maxCooldown > 0f)
                img_Cooldown.fillAmount =
                    currCooldown / maxCooldown;
            else
                img_Cooldown.fillAmount = 0f;
        }

        if (txt_Cooldown != null)
        {
            string currentValue =
                currCooldown > 1f
                    ? currCooldown.ToString("f0")
                    : currCooldown.ToString("f1");

            txt_Cooldown.text = currentValue;
        }
    }

    /// <summary>
    /// 스킬 쿨타임 상태에 따라 UI를 표시합니다.
    /// </summary>
    private void OnIsCooldown(
        SkillSlot slot,
        bool isCooldown)
    {
        if (slot != mySlot)
            return;

        if (!isCooldown)
            currCooldown = 0f;

        if (img_Cooldown != null)
            img_Cooldown.gameObject.SetActive(isCooldown);

        if (txt_Cooldown != null)
            txt_Cooldown.gameObject.SetActive(isCooldown);
    }
}
