using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [Tooltip("이 UI 슬롯이 표시할 실제 스킬 슬롯입니다. Default는 표시 대상이 아닙니다.")]
    public SkillSlot mySlot;

    [Header("UI Settings")]
    [SerializeField] private Image img_Skill;
    [SerializeField] private Image img_Cooldown;
    [SerializeField] private TextMeshProUGUI txt_Cooldown;
    [SerializeField] private Sprite emptySlot;

    private SO_SkillEventHandler handler;
    private float currCooldown;

    private void OnDestroy()
    {
        if (handler == null) return;

        handler.OnSetActiveSkill -= OnDrawSkill;
        handler.OnInSkillCooldown -= OnIsCooldown;
        handler.OnSkillCooldown -= OnSkillCoolDown;
    }

    public void SetSkillHandler(SO_SkillEventHandler source)
    {
        if (handler != null)
        {
            handler.OnSetActiveSkill -= OnDrawSkill;
            handler.OnInSkillCooldown -= OnIsCooldown;
            handler.OnSkillCooldown -= OnSkillCoolDown;
        }

        handler = source;
        if (handler == null)
        {
            SetVisible(false);
            return;
        }

        handler.OnSetActiveSkill += OnDrawSkill;
        handler.OnInSkillCooldown += OnIsCooldown;
        handler.OnSkillCooldown += OnSkillCoolDown;

       // RefreshSkillUI();
    }

    private void OnDrawSkill(SkillSlot slot, ActiveSkill activeSkill)
    {
        if (slot != mySlot)
            return;

        if (activeSkill == null)
        {
            img_Skill.sprite = emptySlot;
            return;
        }

        SetVisible(true);
        img_Skill.sprite = activeSkill.Icon;
        OnIsCooldown(mySlot, activeSkill.IsOnCooldown);
    }

    //private void RefreshSkillUI()
    //{
    //    OnDrawSkill(mySlot, registeredSkill);
    //}

    private void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);

        if (!visible && img_Skill != null)
            img_Skill.sprite = emptySlot;
    }

    // 고민 사항 => 스킬 쿨타임 값이 다 돌면 어떻게 처리하게 할까?
    // 1. 핸들러에게 그러한 정보까지 맡아놓는다.
    // 2. 여기서 따로 처리한다. 스킬 값으로 
    // 스킬 쿨타임 감소
    private void OnSkillCoolDown(SkillSlot slot, float cooldown, float maxCooldown)
    {
        if (slot != mySlot) return;

        currCooldown = cooldown;
        img_Cooldown.fillAmount = currCooldown / maxCooldown;

        string currentValue = currCooldown > 1 ? currCooldown.ToString("f0") : currCooldown.ToString("f1");
        txt_Cooldown.text = currentValue;
    }

    // 스킬이 쿨타임 중인지 아닌지에 따른 동작 
    private void OnIsCooldown(SkillSlot slot, bool isCooldown)
    {
        if (slot != mySlot || !gameObject.activeSelf) return;
        if (isCooldown == false)
            currCooldown = 0;

        img_Cooldown.gameObject.SetActive(isCooldown);
        txt_Cooldown.gameObject.SetActive(isCooldown);
    }

}
