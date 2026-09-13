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

    public void DoAction(ActionData actionData, bool restartFromBeginning = false)
    {
        if (weaponAnimator == null) return; 

        int layer = AnimatorLayerCache.GetLayerIndex(weaponAnimator, actionData?.LayerName);
        DoAction(actionData?.WeaponActionName, layer, restartFromBeginning);
    }

    public void DoAction(string stateName, int layerIndex = 0, bool restartFromBeginning = false)
    {
        if (weaponAnimator == null)
            return;

        if (string.IsNullOrEmpty(stateName) == false)
            weaponAnimator.Play(stateName, layerIndex, restartFromBeginning ? 0f : float.NegativeInfinity);
    }


    // AnimationEvent e 
    public void Play_Sound(string soundName)
    {
        SoundManager.Instance.PlaySFX(soundName);
    }
}
