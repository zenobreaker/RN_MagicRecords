using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;


public enum WeaponType
{
    Unarmed = 0, Fist, Sword, Gun, MAX,
}

public enum SkillSlot
{
    Slot1 = 0, Slot2, Slot3, Slot4, MAX,
}

/// <summary>
/// 무기 관리 - 유사 커맨더 패턴 이용 
/// </summary>

public class WeaponComponent : ActionComponent
{
    [Header("Weapons")]
    [SerializeField] private GameObject[] originPrefabs;

    [Header("Weapon Type")]
    [SerializeField]  private WeaponType initType = WeaponType.Unarmed;
    private WeaponType type = WeaponType.Unarmed;
    public WeaponType Type { get => type; }

    #region Equipment 
    public bool UnarmedMode { get => type == WeaponType.Unarmed; }
    public bool FistMode { get => type == WeaponType.Fist; }
    public bool SwordMode { get => type == WeaponType.Sword; }
    public bool GunMode { get => type == WeaponType.Gun; }



    public void SetFistMode()
    {
        if (state.IdleMode == false)
            return;

        SetMode(WeaponType.Fist);
    }

    public void SetSwordMode()
    {
        if (state.IdleMode == false)
            return;

        SetMode(WeaponType.Sword);
    }

    public void SetGunMode()
    {
        if (state.IdleMode == false)
            return;

        Debug.Log("Gun 장착 ");
        SetMode(WeaponType.Gun);
    }

    public void SetUnarmedMode()
    {
        if (state.IdleMode == false)
            return;

        //TODO: Animator Unarmed 

        if (weaponTable[type] != null)
            weaponTable[type]?.Unequip();

        ChangeType(WeaponType.Unarmed);
    }

    private void SetMode(WeaponType type)
    {
        if (this.type == type)
        {
            SetUnarmedMode();

            return;
        }
        else if (UnarmedMode == false)
        {
            weaponTable[this.type].Unequip();
        }

        if (weaponTable[type] == null)
        {
            SetUnarmedMode();

            return;
        }

        animator.SetBool("isEquipping", true);
        animator.SetInteger("WeaponType", (int)type);

        weaponTable[type].Equip();

        ChangeType(type);
    }
    #endregion

    private StateComponent state;
    private SkillComponent skill;

    private Animator animator;

    private bool bUseSkill = false;
    private Dictionary<WeaponType, Weapon> weaponTable;
    private readonly int IsAction = Animator.StringToHash("IsAction");

    public event Action<WeaponType, WeaponType> OnWeaponTypeChanged;
    public event Action<SO_Combo> OnWeaponTypeChanged_Combo;
    private void Awake()
    {
        weaponTable = new Dictionary<WeaponType, Weapon>();
        animator = GetComponent<Animator>();

        state = GetComponent<StateComponent>();
        skill = GetComponent<SkillComponent>();
        Debug.Log(skill != null);

        Awake_InitWeapon();
    } 

    private void Awake_InitWeapon()
    {
        for (int i = 0; i < (int)WeaponType.MAX; i++)
            weaponTable.Add((WeaponType)i, null);

        if (originPrefabs == null)
            return;

        for (int i = 0; i < originPrefabs.Length; i++)
        {
            GameObject obj = Instantiate<GameObject>(originPrefabs[i], transform);
            Weapon weapon = obj.GetComponent<Weapon>();
            if (weapon != null)
            {
                obj.name = weapon.Type.ToString();
            }
            weaponTable[weapon.Type] = weapon;
        }
    }

    private void Start()
    {
        // Equip
        switch (initType)
        {
            case WeaponType.Fist:
                SetFistMode();
                break;
            case WeaponType.Sword:
                SetSwordMode();
                break;
            case WeaponType.Gun:
                SetGunMode();
                break; 
        }
    }


    private void ChangeType(WeaponType type)
    {
        if (this.type == type)
            return;

        WeaponType prevType = this.type;
        this.type = type;

        OnWeaponTypeChanged?.Invoke(prevType, type);

        Weapon_Combo combo = weaponTable[type] as Weapon_Combo;
        SO_Combo so_Combo = combo?.ComboData;
        OnWeaponTypeChanged_Combo?.Invoke(so_Combo);
    }


    public void DoAction(int index = 0)
    {
        if (animator == null)
            return;

        if (bUseSkill)
            return;

        base.DoAction();

        //animator.SetBool(IsAction, true);

        weaponTable[type]?.DoAction(index);
    }

    public void DoSkillAction(SkillSlot slot)
    {
        bUseSkill = true;
        skill.UseSkill(slot);
    }


    public void Begin_DoAction()
    {
        OnBeginDoAction?.Invoke();


        if (bUseSkill == true)
        {
            skill?.Begin_SkillAction();
            return;
        }

        weaponTable[type]?.Begin_DoAction();
    }

    public override void End_DoAction()
    {
        base.End_DoAction();

        OnEndDoAction?.Invoke();

        if (bUseSkill == true)
        {
            skill?.End_SkillAction();
            bUseSkill = false;

            return;
        }

        weaponTable[type]?.End_DoAction();
    }
}
