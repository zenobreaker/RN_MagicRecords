[System.Serializable]
public class StatModifier 
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
        return GetOnlyValue() + (valueType == ModifierValueType.Percent ? "%" : "");
    }

    public string GetFullValue()
    {
        return type.ToString() + " " + GetValueAndValueType(); 
    }
}
