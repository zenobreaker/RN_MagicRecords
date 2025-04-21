using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField]
    private string PlayAnimName = ""; 

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

    private void ActionWeapon()
    {
        if (weaponAnimator == null)
            return;

        if (string.IsNullOrEmpty(PlayAnimName) == true)
            return; 

        weaponAnimator.Play(PlayAnimName);
    }

    public void Begin_Action()
    {
        ActionWeapon(); 
    }

    public void End_Action()
    {

    }

}
