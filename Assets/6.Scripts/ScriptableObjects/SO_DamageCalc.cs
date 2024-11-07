using UnityEngine;

/// <summary>
/// ������ = ���ݷ� * ��ų ��� - ����
/// </summary>

[CreateAssetMenu(fileName = "SO_DamageCalc", menuName = "Scriptable Objects/SO_DamageCalc")]
public class SO_DamageCalc : ScriptableObject
{
    public float TakeDamage(float damage, float defense)
    {
        float finalValue = 0f;
        finalValue += damage - defense;

        return finalValue;
    }
}
