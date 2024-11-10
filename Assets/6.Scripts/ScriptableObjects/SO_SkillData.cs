using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_SkillData", menuName = "Scriptable Objects/SO_SkillData")]
public class SO_SkillData : ScriptableObject
{
    public int id;
    public string skillName;
    public string skillDescription;
    public int skillLevel;
    public int maxLevel;
    public int[] skillUpgradeCost;
    public Sprite skillImage;

    [Header("Skill Leading Skill ID's")]
    public List<int> leadingSkillList;
    //public float basePower;
    //public float confficient; 
}
