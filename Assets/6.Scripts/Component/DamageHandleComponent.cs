using System;
using System.Collections.Generic;
using UnityEngine;


public enum DamageType
{
    Normal = 0, Strong, Knockback, Down, Airborne, Max
}

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

        for (int i = 0; i < (int)DamageType.Max; i++)
        {
            damageMotionTable.Add((DamageType)i, new List<DamageMotionData>());
        }

        foreach (DamageMotionData data in damageMotions)
        {
            damageMotionTable[data.damageType].Add(data);
        }
    }

    public float CalcDamage(float value)
    {
        float result = value;

        if(status != null)
        {
            float defense = status.GetStatusValue(StatusType.Defense);
            result = Mathf.Max(result - defense, 0.0f);
        }

        return result;
    }

    public void OnDamage(DamageEvent damageEvent)
    {
        if (damageEvent == null) return;

        float value = CalcDamage(damageEvent.value);

        health?.Damage(value);
        
        // ���� ��ǥ�� ��ȯ�ؼ� ������ ���� ȸ���ϴ� ��쿡�� ������ �ؽ�Ʈ�� ������ ȭ������ ���� 
        Vector3 pos = transform.position + Vector3.up * dmgFontOffsetY;
        DamageText dt = ObjectPooler.SpawnFromPool<DamageText>("DamageText", pos);
        dt?.DrawDamage(pos, damageEvent);

        // Play Damage Animation
        PlayDamageAnimation(damageEvent.hitData);
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
