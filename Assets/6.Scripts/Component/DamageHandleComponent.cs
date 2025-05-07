using System.Collections.Generic;
using UnityEngine;


public enum DamageType
{
    Normal = 0, Strong, Knockback, Down, Airborne, Max
}

[System.Serializable]
public class DamageMotion
{
    public DamageType damageType;
    public AnimatorOverrideController AO_DamageMotion;
}

public class DamageHandleComponent : MonoBehaviour
{
    [SerializeField]
    private List<DamageMotion> damageMotions = new List<DamageMotion>();

    private HealthPointComponent health;
    private StatusComponent status; 

    private void Awake()
    {
        health = GetComponent<HealthPointComponent>();
        status = GetComponent<StatusComponent>(); 
    }

    private void Start()
    {

    }

    public float CalcDamage(float value)
    {
        float result = value;

        if(status != null)
        {
            float defense = status.GetStatusValue(StatusType.Defense);
            result = Mathf.Max(result - defense, 0.0f);
        }

        return result;
    }


    public void OnDamage(ActionData action)
    {
        if (action == null) return;

        //float value = CalcDamage(action.Power); 

        //health?.Damage()
    }
}
