using UnityEngine;

public interface ILaunchable
{
    void ApplyLauch(GameObject attacker, Weapon causer, HitData hitData);
}
