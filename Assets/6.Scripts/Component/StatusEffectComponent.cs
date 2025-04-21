using System;
using UnityEngine;

[System.Flags]
public enum StatusEffectType
{
    None = 0,
    Airborne = 1 << 0,
    Down = 1 << 1,
    Stun = 1 << 2,
    Ice = 1 << 3,
    Max,
}

public class StatusEffectComponent : MonoBehaviour
{
    /// <summary>
    /// ĳ���Ϳ� �����̻� ������ ������Ʈ 
    /// </summary>

    [SerializeField] private StatusEffectType statusEffectType;

    public event Action<StatusEffectType, StatusEffectType> OnStatusEffectChanged;

    GameObject OwnerCharacter; 

    private void Start()
    {
        statusEffectType = StatusEffectType.None;
    }


    public void AddStunEffect()
    {
        Debug.Log("Stun!");
        AddStatusEffect(StatusEffectType.Stun);
    }

    public void RemoveStunEffect()
    {
        RemoveStatusEffect(StatusEffectType.Stun);
    }


    private void AddStatusEffect(StatusEffectType inType)
    {
        statusEffectType |= inType;
        OnStatusEffectChanged?.Invoke(statusEffectType, inType);
    }

    private void RemoveStatusEffect(StatusEffectType inType)
    {
        statusEffectType &= ~inType;
        OnStatusEffectChanged?.Invoke(statusEffectType, inType);
    }


    private bool HasCondition(StatusEffectType inType)
    {
        return (statusEffectType & inType) != 0;
    }

    private void ApplyStatusEffect()
    {
        foreach(StatusEffectType type in Enum.GetValues(typeof(StatusEffectType)))
        {
            if (type == StatusEffectType.None)
                continue; 

            if((type & statusEffectType) != StatusEffectType.None)
            {
                HandleStatusEffect(type);   
            }
        }
    }

    private void HandleStatusEffect(StatusEffectType inType)
    {
        switch(inType)
        {
            case StatusEffectType.Stun:
                break; 
        }
    }
}
