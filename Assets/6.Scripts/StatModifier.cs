using System;

[System.Serializable]
public class StatModifier :ICloneable
{
    public StatusType type;
    public float value;
    public ModifierValueType valueType; 

    public StatModifier(StatusType type, float value, ModifierValueType valueType)
    {
        this.type = type;
        this.value = value;
        this.valueType = valueType;
    }

    public override string ToString()
    {
        return GetFullValue();
    }

    public string GetOnlyValue()
    {
        return value.ToString(); 
    }

    /// <summary>
    ///  output : fixed : 0 percent : 0%
    /// </summary>
    /// <returns></returns>
    public string GetValueAndValueType()
    {
        return GetOnlyValue() + (valueType == ModifierValueType.PERCENT ? "%" : "");
    }

    public string GetFullValue()
    {
        return type.ToString() + " " + GetValueAndValueType(); 
    }

    public object Clone()
    {
        return new StatModifier(type, value, valueType);
    }

    public string GetLocalKey()
    {
        string key = ""; 
        switch(type)
        {
            case StatusType.ATTACK:
                key = "stat_attack";
                break; 
            case StatusType.HEALTH:
                key = "stat_health";
                break;
            case StatusType.DEFENSE:
                key = "stat_defense";
                break;
            case StatusType.ATTACKSPEED:
                key = "stat_attack_speed";
                break;
            case StatusType.MOVESPEED:
                key = "stat_move_speed";
                break;
            case StatusType.CRIT_RATIO:
                key = "stat_crit_ratio";
                break;
            case StatusType.CRIT_DMG:
                key = "stat_crit_dmg";
                break;

        }

        return key; 
    }
}
