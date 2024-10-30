
using System;
using UnityEngine;

public interface IActionable
{
    public void DoAction();

    public void End_DoAction();
}

public abstract class ActionComponent
    : MonoBehaviour
    , IActionable
{
    public Action OnBeginDoAction;
    public Action OnEndDoAction;

    protected bool bInAction;
    public bool InAction { get => bInAction; private set => bInAction = value; }

    public virtual void DoAction()
    {
        InAction = true;
    }

    public virtual void End_DoAction()
    {
        InAction = false;
    }

}