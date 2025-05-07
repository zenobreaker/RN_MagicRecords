using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �� ���⺰ ���� �� �޺� �Է� �ð� �� ���� ���� ���� ������ �����Ѵ�. 
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
    ///  �޺� ���� �ð� 
    /// - ó�� �޺� ���� �ð��� ª�ƾ� �Ѵ�.
    /// </summary>
    [SerializeField] private float lastComboCheckTime = 0.1f;
    public float LastComboCheckTime { get => lastComboCheckTime; }
    /// <summary>
    /// ���� �޺��� �Է��� �ٶ�� ���� �ð�
    /// </summary>
    [SerializeField] private float lastInputCheckTime = 0.5f;
    public float LastInputCheckTime { get => lastInputCheckTime; }
    /// <summary>
    /// �޺� ���� �ð� 
    /// - �ش� �ð��� �޺� ť ���� ���� ������ �����ϴ� �ð��̴� ���� �� �޺� �ʱ�ȭ
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
