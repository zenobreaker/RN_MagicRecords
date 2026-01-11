using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class DamageMotionData
{
    public DamageType damageType;
    public AnimationClip damageMotion;
}

[System.Serializable]
public class DamageMotionNameData
{
    public DamageType damageType;
    public string DanameAnimStateSubfixName; 
}

public class DamageHandleComponent : MonoBehaviour
{
    public Action OnDamaged;
    
    [SerializeField]
    private List<DamageMotionData> damageMotions = new List<DamageMotionData>();
    private Dictionary<DamageType, List<DamageMotionData>> damageMotionTable;

    [SerializeField]
    private float dmgFontOffsetY = 1.5f;

    [SerializeField]
    private string DamageAnimStateName = "Hit";

    [Header("Base Controller")]
    [SerializeField] protected RuntimeAnimatorController baseController;


    private Character character; 
    private Animator animator;
    private AnimatorOverrideController overrideController;
    private HealthPointComponent health;
    private StatusComponent status;
   
    private AnimatorOverrideController overridecontroller;

    private void Awake()
    {
        character = GetComponent<Character>();
        animator = GetComponent<Animator>();
        health = GetComponent<HealthPointComponent>();
        status = GetComponent<StatusComponent>();

        Awake_SetDamageMotionData();

        Debug.Assert(animator != null);
        if (animator != null && baseController != null)
        {
            overridecontroller = new AnimatorOverrideController(baseController);
            animator.runtimeAnimatorController = overridecontroller;
        }
    }

    private void Awake_SetDamageMotionData()
    {
        damageMotionTable = new Dictionary<DamageType, List<DamageMotionData>>();

        for (int i = 0; i < (int)DamageType.MAX; i++)
        {
            damageMotionTable.Add((DamageType)i, new List<DamageMotionData>());
        }

        foreach (DamageMotionData data in damageMotions)
        {
            damageMotionTable[data.damageType].Add(data);
        }
    }

    public void OnDamage(GameObject attacker, DamageEvent damageEvent)
    {
        OnDamaged?.Invoke();

        // 이벤트 콜 
        BattleManager.Instance?.NotifyAttackHit(attacker, this.gameObject, damageEvent);
        
        float value = DamageCalculator.CalcDamage(status, damageEvent);

        // 저주에 의한 피해량 수정 
        if(damageEvent.IsDOTEffect() == false && this.TryGetComponent<EffectComponent>(out var effectComp))
        {
            if(effectComp.HasEffect("Curse") is CurseEffect curse)
            {
                value *= (1.0f + curse.GetDamageIncrease());
                Debug.Log($"저주(Curse) 효과로 인해 최종 피해량 {curse.GetDamageIncrease() * 100}% 증가!");
            }
        }

        // 체력 감소
        health?.Damage(value);
        BattleManager.Instance?.NotifyAttackHitFinish(attacker, this.gameObject, value);

        // 텍스트 처리 
        ShowDamageText(value, damageEvent);

        if(damageEvent.hitData.DamageType == DamageType.DOT_POISON ||
            damageEvent.hitData.DamageType == DamageType.DOT_BURN ||
            damageEvent.hitData.DamageType == DamageType.DOT_BLEED)
        {
            return; 
        }

        // Play Damage Animation
        PlayDamageAnimation(damageEvent.hitData);
    }

    private void ShowDamageText(float value, DamageEvent damageEvent)
    {
        // 월드 좌표로 변환해서 보내야 적이 회전하는 경우에도 데미지 텍스트가 온전히 화면으로 보임 
        Vector3 pos = transform.position + Vector3.up * dmgFontOffsetY;
        UIManager.Instance?.DrawDamageText(pos, value, damageEvent);
    }

    public void PlayDamageAnimation(HitData data)
    {
        if(data == null) return;
        // 애니메이터가 없거나 해당 스테이트가 없는 경우 
        if(animator == null || animator.runtimeAnimatorController == null)
        {
            character?.End_Damaged();
            return; 
        }

        if (damageMotionTable == null) return; 

        if(overrideController != null && damageMotionTable.TryGetValue(data.DamageType, out List<DamageMotionData> list))
        {
            overrideController["Hit"] = list[data.HitImpactIndex].damageMotion;
        }

        animator.SetTrigger(DamageAnimStateName);
    }
}
