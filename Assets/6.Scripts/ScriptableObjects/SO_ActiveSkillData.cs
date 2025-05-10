using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PhaseSkill
{
    [Header("Phase Data")]
    public float baseDamage;
    public float confficient = 1.0f; // ��ų ���� �� ���ġ
    public float hitDelay = -1.0f;
    public float duration = -1.0f;

    [Header("Damage Data")]
    public DamageData damageData; 

    [Header("Option List")]
    public List<int> bonusOptionList;

    // ������Ʈ Ǯ�� ���� ���¶�� ���� ������Ʈ ��ü�� ���� �ʿ䰡 ������.
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
