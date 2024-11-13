using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PhaseSkill
{
    [Header("Phase Data")]
    public float basePower;
    public float confficient;
    public float hitDelay;
    public float duration;
    public List<int> bonusOptionList;

    // 오브젝트 풀링 적용 상태라면 굳이 오브젝트 자체를 가질 필요가 없긴함.
    [Header("Skill Prefab")]
    public GameObject skillObject;
    public string objectName;

    [Header("Spawn Data")]
    public Vector3 spawnPosition;
    public Quaternion spwanQuaternion = Quaternion.identity;
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
