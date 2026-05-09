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

        //TODO: 진행할 것인지 팝업
        //TODO : 기존에 진행한 것이 있는데 그대로 할 것인지? 
        AppManager.Instance.EnterTheExplorationProcess();
    }
}
