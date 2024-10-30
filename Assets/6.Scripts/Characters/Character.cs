using UnityEngine;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class Character : MonoBehaviour
{
    protected Animator animator;
    protected new Rigidbody rigidbody;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        Debug.Assert(animator != null);

        rigidbody = GetComponent<Rigidbody>();  
        Debug.Assert(rigidbody != null);


    }

    protected virtual void Start()
    {
        
    }

}
