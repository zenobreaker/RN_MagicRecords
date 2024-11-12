using UnityEngine;

public class SkillComponent : MonoBehaviour
{
    private Animator animator;

    private bool bIsSkillAction = false; 

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        
    }


}
