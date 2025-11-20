using UnityEngine;

public class Exploration_Main_UI : UiBase
{
    protected override void OnEnable()
    {
        base.OnEnable();
        
    }

    public void EnterTheExploration()
    {
        if (AppManager.Instance == null) return;

        AppManager.Instance.EnterTheExplorationProcess();
    }
}
