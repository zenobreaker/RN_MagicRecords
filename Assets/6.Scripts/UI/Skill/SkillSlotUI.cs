using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [Tooltip("이 UI 슬롯이 표시할 실제 스킬 슬롯입니다. Default는 표시 대상이 아닙니다.")]
    [SerializeField] private SkillSlot mySlot;

    [Header("UI Settings")]
    [SerializeField] private Image img_Skill;
    [SerializeField] private Image img_Cooldown;
    [SerializeField] private TextMeshProUGUI txt_Cooldown;
    [SerializeField] private Sprite emptySlot;

    [Header("Character")]
    [SerializeField] private int characterId = 1;

    private float currCooldown;

    private void OnEnable()
    {
        if (SkillManager.Instance == null)
            return;

        SkillManager.Instance.OnDataChanged += RefreshSkillUI;

        RefreshSkillUI();
    }

    private void OnDisable()
    {
        if (SkillManager.Instance == null)
            return;

        SkillManager.Instance.OnDataChanged -= RefreshSkillUI;
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

        SkillRuntimeData skillData =
            SkillManager.Instance.GetActiveSkillData(
                characterId,
                slotIndex);

        if (skillData?.template is not SO_ActiveSkillData skillTemplate)
        {
            ClearSkill();
            return;
        }

        Skill skill = skillTemplate.CreateSkill();

        if (skill is not ActiveSkill activeSkill)
        {
            ClearSkill();
            return;
        }

        OnDrawSkill(mySlot, activeSkill);
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

        SetVisible(true);

        if (img_Skill != null)
            img_Skill.sprite = activeSkill.Icon;

        OnIsCooldown(
            mySlot,
            activeSkill.IsOnCooldown);
    }

    private void ClearSkill()
    {
        if (img_Skill != null)
            img_Skill.sprite = emptySlot;

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

        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);

        if (!visible && img_Skill != null)
            img_Skill.sprite = emptySlot;
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