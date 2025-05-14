using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_ComboInputHanlder", menuName = "Scriptable Objects/SO_ComboInputHanlder")]
public class SO_ComboInputHanlder : ScriptableObject
{
    public event Action<float, float> OnInputResetTime;
    public event Action<int> OnComboIndex; 
    public event Action<InputCommandType> OnInputCommandType;
    public event Action<bool> OnInputEnabled;
    public event Action<float> OnInputEnableTime; 
    public event Action<bool> OnInputBuffered;
    public event Action<float> OnInputBufferTime; 
    public event Action OnInputReset;

    public void HandleInputResetTime(float time, float maxTime) => OnInputResetTime?.Invoke(time, maxTime); 
    public void HandleComboIndex(int count) => OnComboIndex?.Invoke(count);
    public void HandleInputCommandType(InputCommandType inputCommandType) => OnInputCommandType?.Invoke(inputCommandType);
    public void HandleInputEnabled(bool enable) => OnInputEnabled?.Invoke(enable);
    public void HandleInputEnableTime(float time) => OnInputEnableTime?.Invoke(time);
    public void HandleInputBuffered(bool inBuffered) => OnInputBuffered?.Invoke(inBuffered);
    public void HandleInputBufferTime(float time) => OnInputBufferTime?.Invoke(time);
    public void HadleInputReset() => OnInputReset?.Invoke();
}
