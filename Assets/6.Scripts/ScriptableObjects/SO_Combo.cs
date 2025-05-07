using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 각 무기별 구간 별 콤보 입력 시간 및 공격 등의 대한 정보를 저장한다. 
/// </summary>
[System.Serializable]
public class ComboData
{
    [Header("Combo Index")]
    [SerializeField]
    private int comboIndex;
    public int ComboIndex { get => comboIndex; }

    [Header("Combo Input")]
    /// <summary>
    ///  콤보 종료 시간 
    /// - 처음 콤보 종료 시간은 짧아야 한다.
    /// </summary>
    [SerializeField] private float lastComboCheckTime = 0.1f;
    public float LastComboCheckTime { get => lastComboCheckTime; }
    /// <summary>
    /// 다음 콤보를 입력을 바라는 제한 시간
    /// </summary>
    [SerializeField] private float lastInputCheckTime = 0.5f;
    public float LastInputCheckTime { get => lastInputCheckTime; }
    /// <summary>
    /// 콤보 유지 시간 
    /// - 해당 시간은 콤보 큐 등의 대한 정보를 유지하는 시간이다 종료 시 콤보 초기화
    /// </summary>
    [SerializeField] private float comboMaintainTime = 0.2f;
    public float ComboMaintainTime { get => comboMaintainTime; }

    [Header("Action Data")]
    [SerializeField] private ActionData actionData;
    public ActionData Action { get => actionData; }

}

[CreateAssetMenu(fileName = "ComboObject", menuName = "ScriptableObjects/Combo", order = 1)]
public class SO_Combo : ScriptableObject
{
    private int comboCount;

    public List<ComboData> comboDatas = new List<ComboData>();

    public ActionData subActionData;
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
