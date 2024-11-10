using System.Collections.Generic;
using UnityEngine;

public class SO_ActiveSkillData : SO_SkillData
{
    [Header("Skill Settings")]
    public float basePower;
    public float confficient;
    public float hitDelay;
    public float duration;
    public int cost; 
    public float cooldown;
    public float limitCooldown;
    public float castingTime;
    public List<int> bonusOptionList;


    [Header("Skill Prefab")]
    public GameObject prefab;
}
