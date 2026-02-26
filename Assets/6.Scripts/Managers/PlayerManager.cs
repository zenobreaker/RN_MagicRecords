using System.Collections.Generic;
using UnityEngine;

public class PlayerManager :
    Singleton<PlayerManager>
{
    public SO_PlayerObjects playerObjects; 
    public SO_JobData jobdata;

    private List<int> characterIds = new List<int>();
    // key : char id
    private Dictionary<int, CharStatusData> charStatusDatas = new();
    private Dictionary<int, int> charClass = new();
    private Dictionary<int, CharEquipmentData> charEquipments = new();
    private Dictionary<int, int> charEquippedSkills = new();

    private bool isDirty = false;
    public bool IsDirty => isDirty;

    private Dictionary<int, Player> currentPlayerDicts = new Dictionary<int, Player>();

    public void SetCurrentPlayer(Player player)
    {
        currentPlayerDicts[player.CharID] = player;
    }

    //TODO : 가장 ID가 적은 녀석부터 처리
    public Player GetCurrentPlayer(int charID = 0)
    {
        foreach (KeyValuePair<int, Player> p in currentPlayerDicts)
        {
            return p.Value;
        }

        return null;
    }

    public void SetDirty()
    {
        isDirty = true;
        Debug.Log($"PlayerManager Dirty On");
    }

    public void OnInit()
    {
        charStatusDatas.Clear();
        charStatusDatas.Add(1, new TurtleInfoData(1, 1));

        charClass.Clear();
        characterIds.Clear();
        charEquipments.Clear();
        charEquippedSkills.Clear();

        var loadInfo = SaveManager.LoadCharInfoData();
        if (loadInfo != null)
        {
            foreach (var info in loadInfo.charInfoList)
            {
                characterIds.Add(info.charId);
                charClass.Add(info.charId, info.classID);
                var charEquipment = CreateCharEquipmentData(info.charId, info.equippedItemIds);
                charEquipments.Add(info.charId, charEquipment);

                // Inventory manager에게 해당 장비를 장착했다고 알려줌
                ManagerWaiter.WaitForManager<InventoryManager>((inventory) =>
                {
                    inventory.EquipItem(info.charId, charEquipment);
                });

                ManagerWaiter.WaitForManager<AppManager>((app) =>
                {
                    app.EquipSavedClassActiveSkill(info.classID, info.equippedSkillIds);
                });

            }
        }
        else
        {
            // 임시적으로 리스트에 추가
            characterIds.Add(1);
            charClass.Add(1, 1);
            charEquipments.Add(1, new CharEquipmentData { characterId = 1, });
            foreach (var ce in charEquipments)
            {
                ce.Value.Init();
            }
        }

        playerObjects?.Init(); 
        jobdata?.Init(); 

        ManagerWaiter.WaitForManager<InventoryManager>((inventory) =>
        {
            inventory.OnDataChanged += SetDirty;
        });

        ManagerWaiter.WaitForManager<SkillTreeManager>((tree) =>
        {
            tree.OnDataChanged += SetDirty;
        });
    }

    protected override void SyncDataFromSingleton()
    {
        base.SyncDataFromSingleton();

        characterIds = Instance.characterIds;
        charEquipments = Instance.charEquipments;
        charEquipments = Instance.charEquipments;
    }

    public CharEquipmentData GetCharEquipmentData(int charId)
    {
        charEquipments.TryGetValue(charId, out CharEquipmentData ce);
        return ce;
    }

    public CharStatusData GetCharacterStatus(int charId)
    {
        charStatusDatas.TryGetValue(charId, out CharStatusData ce);
        return ce;
    }

    private CharEquipmentData CreateCharEquipmentData(int charId, List<string> equipementIds)
    {
        if (equipementIds.Count == 0) return new CharEquipmentData { characterId = charId };

        CharEquipmentData newData = new CharEquipmentData();
        newData.Init();
        newData.characterId = charId;

        int idx = 0;
        foreach (var equipId in equipementIds)
        {
            newData.equipments[idx++].itemUniqueId = equipId;
        }

        return newData;
    }

    public CharacterInfo GetCharacterInfo(int charID)
    {
        if(playerObjects == null) return null;

        return playerObjects.GetPlayerInfo(charID);
    }

    public JobInfo GetJobInfo(int jobID)
    {
        if (jobdata == null) return null; 

        return jobdata.GetJobInfo(jobID);
    }

    public void SaveIfDirty()
    {
        if (isDirty == false) return;

        CharInfoListData listData = new();
        foreach (var info in characterIds)
        {
            CharacterSaveData newData = new()
            {
                charId = info,
                charLevel = charStatusDatas[info].level,
                classID = charClass.TryGetValue(info, out var value) ? value : 1,
                equippedItemIds = charEquipments[info].GetEquippedItemIDs(),
                equippedSkillIds = AppManager.Instance.GetEquippedActiveSkillIDListByCharID(info)
            };

            listData.charInfoList.Add(newData);
        }

        SaveManager.SaveCharacter(listData);
        isDirty = false;
    }
}
