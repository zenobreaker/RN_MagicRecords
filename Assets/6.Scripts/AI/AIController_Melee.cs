using UnityEngine;
using UnityEngine.EventSystems;

public class AIController_Melee : AIController
{
    [SerializeField] private float attackRange;

    private StateComponent state;

    public void Update()
    {
        if (perception == null || aiBehaivour == null) return;

        if (aiBehaivour.GetCanMove() == false)
            return; 

        GameObject target = perception.GetTarget();
        aiBehaivour.SetTarget(target); 
  
        if (target == null)
        {
            aiBehaivour.SetWaitMode();
            //aiBehaivour.SetPatrolMode(); 
            return; 
        }
        
        float distance = Vector3.Distance(target.transform.position, transform.position); 
        if(distance <= attackRange)
        {
            aiBehaivour.SetActionMode();
            return;
        }

        aiBehaivour.SetApproachMode();    
    }
}
