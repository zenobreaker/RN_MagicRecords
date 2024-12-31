using System;
using UnityEngine;

public class ConditionComponent : MonoBehaviour
{
    public enum ConditionType
    {
        None, 
        Down, 
        Airborne, 
        Max,
    }

    private ConditionType myCondition;
    public ConditionType MyCondition { get => myCondition; }

    public event Action<ConditionType, ConditionType> OnConditionChanged;

    public bool NoneCondition { get => myCondition == ConditionType.None; }
    public bool DownCondition { get => myCondition == ConditionType.Down; }
    public bool AirborneCondition { get=> myCondition == ConditionType.Airborne; }
    public void SetNoneConditon() => ChangeCondition(ConditionType.None);
    public void SetDownCondition() => ChangeCondition(ConditionType.Down);
    public void SetAirborneCondition() => ChangeCondition(ConditionType.Airborne);

    
    private void ChangeCondition(ConditionType type)
    {
        if (this.myCondition == type)
            return;

        ConditionType prevCondition = this.myCondition;
        this.myCondition = type;

        OnConditionChanged?.Invoke(prevCondition, myCondition);    
    }
 
}
