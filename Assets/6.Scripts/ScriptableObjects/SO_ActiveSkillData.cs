using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PhaseSkill
{
    [Header("Phase Data")]
    public float baseDamage;
    public float confficient = 1.0f; // 스킬 레벨 별 계수치
    public float hitDelay = -1.0f;
    public float duration = -1.0f;

    [Header("Damage Data")]
    public DamageData damageData; 

    [Header("Option List")]
    public List<int> bonusOptionList;

    // 오브젝트 풀링 적용 상태라면 굳이 오브젝트 자체를 가질 필요가 없긴함.
    [Header("Skill Prefab")]
    public GameObject skillObject;
    public string objectName;

    [Header("Skill Action")]
    public string skillActionPrefix = "Skills";
    public string skillActionAnimation;
    public string weaponActionAnimation;
    public ActionData actionData; 

    [Header("Skill Sounds")]
    public string soundName; 

    [Header("Spawn Data")]
    public Vector3 spawnPosition;
    public Quaternion spwanQuaternion = Quaternion.identity;


    public void SetDamageData(float baseDamage, float coefficient = 1.0f)
    {
        damageData = new DamageData();
        damageData.Power = baseDamage * coefficient;
    }
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
}
