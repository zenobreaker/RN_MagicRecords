using System;
using System.Collections.Generic;
using UnityEngine;


public enum WeaponType
{
    Unarmed = 0, Fist, Sword, Gun, MAX,
}


/// <summary>
/// 무기 관리 - 유사 커맨더 패턴 이용 
/// </summary>

public class WeaponComponent 
    : ActionComponent
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

        if (weaponTable[type] != null)
            weaponTable[type]?.Unequip();

        ChangeType(WeaponType.Unarmed);
        visual?.SetWeaponEquipAnimation((int)WeaponType.Unarmed);
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

        visual?.SetWeaponEquipAnimation((int)type);

        weaponTable[type].Equip();
        ChangeType(type);
    }
    #endregion

    private CharacterVisual visual;
    private StateComponent state;
    private Dictionary<WeaponType, Weapon> weaponTable;
 
    public event Action<WeaponType, WeaponType> OnWeaponTypeChanged;
    public event Action<SO_Combo> OnWeaponTypeChanged_Combo;

    private void Awake()
    {
        weaponTable = new Dictionary<WeaponType, Weapon>();

        visual = GetComponentInChildren<CharacterVisual>();
        state = GetComponent<StateComponent>();

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
            if (originPrefabs[i] == null) continue; 

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


    public Weapon GetCurrentWeapon()
    {
        if (weaponTable == null) return null;
        return weaponTable[type];
    }

    public void DoActionWithIndex(int index = 0)
    {
        if (InAction == true || state.ActionMode) 
            return; 

        base.DoAction();
        weaponTable[type]?.DoAction(index);
    }

    public override void BeginDoAction()
    {
        base.BeginDoAction();

        Debug.Log($"Weapon Begin DoAction");
        OnBeginDoAction?.Invoke();
        weaponTable[type]?.Begin_DoAction();
    }

    public override void EndDoAction()
    {
        base.EndDoAction();

        Debug.Log($"Weapon End DoAction");
        OnEndDoAction?.Invoke();
        weaponTable[type]?.End_DoAction();
    }

    public override void BeginJudgeAttack(AnimationEvent e) 
    {
        base.BeginJudgeAttack(e); 
        weaponTable[type]?.Begin_JudgeAttack(e); 
    }

    public override void EndJudgeAttack(AnimationEvent e) 
    {
        base.EndJudgeAttack(e); 
        weaponTable[type]?.End_JudgeAttack(e); 
    }

    public override void PlaySound()
    {
        base.PlaySound();
        weaponTable[type]?.Play_PlaySound();
    }
    public override void PlayCameraShake()
    {
        base.PlayCameraShake();
        weaponTable[type]?.Play_CameraShake();
    }
}
