using Unity.Behavior;

///////////////////////////////////////////////////////////////////////////////
//  Damage 
public enum DamageType
{
    Normal = 0, Strong, Knockback, Down, Airborne, Max
}


///////////////////////////////////////////////////////////////////////////////
//  Input 

public enum InputCommandType
{
    None = 0,
    Action = 1,
    Skill,
    Move,
    Dash,
    Max,
}


///////////////////////////////////////////////////////////////////////////////
//  AI State
[BlackboardEnum]
public enum AIState
{
    Wait = 0, Patrol, Approach, Action, Damaged, Dead, Max,
}

///////////////////////////////////////////////////////////////////////////////
//  Buff
public enum BuffStackPolicy
{
    RefreshOnly = 0,
    Stackable,
    IgnoreIfExsist,
}

public enum BuffValueType
{
    Percent = 0,
    Fixed,
}

///////////////////////////////////////////////////////////////////////////////
//  Equipment
public enum EquipParts
{
    None = -1,
    Weapon = 0,
    CarFrame = 1,
    Core,
    Engine,
    Sensor,
    Wheel,
    Max = 6,
}