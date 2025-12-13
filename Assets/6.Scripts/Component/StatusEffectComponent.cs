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

/// <summary>
/// 캐릭터에 상태이상 관리용 컴포넌트 
/// </summary>
public class StatusEffectComponent : MonoBehaviour
{
    [SerializeField] private StatusEffectType statusEffectType;

    public event Action<StatusEffectType, StatusEffectType> OnStatusEffectChanged;

    private GameObject rootObject;


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


    public void AddStatusEffect(StatusEffectType inType)
    {
        statusEffectType |= inType;
        OnStatusEffectChanged?.Invoke(statusEffectType, inType);
    }

    public void RemoveStatusEffect(StatusEffectType inType)
    {
        statusEffectType &= ~inType;
        OnStatusEffectChanged?.Invoke(statusEffectType, inType);
    }


    private bool HasCondition(StatusEffectType inType)
    {
        return (statusEffectType & inType) != 0;
    }

    // 움직일 수 있는 상태이상인가에 대한 반환 
    public bool GetMovableCondition()
    {
        bool check = false;
        check |= HasCondition(StatusEffectType.Stun);
        check |= HasCondition(StatusEffectType.Down);
        check |= HasCondition(StatusEffectType.Ice);
        check |= HasCondition(StatusEffectType.Airborne);

        return check == false; 
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
