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

        if (aiBehaivour.GetTarget() == null)
        {
            GameObject target = perception.GetTarget();
            aiBehaivour.SetTarget(target);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        
    }

#endif
}
