using Unity.Behavior;



///////////////////////////////////////////////////////////////////////////////
//  StageType
[System.Serializable]
public enum StageType
{
    None = 0,
    Combat,
    Event,
    Shop,
    Boss_Combat,
}

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
    EXPOLORE_CREDIT,
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

    EXPLORE_POINT = 98, // 탐사 재화
    CREDIT = 99,        // 특수 재화
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
    NORMAL = 0,
    STRONG,
    KNOCKBACK,
    DOWN,
    AIRBORNE,
    DOT_BLEED,
    DOT_BURN,
    DOT_POISON,

    DOT_HATERD,

    MAX
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
//  Effect Type 
public enum EffectType
{
    NONE,
    BUFF,
    DEBUFF,
    NEUTRAL,
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

///////////////////////////////////////////////////////////////////////////////
//  Item Rank 
public enum ItemRank
{
    NONE,
    COMMON,
    MAGIC,
    RARE,
    UNIQUE,
    LEGENDARY,

    MAX = LEGENDARY,
}

///////////////////////////////////////////////////////////////////////////////
//  Passive Trigger 
public enum TriggerEvent
{
    ON_GAME_START = 0,
    ON_ENTER_ROOM = 1,
    On_ATTACK,
    ON_DEAMGED,
    ON_ENENMY_KILLED
}

///////////////////////////////////////////////////////////////////////////////
//  Skill Trigger Time
public enum SkillTriggerTime
{
    OnExecute,
    OnBeginDoAction,
    OnJudgeAttack,
    OnEndDoAction,
    OnSoundEvent,
    OnCameraShake,
}

///////////////////////////////////////////////////////////////////////////////
//  Record Type 
public enum RecordType
{
    NONE,
    STAT,
    AGUMENT, // 증강
    MODIFY,  // 변형
    UNIQUE, // 독자적
}

// 필터링을 위한 Enum (시트의 TargetFilter와 맞춤)
public enum TargetFilterType
{
    ALL,
    Shooter, 
}

public enum RecordRarity
{

}
