using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class Character 
    : MonoBehaviour
    , IStoppable
    , ISlowable
{
    protected Animator animator;
    protected new Rigidbody rigidbody;

    protected StateComponent state;
    protected HealthPointComponent healthPoint;

    private float originAnimSpeed; 

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        Debug.Assert(animator != null);
        originAnimSpeed = animator.speed;

        rigidbody = GetComponent<Rigidbody>();  
        Debug.Assert(rigidbody != null);

        state = GetComponent<StateComponent>();
        healthPoint = GetComponent<HealthPointComponent>();
    }

    protected virtual void Start()
    {
        Regist_MovableStopper();
        Regist_MovableSlower();
    }


    protected virtual void End_Damaged()
    {

    }

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
