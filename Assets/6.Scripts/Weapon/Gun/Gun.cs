using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Gun : Weapon_Combo
{

    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private string[] muzzle_names = { "Gun_Muzzle" };
    [SerializeField] private string bulletName = "Bullet";


    private List<Transform> muzzleTransforms = new();

    // [발사 모드 보너스] 한 번에 다 쏠지, 번갈아가며 쏠지?
    [Tooltip("체크하면 더블배럴처럼 한 번에 쏘고, 끄면 쌍권총처럼 번갈아 쏩니다.")]
    [SerializeField] private bool fireSimultaneously = true;
    private int currentMuzzleIndex = 0; // 번갈아 쏠 때 사용할 인덱스

    protected override void Start()
    {
        base.Start();
    }

    public List<Transform> GetMuzzleTransforms()
    {
        return muzzleTransforms;
    }


    public override void Begin_Equip()
    {
        base.Begin_Equip();

        if (rootObject == null)
            return;

        muzzleTransforms.Clear();

        foreach (string muzzleName in muzzle_names)
        {
            Transform foundMuzzle = rootObject.transform.FindChildByName(muzzleName);
            if (foundMuzzle != null)
            {
                muzzleTransforms.Add(foundMuzzle);
            }
        }
        End_Equip();
    }

    public override void Begin_DoAction()
    {
        if (muzzleTransforms.Count == 0)
            return;

        base.Begin_DoAction();

        actionDatas[index].Play_CameraShake();
    }

    public override void Begin_JudgeAttack(AnimationEvent e)
    {
        base.Begin_JudgeAttack(e);

        
        if(fireSimultaneously)
        {
            foreach(Transform muzzle in muzzleTransforms)
                FireBulletFromMuzzle(muzzle);
        }
        else
        {
            Transform muzzle = muzzleTransforms[currentMuzzleIndex];
            FireBulletFromMuzzle(muzzle); 

            currentMuzzleIndex = (currentMuzzleIndex + 1) % muzzleTransforms.Count;
        }

    }

    private void FireBulletFromMuzzle(Transform muzzle)
    {
        if(muzzleFlashPrefab != null)
        {
            Instantiate<GameObject>(muzzleFlashPrefab, muzzle); 
        }


        GameObject obj = ObjectPooler.SpawnFromPool(bulletName, muzzle.position, muzzle.rotation);
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
        // hit Sound Play
        //SoundManager.Instance.PlaySFX(actionDatas[index].hitSoundName);

        //Instantiate<GameObject>(actionDatas[index].HitParticle, point, rootObject.transform.rotation);
    }
}
