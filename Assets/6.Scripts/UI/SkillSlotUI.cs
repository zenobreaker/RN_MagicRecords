using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
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
        hanlder.OnSkillCooldown_TwoParam += OnSkillCoolDown;
    }

    // ��ų �̹��� �׸���
    private void OnDrawSkill(ActiveSkill activeSkill)
    {
        bool bCheck = true;
        bCheck &= activeSkill != null;
        bCheck &= activeSkill.SO_SkillData != null;

        if (bCheck == false)
            return;

        img_Skill.sprite = activeSkill.SO_SkillData.skillImage;

        OnIsCooldown(activeSkill.IsOnCooldown);
    }

    // ��� ���� => ��ų ��Ÿ�� ���� �� ���� ��� ó���ϰ� �ұ�?
    // 1. �ڵ鷯���� �׷��� �������� �þƳ��´�.
    // 2. ���⼭ ���� ó���Ѵ�. ��ų ������ 
    // ��ų ��Ÿ�� ����
    private void OnSkillCoolDown(float cooldown, float maxCooldown)
    {
        currCooldown = cooldown;
        img_Cooldown.fillAmount = currCooldown / maxCooldown;

        string currentValue = currCooldown > 1 ? currCooldown.ToString("f0") : currCooldown.ToString("f1");
        txt_Cooldown.text = currentValue;
    }

    // ��ų�� ��Ÿ�� ������ �ƴ����� ���� ���� 
    private void OnIsCooldown(bool isCooldown)
    {
        if (isCooldown == false)
            currCooldown = 0;

        img_Cooldown.gameObject.SetActive(isCooldown);
        txt_Cooldown.gameObject.SetActive(isCooldown);
    }

}
