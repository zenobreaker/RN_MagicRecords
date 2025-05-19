using UnityEngine;

public interface ILaunchable
{
    void ApplyLaunch(GameObject attacker, Weapon causer, HitData hitData);
}
