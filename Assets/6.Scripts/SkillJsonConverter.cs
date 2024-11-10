using UnityEngine;



[System.Serializable]
public class SkillDataJson
{
    public int id;
    public int jobID;
    public string keycode;
    public string objectName;
    public int isPassive;
    public int activation;
}

[System.Serializable]
public class SkillDataJsonAllData
{
    public SkillDataJson[] skillDataJson;
}


[System.Serializable]
public class ActiveSkillDataJson
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

    public int activation;
}

[System.Serializable]
public class ActiveSkillDataJsonAllData
{
    public ActiveSkillDataJson[] activeSkillDataJson;
}


[System.Serializable]
public class ActiveSkillPhaseJsonAllData
{
    public PhaseSkillData[] PhaseSkillDataJson;
}

[System.Serializable]
public class PassiveSkillDataJson
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
public class PassiveSkillDataJsonAllData
{
    public PassiveSkillDataJson[] passiveSkillDataJson;
}




public class SkillJsonConverter : MonoBehaviour
{

    [Header("Json Skill Data")]
    public TextAsset skillDataJson;
    public TextAsset activeSkillDataJosn;
    public TextAsset passiveSkillDataJson;


    private SkillDataJsonAllData skillDataJsonAllData;
    private ActiveSkillDataJsonAllData activeSkillDataJsonAllData;
    private PassiveSkillDataJsonAllData passiveSkillDataJsonAllData;


    private void Awake()
    {

        skillDataJsonAllData = JsonUtility.FromJson<SkillDataJsonAllData>(skillDataJson.text);
        activeSkillDataJsonAllData =
            JsonUtility.FromJson<ActiveSkillDataJsonAllData>(activeSkillDataJosn.text);
        passiveSkillDataJsonAllData =
            JsonUtility.FromJson<PassiveSkillDataJsonAllData>(passiveSkillDataJson.text);
    }

    private void Start()
    {

    }

    public void ConvertSkillData_Skill()
    {
        if (skillDataJson == null || skillDataJsonAllData == null)
            return;

        // 알지 알지.. 
        foreach (SkillDataJson data in skillDataJsonAllData.skillDataJson)
        {
            if (data.isPassive == 1)
                ConvertSkillData_Passive(data);
            else
                ConvertSkillData_Active(data);
        }

    }

    private void ConvertSkillData_Passive(SkillDataJson data)
    {
        if (passiveSkillDataJsonAllData == null)
            return;

        
    }

    private void ConvertSkillData_Active(SkillDataJson data)
    {
        if (activeSkillDataJsonAllData == null)
            return;

        foreach (ActiveSkillDataJson active in activeSkillDataJsonAllData.activeSkillDataJson)
        {
            SO_ActiveSkillData activeSkill = ScriptableObject.CreateInstance<SO_ActiveSkillData>();
        }
    }

}
