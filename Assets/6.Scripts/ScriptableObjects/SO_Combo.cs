using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 각 무기별 구간 별 콤보 입력 시간 및 공격 등의 대한 정보를 저장한다. 
/// </summary>
[System.Serializable]
public class ComboData
{
    [SerializeField]
    private int comboIndex;
    public int ComboIndex { get => comboIndex; }

    public string ComboName;
    /// <summary>
    ///  콤보 종료 시간 
    /// - 처음 콤보 종료 시간은 짧아야 한다.
    /// </summary>
    public float lastComboCheckTime = 0.1f;
    /// <summary>
    /// 다음 콤보를 입력을 바라는 제한 시간
    /// </summary>
    public float lastInputCheckTime = 0.5f;
    /// <summary>
    /// 콤보 유지 시간 
    /// - 해당 시간은 콤보 큐 등의 대한 정보를 유지하는 시간이다 종료 시 콤보 초기화
    /// </summary>
    public float comboMaintainTime = 0.2f;


    public DoActionData doActionData;

    public string GetComboName { get => ComboName; }


}

[CreateAssetMenu(fileName = "ComboObject", menuName = "ScriptableObjects/Combo", order = 1)]
public class SO_Combo : ScriptableObject
{
    public List<ComboData> comboDatas = new List<ComboData>();
    private int comboCount;
    public DoActionData subActionData;
    public event Action OnFinishCombo;

    public void SetOnFinishCombo(Action onFinishCombo)
    {
        if (OnFinishCombo != null)
        {
            foreach (Action action in OnFinishCombo.GetInvocationList())
            {
                if (action == OnFinishCombo)
                    return;
            }
        }

        OnFinishCombo = onFinishCombo;
    }

    public ComboData GetComboData(int index)
    {
        return comboDatas[index];
    }

    public ComboData GetComboDataByRewind(int index)
    {
        index %= (comboDatas.Count);

        return GetComboData(index);
    }

    public ComboData GetNextComboData()
    {
        return GetComboDataByRewind(comboCount++);
    }

    public void ResetComboIndex()
    {
        comboCount = 0;
    }


    public void OnChangeCombo(int combo)
    {
        if (combo >= (comboDatas.Count))
        {
            OnFinishCombo?.Invoke();
        }
    }
}
