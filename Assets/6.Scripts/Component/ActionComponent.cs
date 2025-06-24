
using System;
using UnityEngine;


public abstract class ActionComponent
    : MonoBehaviour
{
    public Action OnDoAction;
    public Action OnBeginDoAction;
    public Action OnEndDoAction;

    protected GameObject rootObject;
    protected bool bInAction;
    public bool InAction { get => bInAction; private set => bInAction = value; }

    public virtual void DoAction()
    {
        OnDoAction?.Invoke(); 
        InAction = true;
    }
    public virtual void StartAction() { }

    public virtual void BeginDoAction() { }
        
    public virtual void EndDoAction()
    {
        Debug.Log($"Current : {bInAction}");
        InAction = false;
        Debug.Log($"Affter : {bInAction}");
    }

    public virtual void BeginJudgeAttack(AnimationEvent e) { }
    public virtual void EndJudgeAttack(AnimationEvent e) { }

    public virtual void PlaySound() { }
    public virtual void PlayCameraShake() { }

}