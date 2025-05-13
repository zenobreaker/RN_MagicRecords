using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_ComboInputHanlder", menuName = "Scriptable Objects/SO_ComboInputHanlder")]
public class SO_ComboInputHanlder : ScriptableObject
{
    public event Action<float> OnComboRemainTime;
    public event Action<int> OnComboCount; 

    public void HandleComboRemainTime(float time) => OnComboRemainTime?.Invoke(time); 
    public void HandleComboCount(int count) => OnComboCount?.Invoke(count);
    
}
