using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �� ���⺰ ���� �� �޺� �Է� �ð� �� ���� ���� ���� ������ �����Ѵ�. 
/// </summary>
[System.Serializable]
public class ComboData
{
    [SerializeField]
    private int comboIndex;
    public int ComboIndex { get => comboIndex; }

    public string ComboName;
    /// <summary>
    ///  �޺� ���� �ð� 
    /// - ó�� �޺� ���� �ð��� ª�ƾ� �Ѵ�.
    /// </summary>
    public float lastComboCheckTime = 0.1f;
    /// <summary>
    /// ���� �޺��� �Է��� �ٶ�� ���� �ð�
    /// </summary>
    public float lastInputCheckTime = 0.5f;
    /// <summary>
    /// �޺� ���� �ð� 
    /// - �ش� �ð��� �޺� ť ���� ���� ������ �����ϴ� �ð��̴� ���� �� �޺� �ʱ�ȭ
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
