using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    public SkillSlot mySlot; 

    [Header("UI Settings")]
    [SerializeField] private Image img_Skill;
    [SerializeField] private Image img_Cooldown;
    [SerializeField] private TextMeshProUGUI txt_Cooldown;


    private float currCooldown;

    public void SetSkillHandler(SO_SkillEventHandler hanlder)
    {
        if (hanlder == null)
            return; 

        hanlder.OnSetActiveSkill += OnDrawSkill;
        hanlder.OnInSkillCooldown += OnIsCooldown;
        hanlder.OnSkillCooldown += OnSkillCoolDown;
    }

    // ��ų �̹��� �׸���
    private void OnDrawSkill(SkillSlot slot, ActiveSkill activeSkill)
    {
        bool bCheck = true;
        bCheck &= mySlot == slot; 
        bCheck &= activeSkill != null;
        if (bCheck == false)
            return;

        img_Skill.sprite = activeSkill.skillSprite;

        OnIsCooldown(slot, activeSkill.IsOnCooldown);
    }

    // ��� ���� => ��ų ��Ÿ�� ���� �� ���� ��� ó���ϰ� �ұ�?
    // 1. �ڵ鷯���� �׷��� �������� �þƳ��´�.
    // 2. ���⼭ ���� ó���Ѵ�. ��ų ������ 
    // ��ų ��Ÿ�� ����
    private void OnSkillCoolDown(SkillSlot slot, float cooldown, float maxCooldown)
    {
        if (slot != mySlot) return;

        currCooldown = cooldown;
        img_Cooldown.fillAmount = currCooldown / maxCooldown;

        string currentValue = currCooldown > 1 ? currCooldown.ToString("f0") : currCooldown.ToString("f1");
        txt_Cooldown.text = currentValue;
    }

    // ��ų�� ��Ÿ�� ������ �ƴ����� ���� ���� 
    private void OnIsCooldown(SkillSlot slot, bool isCooldown)
    {
        if (slot != mySlot) return; 
        if (isCooldown == false )
            currCooldown = 0;

        img_Cooldown.gameObject.SetActive(isCooldown);
        txt_Cooldown.gameObject.SetActive(isCooldown);
    }

}
