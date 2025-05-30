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

    // 스킬 이미지 그리기
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

    // 고민 사항 => 스킬 쿨타임 값이 다 돌면 어떻게 처리하게 할까?
    // 1. 핸들러에게 그러한 정보까지 맡아놓는다.
    // 2. 여기서 따로 처리한다. 스킬 값으로 
    // 스킬 쿨타임 감소
    private void OnSkillCoolDown(float cooldown, float maxCooldown)
    {
        currCooldown = cooldown;
        img_Cooldown.fillAmount = currCooldown / maxCooldown;

        string currentValue = currCooldown > 1 ? currCooldown.ToString("f0") : currCooldown.ToString("f1");
        txt_Cooldown.text = currentValue;
    }

    // 스킬이 쿨타임 중인지 아닌지에 따른 동작 
    private void OnIsCooldown(bool isCooldown)
    {
        if (isCooldown == false)
            currCooldown = 0;

        img_Cooldown.gameObject.SetActive(isCooldown);
        txt_Cooldown.gameObject.SetActive(isCooldown);
    }

}
