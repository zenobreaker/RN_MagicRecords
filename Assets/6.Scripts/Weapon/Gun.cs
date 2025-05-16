using System;
using System.Linq;
using UnityEngine;

public class Gun : Weapon_Combo
{

    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private string muzzle_name = "Gun_Muzzle";
    [SerializeField] private string bulletName = "Bullet";
    
    
    private Transform muzzleTransform;

    protected override void Start()
    {
        base.Start();
    }


    public override void Begin_Equip()
    {
        base.Begin_Equip();

        if (rootObject == null)
            return;

        muzzleTransform = rootObject.transform.FindChildByName(muzzle_name);
    }

    public override void Begin_DoAction()
    {
        if (muzzleTransform == null)
            return;

        base.Begin_DoAction();
     
        actionDatas[index].Play_CameraShake();
    }

    public override void Begin_JudgeAttack()
    {
        base.Begin_JudgeAttack();

        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate<GameObject>(muzzleFlashPrefab, muzzleTransform);
        }

        GameObject obj = ObjectPooler.SpawnFromPool(bulletName, muzzleTransform.position, rootObject.transform.rotation);
        if (obj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.SetDamageInfo(rootObject, damageDatas[index]);
            projectile.AddIgnore(rootObject);
            projectile.OnProjectileHit -= OnProjectileHit;
            projectile.OnProjectileHit += OnProjectileHit;
        }
    }

    public override void Play_PlaySound()
    {
        base.Play_PlaySound();

        actionDatas[index].Play_Sound(); 
    }

    public override void Play_CameraShake()
    {
        base.Play_CameraShake();

        actionDatas[index].Play_CameraShake();
    }

    private void OnProjectileHit(Collider self, Collider other, Vector3 point)
    {
#if UNITY_EDITOR
        Debug.Log($"self : {self} other : {other} Index : {index}");
#endif
        // hit Sound Play
        //SoundManager.Instance.PlaySFX(actionDatas[index].hitSoundName);

        //Instantiate<GameObject>(actionDatas[index].HitParticle, point, rootObject.transform.rotation);
    }
}
