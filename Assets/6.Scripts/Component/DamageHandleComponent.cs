using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;


[System.Serializable]
public class DamageMotionData
{
    public DamageType damageType;
    public AnimatorOverrideController AO_DamageMotion;
}

[System.Serializable]
public class DamageMotionNameData
{
    public DamageType damageType;
    public string DanameAnimStateSubfixName; 
}

public class DamageHandleComponent : MonoBehaviour
{
    [SerializeField]
    private List<DamageMotionData> damageMotions = new List<DamageMotionData>();
    private Dictionary<DamageType, List<DamageMotionData>> damageMotionTable;

    [SerializeField]
    private float dmgFontOffsetY = 1.5f;

    [SerializeField]
    private string DamageAnimStateName = "Hit";

    private Animator animator; 
    private HealthPointComponent health;
    private StatusComponent status;

    public Action OnDamaged;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        health = GetComponent<HealthPointComponent>();
        status = GetComponent<StatusComponent>();

        Awake_SetDamageMotionData();
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
        DamageText dt = ObjectPooler.SpawnFromPool<DamageText>("DamageText", pos);
        dt?.DrawDamage(pos, value, damageEvent);

    }

    public void PlayDamageAnimation(HitData data)
    {
        if(data == null) return;
        if (animator == null) return;
        if (damageMotionTable == null) return; 

        if(damageMotionTable.TryGetValue(data.DamageType, out List<DamageMotionData> list))
        {
            if (list.Count > data.HitImpactIndex && list[data.HitImpactIndex].AO_DamageMotion != null)
                animator.runtimeAnimatorController = list[data.HitImpactIndex].AO_DamageMotion;
        }

        animator.SetTrigger(DamageAnimStateName);
    }

}
