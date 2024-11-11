using UnityEngine;



[System.Serializable]
public class SkillData
{
    public int id;
    public int jobID;
    public string keycode;
    public int isPassive;
    public int activation;
}

[System.Serializable]
public class MasterSkillDataGroup
{
    public SkillData[] MasterSkillDataJson;
}


[System.Serializable]
public class ActiveSkillData
{
    public int id;
    public string skillKeycode;
    public int maxLevel;
    public float cooldown;
    public float limitCooldown;
    public float castingTime;
    public int cost;
    public string leadingSkillList;
   
    public int activation;
}


[System.Serializable]
public class PhaseSkillData
{
    public int targetID;
    public int phase;
    public float baseDamage;
    public float coefficient;
    public float hitDelay;
    public float duration;
    public string bonusOptionList;
    public string objectName;

    public int activation;
}

[System.Serializable]
public class ActiveSkillDataGroup
{
    public ActiveSkillData[] ActiveSkillDataJson;
}


[System.Serializable]
public class PhaseSkillDataGroup
{
    public PhaseSkillData[] PhaseSkillDataJson;
}

[System.Serializable]
public class PassiveSkillData
{
    public int id;
    public string skillKeycode;
    public int maxLevel;
    public string bonusOptionList;
    public string enhanceSkillTargetList;
    public string leadingSkillList;
    public float coefficient;
    public int activation;
}

[System.Serializable]
public class PassiveSkillDataGroup
{
    public PassiveSkillData[] PassiveSkillDataJson;
}




public class SkillJsonConverter : MonoBehaviour
{

    [Header("Json Skill Data")]
    public TextAsset skillDataJson;
    public TextAsset activeSkillDataJosn;
    public TextAsset passiveSkillDataJson;


   


    private void Awake()
    {

   
    }

    private void Start()
    {

    }

    public void ConvertSkillData_Skill()
    {
       

    }


}
