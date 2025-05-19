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
        if (weaponAnimator == null || overrideController == null)
            return;

        weaponAnimator.runtimeAnimatorController = overrideController;
    }

    public void DoAction(string stateName)
    {
        if (weaponAnimator == null)
            return;

        weaponAnimator.Play(stateName);
    }


    // AnimationEvent e 
    public void Play_Sound(string soundName)
    {
        SoundManager.Instance.PlaySFX(soundName);
    }
}
