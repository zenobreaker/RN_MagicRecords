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

        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate<GameObject>(muzzleFlashPrefab, muzzleTransform);
        }

        GameObject obj = ObjectPooler.SpawnFromPool(bulletName, muzzleTransform.position, rootObject.transform.rotation);
        //TODO: TryGetComponent가 너무 자주 호출되서 비용이 높다면 이벤트 처리를 다르게 생각.
        if (obj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.AddIgnore(rootObject);
            projectile.Index = index;
            projectile.OnProjectileHit -= OnProjectileHit;
            projectile.OnProjectileHit += OnProjectileHit;
        }

        Play_Impulse(doActionDatas[index]);
    }


    public override void Begin_PlaySound()
    {
        base.Begin_PlaySound();

        SoundManager.Instance.PlaySFX(doActionDatas[index].SoundName);
    }


    private void OnProjectileHit(Collider self, Collider other, Vector3 point)
    {
#if UNITY_EDITOR
        Debug.Log($"self : {self} other : {other} Index : {index}");
#endif
        var projectile = self.GetComponent<Projectile>();
        if(projectile == null) return;

        int myIndex = projectile.Index;  // 충돌한 총알의 index 사용

        // hit Sound Play
        //SoundManager.Instance.PlaySFX(doActionDatas[index].hitSoundName);

        // Damage 
        IDamagable damage = other.GetComponent<IDamagable>();
        if (damage != null)
        {
            Vector3 hitPoint = self.ClosestPoint(other.transform.position);
            hitPoint = other.transform.InverseTransformPoint(hitPoint);

            if(doActionDatas[myIndex].damageDatas.Count > 0)
            {
                damage?.OnDamage(rootObject, this, hitPoint, doActionDatas[myIndex].damageDatas[0].GetMyDamageEvent(self.gameObject, status));
            }

        }

        //Instantiate<GameObject>(doActionDatas[index].HitParticle, point, rootObject.transform.rotation);
    }
}
