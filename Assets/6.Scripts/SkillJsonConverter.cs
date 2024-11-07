using UnityEngine;



[System.Serializable]
public class SkillDataJson
{
    public int id;
    public int userID;
    public string keycode;
    public string callName;
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
public class ActiveSKillDataJson
{
    public int id;
    public string skillKeycode;
    public int maxLevel;
    public float baseDamage;
    public float coefficient;
    public int hitCount;
    public int cost;
    public float baseCoolTime;
    public float decreaseTimeValue;
    public string bonusOptionList;
    public string bonusSpecialOptionList;
    public string leadingSkillList;
    public int activation;
}

[System.Serializable]
public class ActiveSkillDataJsonAllData
{
    public ActiveSKillDataJson[] activeSkillDataJson;
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


    private void Start()
    {
        
    }

    public void ConvertSkillData_Skill()
    {
        if (skillDataJson == null)
            return; 


        skillDataJsonAllData = JsonUtility.FromJson<SkillDataJsonAllData>(skillDataJson.text);
        if (skillDataJsonAllData == null)
            return;



    }

    public void ConvertSkillData_Passive()
    {
        passiveSkillDataJsonAllData = JsonUtility.FromJson<PassiveSkillDataJsonAllData>(passiveSkillDataJson.text);

    }

    public void ConvertSkillData_Active()
    {

        activeSkillDataJsonAllData = JsonUtility.FromJson<ActiveSkillDataJsonAllData>(activeSkillDataJosn.text);
    }


}
