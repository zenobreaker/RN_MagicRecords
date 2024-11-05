using System;
using UnityEngine;

public class Gun : Weapon_Combo
{
    [SerializeField] private GameObject bulletPrefabs;
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
        if (bulletPrefabs == null  || muzzleTransform == null)
            return;

        base.Begin_DoAction();

        //GameObject obj = Instantiate<GameObject>(bulletPrefabs, muzzleTransform.position, rootObject.transform.rotation);
        GameObject obj = ObjectPooler.SpawnFromPool(bulletName, muzzleTransform.position, rootObject.transform.rotation);
        //TODO: TryGetComponent가 너무 자주 호출되서 비용이 높다면 이벤트 처리를 다르게 생각.
        if (obj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.OnProjectileHit -= OnProjectileHit;
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
