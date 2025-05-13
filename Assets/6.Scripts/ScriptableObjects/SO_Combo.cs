using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 각 무기별 구간 별 콤보 입력 시간 및 공격 등의 대한 정보를 저장한다. 
/// </summary>
[System.Serializable]
public class ComboData
{
    [Header("Combo Input")]
    [SerializeField, Tooltip("선입력 허용 시간")] 
    private float inputBufferTime = 0.5f;
    public float InputBufferTime { get => inputBufferTime; }

    [SerializeField, Tooltip("이전 입력이 이후 다음 입력이 유효한 최대 시간")]
    private float lastInputCheckTime = 0.5f;
    public float LastInputCheckTime { get => lastInputCheckTime; }

    [SerializeField, Tooltip("입력이 없을 경우 콤보를 초기화하는 시간")]
    private float comboResetTime = 0.5f;
    public float ComboResetTime { get => comboResetTime; }
}

[CreateAssetMenu(fileName = "ComboObject", menuName = "Scriptable Objects/Combo", order = 1)]
public class SO_Combo : ScriptableObject
{
    public List<ComboData> comboDatas = new List<ComboData>();

    private Action onFinissCombo;
    public event Action OnFinishCombo
    {
        add
        {
            if (onFinissCombo == null || !onFinissCombo.GetInvocationList().Contains(value))
            {
                onFinissCombo += value;
            }
        }
        remove
        {
            onFinissCombo -= value;
        }
    }

    public ComboData GetComboData(int index)
    {
        if (index >= comboDatas.Count)
            index = 0;

        return comboDatas[index];
    }

    public int MaxComboIndex()
    {
        return comboDatas.Count; 
    }

}
