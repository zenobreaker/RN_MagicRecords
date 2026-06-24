using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PhaseSkill
{
    public string phaseName;
    public bool isInstant; // 💡 이 페이즈가 즉시 종료되는지 여부

    [Header("Module")]
    [SerializeReference]
    [SelectImplementationAttribute]
    public List<SkillModule> modules; 

    // 오브젝트 풀링 적용 상태라면 굳이 오브젝트 자체를 가질 필요가 없긴함.
    [Header("Skill Prefab")]
    public GameObject skillObject;
    public string objectName;

    [Header("Skill Sounds")]
    public string soundName; 

    [Header("Spawn Data")]
    public Vector3 spawnPosition;
    
    [SerializeField]
    private Quaternion spwanQuaternion;
    public Quaternion ValidSpawnQuaternion =>
        spwanQuaternion.Equals(new Quaternion(0, 0, 0, 0)) ? Quaternion.identity : spwanQuaternion;

    public PhaseSkill() 
    {
        spwanQuaternion = Quaternion.identity;
    }

    //public void SetDamageData(float baseDamage, float coefficient = 1.0f, int level = 1)
    //{
    //    damageData = new DamageData();
    //    damageData.baseDamage = baseDamage;
    //    damageData.statCoefficient = coefficient;
    //}
}

[CreateAssetMenu(fileName = "SO_SkillData", menuName = "Scriptable Objects/SO_ActiveSkillData")]
public class SO_ActiveSkillData : SO_SkillData
{
    [Header("Skill Settings")]
    [Tooltip("AI용 값이므로 웬만하면 건들지 않는다.")]
    public float range = -1;
    //public int cost; 
    //public float cooldown;
    //public float limitCooldown;
    //public float castingTime;

    [Header("동시 사용 가능 스킬 여부")]
    public bool isConcurrentSkill = false;
    public ActionData actionData;

    [Header("Phase")]
    public List<PhaseSkill> phaseList;

    public SO_ActiveSkillData Clone() => Instantiate(this);

    public float Range => range; 
}
