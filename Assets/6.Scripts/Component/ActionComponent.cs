
using System;
using UnityEngine;


public abstract class ActionComponent
    : MonoBehaviour
{
    public Action OnBeginDoAction;
    public Action OnEndDoAction;

    protected GameObject rootObject;
    protected bool bInAction;
    public bool InAction { get => bInAction; private set => bInAction = value; }

    public virtual void DoAction()
    {
        InAction = true;
    }

    public virtual void BeginDoAction() { }
        
    public virtual void EndDoAction()
    {
        InAction = false;
    }

    public virtual void BeginJudgeAttack() { }
    public virtual void EndJudgeAttack() { }

    public virtual void PlaySound() { }
    public virtual void PlayCameraShake() { }

}