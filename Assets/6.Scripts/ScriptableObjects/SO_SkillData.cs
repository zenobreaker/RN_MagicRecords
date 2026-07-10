using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class SkillLevelData
{
    //[Header("Cost")]
    //public int requiredGold;
    //public int requiredSkillPoint;
    public float range;

    public float limitMinCooldown; 
    public float cooldown;
    public float castingTime = -1.0f;
    public float chargeTime = 0.0f; 
    public DamageData  damageData;
    public List<int> bonusOptionList;

    public int spawnCount;
    public float angle;
}

[CreateAssetMenu(fileName = "SO_SkillData", menuName = "Scriptable Objects/SO_SkillData")]
public class SO_SkillData : ScriptableObject
{
    [Header("Skill Info")]
    public int id;
    public string skillName;
    public string skillDescription;
    public int learnableLevel; 
    public int maxLevel;
    public int[] skillUpgradeCost;
    public Sprite skillImage;
    //TODO : 캐스팅 중 무빙 가능하다면 별도의 프로퍼티 추가 
    // public bool stopAgentOnCast = true; 
    [Header("Skill Leading Skill ID's")]
    public List<int> leadingSkillList;

    [Header("Skill Level ")]
    public List<SkillLevelData> levelDatas;

    public virtual Skill CreateSkill() { return SkillFactory.CreateSkill(this); }

    public float GetCooldown()
    {
        if(levelDatas.Count <= 0) return 0.0f;

        return levelDatas.First().cooldown;
    }
}
