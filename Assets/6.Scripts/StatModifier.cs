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
}
