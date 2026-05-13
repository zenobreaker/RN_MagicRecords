using Unity.Behavior;



///////////////////////////////////////////////////////////////////////////////
//  Language
public enum LanguageType { KR, EN }

///////////////////////////////////////////////////////////////////////////////
//  Record UI Type 
public enum RecordUIMode
{
    DRAFT,          // 새로운 레코드를 획득하는 모드
    SELECT_OWNED,    // 보유 중인 레코드를 선택하는 모드 (아카이브 보관용)
    SELECT_SAVED,   // 지난 회차에 저장한 레코드 선택하는 모드
}

///////////////////////////////////////////////////////////////////////////////
//  Exlplore
public enum ExploreState
{
    NONE,
    READY, // 탐사 선택 직후
    ON_EXPLORE, // 탐사 메인 씬
    IN_STAGE,   // 실제 전투
    STAGE_CLEAR, // 전투 종료 및 보상 획득 시점
    FINISH,     // 탐사 전체 종료
}

///////////////////////////////////////////////////////////////////////////////
// Node State 
public enum MapNodeState
{
    Locked,     // 선택 불가 (다른 분기점이거나 아직 못 가는 미래)
    Selectable, // 선택 가능 (현재 노드에서 갈 수 있는 다음 노드)
    Current,    // 현재 내 위치
    Cleared     // 이미 지나온 과거의 노드
}

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
    EXPOLORE_GOLD,
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
    EMPTY,
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
    NORMAL,
    RARE,
    UNIQUE,
    LEGENDARY,
    MYTH,
}


///////////////////////////////////////////////////////////////////////////////
// 이벤트 
public enum EventCostType
{
    NONE,
    GOLD,
    EXPLORE_COIN,
    HP_PERCENT,
    RECORD_ANY,
    RECORD_SAVE,
    MANA_DEBUFF,
    COMBAT_ELITE
}

// 이벤트 보상
public enum EventRewardType
{
    NONE, // 아무런 보상이 없는 등
    HP_HEAL,
    HP_HEAL_PERCENT,
    HP_DAMAGE,  // 고정 데미지 
    HP_DMG_PERCENT,
    ITEM,
    RECORD_ONE_OF_ALL,
    RECORD_ONE_OF_NORMAL,
    RECORD_ONE_OF_RARE,
    RECORD_ONE_OF_UNIQUE,
    RECORD_ONE_OF_LEGEND,
    RECORD_ONE_OF_MYTH,
    RECORD_DRAFT, // N개 중 택 1하는 이벤트 
    RECORD_SKILL_UP, // 스킬 강화 이벤트 
    MANA_REGEN_BUFF,
    ARCHIVE_SAVE,
    ARCHIVE_LOAD,

}



///////////////////////////////////////////////////////////////////////////////
//  Target Position Type 
public enum TargetPositionType
{
    CasterForward,      // 시전자의 정면 일정 거리
    FixedLocalOffset,   // 시전자 기준 특정 위치 (예: 내 오른쪽 2미터)
    RandomAroundCaster, // (미래용) 시전자 주변 무작위 위치 (메테오 샤워용)
    ReadFromBlackboard,  // (미래용) 마우스 클릭 등 외부에서 이미 세팅한 값 유지
    NearestEnemy,        // 가장 가까운 적 하나
    MultipleEnemies      // 범위 내 여러 명의 적
}

public enum FirePatternType
{
    RegularFan,   // 정해진 간격으로 쏘는 부채꼴 (기존 방식)
    RandomSpread  // 집탄율 범위 내에서 무작위 난사
}