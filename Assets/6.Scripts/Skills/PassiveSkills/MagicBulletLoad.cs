using System.Collections.Generic;
using UnityEngine;

public struct BulletData
{
    public bool isCrit;
    public BulletData(bool isCrit)
        { this.isCrit = isCrit; }
}


/// <summary>
///  패시브 - 마법탄 장전 
///  습득 시 4/6/7 발 탄환을 장전, 탄환류 스킬에 소모됨 
///  마지막 공격 시, 탄환 1발 장전 
/// </summary>

public class Passive_MagicBulletLoad
    : PassiveSkill
    , IMagicBulletDataProvider
{
    private int maxBullets;
    private int currentBullet;
    private Queue<BulletData> bullets;
    private SkillComponent skillComponent;
    private WeaponComponent weaponComponent;
    private float reloadProgress;
    public const float BaseReloadInterval = 4f;

    public float ReloadInterval => BaseReloadInterval * (1f -
        (skillComponent.SafeInvoke(component => component.GetCapability<Module_Passive_AutoReload>())?.Reduction ?? 0f));

    public override void OnUpdate(float dt)
    {
        if (owner == null || !owner.activeInHierarchy || dt <= 0f) return;
        if (bullets.Count >= maxBullets) { reloadProgress = 0f; return; }

        // 비율로 진행도를 보관해 레코드 획득/제거 시 이미 진행한 장전을 유지합니다.
        reloadProgress += dt / ReloadInterval;
        while (reloadProgress >= 1f && bullets.Count < maxBullets)
        {
            reloadProgress -= 1f;
            Reload(1);
        }
        if (bullets.Count >= maxBullets) reloadProgress = 0f;
    }


    public Passive_MagicBulletLoad(int skillID, string skillName, string skillDesc, Sprite skillIcon) 
        : base(skillID, skillName, skillDesc, skillIcon)
    {
        bullets = new();
        currentBullet = 0;
    }

    public Passive_MagicBulletLoad(SO_SkillData skillData)
        : base(skillData)
    {
        bullets = new();
        currentBullet = 0; 
    }


    public bool TryConsumBullet(out bool isCrit)
    {
        bool consumed = TryConsumeBullet(out BulletData bullet);
        isCrit = consumed && bullet.isCrit;
        return consumed;
    }

    public bool TryConsumeBullet(out BulletData bullet)
    {
        bullet = default;
        if (bullets.Count == 0) return false;

        bullet = bullets.Dequeue();
        Debug.Log($"탄환 소모 ! crit = {bullet.isCrit}");

        NotifyBulletChanged();
        return true; 
    }

    public void Reload(int amount)
    {
        for(int i = 0; i < amount; i++)
        {
            if (bullets.Count >= maxBullets) break;

            ++currentBullet;
            bool isCrit = currentBullet == 4 || currentBullet == 7;
            currentBullet = currentBullet % maxBullets;
            bullets.Enqueue(new BulletData(isCrit));
        }

        NotifyBulletChanged();
    }

    public int CurrentBulletCount => bullets.Count;

    public override void OnAcquire(GameObject owner)
    {
        reloadProgress = 0f;
        if (owner != null)
            this.owner = owner;

        // SkillComponent에 제네릭으로 자신을 등록 
        if(owner != null &&
            owner.TryGetComponent<SkillComponent>(out skillComponent))
        {
            skillComponent.RegisterCapability<IMagicBulletProvider>(this);
        }

        if (owner != null && 
            owner.TryGetComponent<WeaponComponent>(out weaponComponent))
        {
            BindLastAttackEvent();
        }

        CalculateMaxBullet();
        GenerateBullets();

        NotifyBulletChanged();
    }

    public override void OnLose()
    {
        reloadProgress = 0f;
        bullets.Clear();
        NotifyBulletChanged();
     
        Weapon weapon = weaponComponent.SafeInvoke(component => component.GetCurrentWeapon());
        if (weapon != null)
        {
            weapon.OnLastAttackExecuted -= ExecuteReload;
        }
        owner = null;
        skillComponent = null;
        weaponComponent = null;
    }

    public override void OnChangedLevel(int newLevel)
    {
        skillLevel = newLevel;
        CalculateMaxBullet();
        GenerateBullets();
    }

    // 레벨별 최대 탄환 수 계산 
    private void CalculateMaxBullet()
    {
        maxBullets = skillLevel switch
        {
            1 => 4,
            2 => 6,
            3 => 7,
            _ => 4
        };
    }

    // 탄환 생성 
    private void GenerateBullets() 
    {
        reloadProgress = 0f;
        bullets.Clear();

        for (int i = 1; i <= maxBullets; i++)
        {
            bool isCrit = (i == 4 || i == 7);
            bullets.Enqueue(new BulletData(isCrit));
        }
        NotifyBulletInit();
    }

    private void BindLastAttackEvent()
    {
        Weapon weapon = weaponComponent.SafeInvoke(component => component.GetCurrentWeapon());
        if (weapon != null)
        {
            weapon.OnLastAttackExecuted -= ExecuteReload;
            weapon.OnLastAttackExecuted += ExecuteReload;
        }
    }

    private void ExecuteReload(GameObject owner)
    {
        if (bullets.Count >= maxBullets)
            return; 

        ++currentBullet;
        bool isCrit = currentBullet == 4 || currentBullet == 7;
        currentBullet = currentBullet % maxBullets;
        bullets.Enqueue(new BulletData(isCrit));

        NotifyBulletChanged();
    }
    

    private void NotifyBulletInit()
    {
        skillComponent.SafeInvoke(component => component.NotifyBulletInit(this.maxBullets));
    }

    private void NotifyBulletChanged()
    {
        skillComponent.SafeInvoke(component => component.NotifyMagicBulletChanged(this.bullets));
    }

  
}
