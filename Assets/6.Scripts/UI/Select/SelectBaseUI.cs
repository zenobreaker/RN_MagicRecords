
using System;
using UnityEngine;
using UnityEngine.UI;

public class SelectBaseUI : UIPopUp
{

    public event Action OnSelectionChanged;

    protected override void Awake()
    {
        base.Awake();
    }


    protected override void DrawPopUp()
    {

    }

    protected void SelectionChanged()
    {
        OnSelectionChanged?.Invoke();
    }
}
