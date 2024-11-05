using UnityEngine;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class Character : MonoBehaviour
{
    protected Animator animator;
    protected new Rigidbody rigidbody;


    protected StateComponent state;
    protected HealthPointComponent healthPoint;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        Debug.Assert(animator != null);

        rigidbody = GetComponent<Rigidbody>();  
        Debug.Assert(rigidbody != null);

        state = GetComponent<StateComponent>();
        healthPoint = GetComponent<HealthPointComponent>();
    }

    protected virtual void Start()
    {
        
    }


    protected virtual void End_Damaged()
    {

    }
}
