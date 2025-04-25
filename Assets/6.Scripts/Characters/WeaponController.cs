using UnityEngine;

public class WeaponController : MonoBehaviour
{
    private Animator weaponAnimator;

    private void Start()
    {
        weaponAnimator = GetComponent<Animator>();
    }

    public void SetWeaponAnimation(AnimatorOverrideController overrideController)
    {
        if (weaponAnimator == null)
            return;

        weaponAnimator.runtimeAnimatorController = overrideController;
    }

    public void DoAction(string stateName)
    { 
        if (weaponAnimator == null)
            return;

        weaponAnimator.Play(stateName);
    }
}
