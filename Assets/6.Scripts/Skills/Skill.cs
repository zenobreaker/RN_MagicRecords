using UnityEngine;

public abstract class Skill 
{
    protected int skillID;
    public int SkillID {  get { return skillID; } }
    protected string skillName;
    public string Name { get { return skillName; } }
    protected string skillDescription;
    public string Description { get { return skillDescription; } }
    protected int skillLevel;
    public int SkillLevel { get { return skillLevel; } }

    public Sprite Icon { get; private set; }

    public Skill (int id, string name, string desc, Sprite icon)
    {
        skillID = id;
        skillName = name;
        skillDescription = desc;
        Icon = icon;
    }

    public Skill(SO_SkillData skillData)
        : this(skillData.id, skillData.skillName, skillData.skillDescription, skillData.skillImage)
    {

    }

    public void SetLevel(int level) { skillLevel = level; }
}
