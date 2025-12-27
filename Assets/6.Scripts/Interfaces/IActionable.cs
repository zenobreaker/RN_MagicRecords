using UnityEngine;

public interface IActionable 
{
    void SetActionComponent(ActionComponent action);
    ActionComponent GetCurrentAction();
}
