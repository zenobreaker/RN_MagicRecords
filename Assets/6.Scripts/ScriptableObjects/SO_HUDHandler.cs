using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_HUDHandler", menuName = "Scriptable Objects/SO_HUDHandler")]
public class SO_HUDHandler : ScriptableObject
{
    public event Action<float> OnInitHP;
    public event Action<float> OnInitMP;

    public event Action<float> OnChangeHP;
    public event Action<float, float> OnChangeHP_TwoParam;
    public event Action<float> OnChangeMP;
    public event Action<float, float> OnChangeMP_TwoParam;
    public event Action<Character, float, float> OnChangedBossHP_TowParam;


    public event Action<BaseEffect> OnEffect;


 

    public void OnInitValue_HP(float value) => OnInitHP?.Invoke(value);
    public void OnInitValue_MP(float value) => OnInitMP?.Invoke(value);

    public void OnChangeValue_HP(float value) => OnChangeHP?.Invoke(value);

    public void OnChangeValue_HP(float value1, float value2) => OnChangeHP_TwoParam?.Invoke(value1, value2);

    public void OnChangeValue_MP(float value) => OnChangeMP.Invoke(value);

    public void OnChangeValue_MP(float value1, float value2) => OnChangeMP_TwoParam?.Invoke(value1, value2);

    public void OnApplyEffect(BaseEffect effect)
    {
        OnEffect?.Invoke(effect);
    }

    public void OnChangedValue_BossHP(Character boss, float value1, float value2)
        => OnChangedBossHP_TowParam?.Invoke(boss, value1, value2);
}
