using System.Collections.Generic;
using UnityEngine;

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

    public virtual Skill CreateSkill() { return SkillFactory.CreateSkill(this); }
}
