using Unity.Behavior;

///////////////////////////////////////////////////////////////////////////////
//  ItemCategory
[System.Serializable]
public enum ItemCategory
{
    NONE,
    CURRENCY = 1,
    EQUIPMENT = 2,
    INGREDIANT,
    MAX,
}

///////////////////////////////////////////////////////////////////////////////
//  Currency
public enum CurrencyType
{
    NONE,
    GOLD, 
    DIAMOND, 
}

///////////////////////////////////////////////////////////////////////////////
//  Reward
public enum RewardType
{
    NONE,
    CURRENCY = 1, 
    EXP, 
    INGREDIENT,
    EQUIPMENT,

    CREDIT = 99,
}

///////////////////////////////////////////////////////////////////////////////
//  Status
public enum StatusType
{
    NONE = 0, 
    ATTACK = 1, 
    DEFENSE = 2, 
    ATTACKSPEED = 3, 
    MOVESPEED = 4,
    CRIT_RATIO = 5,  
    CRIT_DMG = 6, 
    
    HEALTH,
    HEALTH_REGEN,

    MAX,
}

///////////////////////////////////////////////////////////////////////////////
//  Damage 
public enum DamageType
{
    NORMAL = 0, STRONG, KNOCKBACK, DOWN, AIRBORNE, MAX
}


///////////////////////////////////////////////////////////////////////////////
//  Input 
public enum InputCommandType
{
    NONE = 0,
    ACTION = 1,
    SKILL,
    MOVE,
    DASH,
    MAX,
}

///////////////////////////////////////////////////////////////////////////////
//  AI State
[BlackboardEnum]
public enum AIState
{
    WAIT = 0, PATROL, APPROACH, ACTION, DAMAGED, DEAD, MAX,
}

///////////////////////////////////////////////////////////////////////////////
//  Buff
public enum BuffStackPolicy
{
    REFRESH_ONLY = 0,
    STACKABLE,
    IGNOREIFEXSIST,
}

public enum ModifierValueType
{
    FIXED,
    PERCENT,
}

///////////////////////////////////////////////////////////////////////////////
//  Equipment
public enum EquipParts
{
    NONE = -1,
    WEAPON = 0,
    CARFRAME = 1,
    CORE,
    ENGINE,
    SENSOR,
    WHEEL,
    MAX = 6,
}