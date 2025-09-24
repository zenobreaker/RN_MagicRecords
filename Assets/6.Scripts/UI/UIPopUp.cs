using UnityEngine;

public abstract class UIPopUp : UiBase
{
    
    protected abstract void DrawPopUp();

    protected virtual void OpenPopUp()
    {
        UIManager.Instance?.OpenUI(this);
    }

    protected virtual void ClosePopUp()
    {
        UIManager.Instance?.CloseTopUI();
    }

}
