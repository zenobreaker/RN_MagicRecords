using System;
using UnityEngine;

public class Gun : Weapon_Combo
{
    [SerializeField] private GameObject bulletPrefabs;

    protected override void Start()
    {
        base.Start();
    }


    public override void Begin_DoAction()
    {
        base.Begin_DoAction();

        GameObject obj = Instantiate<GameObject>(bulletPrefabs, transform.position, rootObject.transform.rotation);
        if (obj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.OnProjectileHit += OnProjectileHit;
        }
    }


    private void OnProjectileHit(Collider self, Collider other, Vector3 point)
    {
        Debug.Log($"self : {self} other : {other}");

        // hit Sound Play
        //SoundManager.Instance.PlaySFX(doActionDatas[index].hitSoundName);

        //IDamagable damage = other.GetComponent<IDamagable>();
        //if (damage != null)
        //{
        //    Vector3 hitPoint = self.ClosestPoint(other.transform.position);
        //    hitPoint = other.transform.InverseTransformPoint(hitPoint);
        //    damage?.OnDamage(rootObject, this, hitPoint, doActionDatas[index]);

        //    return;
        //}

        //Instantiate<GameObject>(doActionDatas[index].HitParticle, point, rootObject.transform.rotation);
    }
}
