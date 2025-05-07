using UnityEngine;

public interface IDamagable 
{
    public void OnDamage(GameObject attacker, Weapon causer, Vector3 hitPoint, ActionData data);
}
