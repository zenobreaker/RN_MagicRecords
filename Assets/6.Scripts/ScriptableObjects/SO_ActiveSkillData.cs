using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PhaseSkill
{
    public float basePower;
    public float confficient;
    public float hitDelay;
    public float duration;
    public List<int> bonusOptionList;
}

public class SO_ActiveSkillData : SO_SkillData
{
    [Header("Skill Settings")]
    public int cost; 
    public float cooldown;
    public float limitCooldown;
    public float castingTime;

    [Header("Phase")]
    public List<PhaseSkill> phaseList; 
    
    [Header("Skill Prefab")]
    public GameObject skillObject;
}
