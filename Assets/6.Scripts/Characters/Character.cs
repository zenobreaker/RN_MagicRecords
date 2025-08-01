using System;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class Character
    : MonoBehaviour
    , IStoppable
    , ISlowable
    , ITeamAgent
{
    protected GenenricTeamId genericTeamId; 

    protected Animator animator;
    protected new Rigidbody rigidbody;
    protected CharacterVisual visual;
    public CharacterVisual Visual { get => visual; }

    protected StateComponent state;
    protected HealthPointComponent healthPoint;
    protected StatusComponent status; 

    protected float originAnimSpeed;
    protected bool bInAction = false; 
    public virtual bool InAction { get { return bInAction; } protected set { bInAction = value; } }

    public Action OnBeginDoAction;
    public Action OnEndDoAction;
    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        Debug.Assert(animator != null);
        originAnimSpeed = animator.speed;

        rigidbody = GetComponent<Rigidbody>();
        Debug.Assert(rigidbody != null);

        state = GetComponent<StateComponent>();
        healthPoint = GetComponent<HealthPointComponent>();
        status = GetComponent<StatusComponent>();

        visual = GetComponentInChildren<CharacterVisual>();
    }

    protected virtual void Start()
    {
        Regist_MovableStopper();
        Regist_MovableSlower();
    }

    protected virtual void End_Damaged() { bInAction = false; }


    public void SetGenericTeamId(GenenricTeamId id)
    {
        genericTeamId = id; 
    }

    public GenenricTeamId GetGeneriTeamId()
    {
        return genericTeamId;
    }


    #region AnimationEvent
    public virtual void Start_DoAction() { }
    public virtual void Begin_DoAction() { OnBeginDoAction?.Invoke(); }
    public virtual void End_DoAction() { OnEndDoAction?.Invoke(); }
    public virtual void Begin_JudgeAttack(AnimationEvent e = null) { }
    public virtual void End_JudgeAttack(AnimationEvent e = null) { }
    public virtual void Play_Sound() { }
    public virtual void Play_CameraShake() { }
    #endregion

    #region Slow
    private void Regist_MovableSlower()
    {
        MovableSlower.Instance.Regist(this);
    }
    public void ApplySlow(float duration, float slowFactor)
    {
        animator.speed = originAnimSpeed * slowFactor;
        StopCoroutine(ResetSpeedAfterDelay(duration));
        StartCoroutine(ResetSpeedAfterDelay(duration));
    }

    public void ResetSpeed()
    {
        animator.speed = originAnimSpeed;
    }

    public IEnumerator ResetSpeedAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        ResetSpeed();
    }

    #endregion

    #region Movable
    public void Regist_MovableStopper()
    {
        MovableStopper.Instance.Regist(this);
    }

    public IEnumerator Start_FrameDelay(int frame)
    {
        animator.speed = 0.0f;

        for (int i = 0; i < frame; i++)
            yield return new WaitForFixedUpdate();

        animator.speed = originAnimSpeed;
    }
    #endregion
}
