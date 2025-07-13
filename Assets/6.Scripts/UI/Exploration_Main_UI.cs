using UnityEngine;

public class Exploration_Main_UI : UiBase
{
    protected override void OnEnable()
    {
        base.OnEnable();
        UIManager.Instance.OpenUI(this); 
    }
}
