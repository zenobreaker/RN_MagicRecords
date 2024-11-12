using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

using JOB_ID = System.Int32;
using SKILL_ID = System.Int32;

namespace UserEditor
{
    public class SkillDataImporter : EditorWindow
    {
        public enum JobType
        {
            Common = 0,     // ����
            Shooter = 1,    // ���
            MAX = Shooter,
        }


        private MasterSkillDataGroup masterskillDataGroupData;
        private ActiveSkillDataGroup activeSkillDataGroupData;
        private PhaseSkillDataGroup phaseSkillDataGroupData;
        private PassiveSkillDataGroup passiveSkillDataGroupData;

        private string masterSkillFilePath = "Assets/98.Datas/MasterSkillDataJson.json";
        private string activeSkillFilePath = "Assets/98.Datas/ActiveSkillDataJson.json";
        private string phaseSkillFilePath = "Assets/98.Datas/PhaseSkillDataJson.json";

        private Dictionary<JOB_ID, List<ActiveSkillData>> activeSkillTable = new Dictionary<int, List<ActiveSkillData>>();
        private Dictionary<SKILL_ID, List<PhaseSkillData>> activeSkillPhaseTable = new Dictionary<int, List<PhaseSkillData>>();

        [MenuItem("Tools/Skill Data Impoter")]
        public static void OpenWindow()
        {
            GetWindow<SkillDataImporter>("Skill Data Importer");
        }
        private void OnGUI()
        {
            GUILayout.Label("Skill Data Importer", EditorStyles.boldLabel);
            masterSkillFilePath = EditorGUILayout.TextField("JSON File Path", masterSkillFilePath);

            if (GUILayout.Button("Load Json Data"))
            {
                LoadJsonData();
            }

            if (GUILayout.Button("Clear All Skill Data"))
            {
                activeSkillTable.Clear();
                activeSkillPhaseTable.Clear();
            }

            if (GUILayout.Button("Convert Active Skill Data"))
            {
                //foreach(var pair in activeSkillTable)
                //{
                //    if (pair.Value.Count <= 0)
                //    { continue; }   

                //    foreach(var p2 in pair.Value)
                //    {
                //        Debug.Log(p2.skillKeycode);
                //    }
                //}

                Convert_SkillData();
            }
            if (GUILayout.Button("Create Folder"))
            {
                CreateFolder("test");
            }
        }

        private string GetSkillNameKeycode(string skillKeycode)
            => "name_" + skillKeycode;

        private string GetSkillDecKeycode(string skillKeycode)
            => "desc_" + skillKeycode;

        private string GetSkillImageKeycode(string skillKeycode)
            => "img_" + skillKeycode;


        private void LoadJsonData()
        {

            if (!File.Exists(masterSkillFilePath))
            {
                Debug.LogError(" Master Skill Data JSON ���� ��ΰ� �߸��Ǿ����ϴ�.");
                return;
            }

            string masterSkillJson = File.ReadAllText(masterSkillFilePath);
            masterskillDataGroupData = JsonUtility.FromJson<MasterSkillDataGroup>(masterSkillJson);

            if (!File.Exists(activeSkillFilePath))
            {
                Debug.LogError(" Active Skill Data JSON ���� ��ΰ� �߸��Ǿ����ϴ�.");
                return;
            }


            string activeSkillJson = File.ReadAllText(activeSkillFilePath);
            activeSkillDataGroupData = JsonUtility.FromJson<ActiveSkillDataGroup>(activeSkillJson);

            if (!File.Exists(phaseSkillFilePath))
            {
                Debug.LogError(" Phase Skill Data JSON ���� ��ΰ� �߸��Ǿ����ϴ�.");
                return;
            }

            string phaseSkillJson = File.ReadAllText(phaseSkillFilePath);
            phaseSkillDataGroupData = JsonUtility.FromJson<PhaseSkillDataGroup>(phaseSkillJson);


            for (int job = (int)JobType.Common; job <= (int)JobType.MAX; job++)
            {
                var list = LoadSkillDataFromJobID(job);
                if (list == null)
                    continue;
                
                // Active 
                if (activeSkillTable.ContainsKey(job) == false)
                {
                    List<ActiveSkillData> activeList = new List<ActiveSkillData>();
                    activeSkillTable.Add(job, activeList);

                    foreach (SkillData skillDataJson in list)
                    {
                        ActiveSkillData activeSkill = LoadActiveSkillData(skillDataJson.id);
                        if (activeSkill == null)
                            continue; 

                        if (activeSkillTable[job].Contains(activeSkill) == false)
                            activeSkillTable[job].Add(activeSkill);
                        else
                        {
                            // ������ �ִٸ� �����ϰ� �߰� 
                            activeSkillTable[job].Remove(activeSkill);
                            activeSkillTable[job].Add(activeSkill);
                        }
                    }

                    // ����
                    if (activeSkillTable[job].Count > 0)
                        activeSkillTable[job].Sort((a, b) => a.id.CompareTo(b.id));

                } // if end 

                //TODO: �нú� �߰� 

            }// for end 

            Debug.Log("������ �ε� �Ϸ�");
        }

    

        // ������ ��ų ���̺��� ��ų ������ ID�� ���� ��� ������ 
        private List<SkillData> LoadSkillDataFromJobID(JOB_ID id)
        {
            // �����Ͱ� null�� �ƴ��� Ȯ��
            if (masterskillDataGroupData == null || masterskillDataGroupData.MasterSkillDataJson == null)
            {
                Debug.LogWarning("Master skill data group or JSON data is null. Returning empty list.");
                return new List<SkillData>();
            }

            // jobID�� id�� ��ġ�ϴ� �����͸� ���͸�
            return masterskillDataGroupData.MasterSkillDataJson
                .Where(data => data.jobID == id)
                .ToList();
        }

        private ActiveSkillData LoadActiveSkillData(SKILL_ID id)
        {
            if (activeSkillDataGroupData == null)
                return null;

           
            foreach (ActiveSkillData data in activeSkillDataGroupData.ActiveSkillDataJson)
            {
                if (id != data.id)
                    continue;

                //Phase 
                if (activeSkillPhaseTable.ContainsKey(id) == false)
                {
                    activeSkillPhaseTable.Add(id, GetPhaseSkillDataList(id));
                }

                return data;
            }

            return null;
        }

        private List<PhaseSkillData> GetPhaseSkillDataList(int skillID)
        {
            return phaseSkillDataGroupData?.PhaseSkillDataJson.
                Where(phaseSkillData => phaseSkillData.targetID == skillID).ToList();
        }

        private void Convert_SkillData()
        {
            for(int job = (int)JobType.Common; job <= (int)JobType.MAX; job++)
            {
                //Active 
                Convert_ActiveSkill(job);

                //TODO : Passive 
            }
        }

        private void Convert_ActiveSkill(JOB_ID jobID)
        {
            foreach (KeyValuePair<JOB_ID, List<ActiveSkillData>> jobAndSkillPair in activeSkillTable)
            {
                if (jobAndSkillPair.Key != jobID)
                    continue; 

                if(jobAndSkillPair.Value.Count <= 0)
                    continue;

                // Create Scriptable Object
                string assetPath = "";
                string jobPath = "";
                {
                    jobPath = ((JobType)jobID).ToString();
                    // ���� ����
                    CreateFolder(jobPath);
                   
                }

                foreach (ActiveSkillData data in  jobAndSkillPair.Value)
                {
                    SO_ActiveSkillData soSkill = SO_ActiveSkillData.CreateInstance<SO_ActiveSkillData>();

                    List<PhaseSkillData> phaseList = activeSkillPhaseTable[data.id];
                    if (phaseList == null)
                        continue;

                    soSkill.id = data.id;
                    soSkill.skillDescription = GetSkillDecKeycode(data.skillKeycode);
                    soSkill.skillName = GetSkillNameKeycode(data.skillKeycode);
                    soSkill.skillLevel = 1;
                    soSkill.maxLevel = data.maxLevel;
                    soSkill.cooldown = data.cooldown;
                    soSkill.limitCooldown = data.limitCooldown;
                    soSkill.castingTime = data.castingTime;
                    soSkill.cost = data.cost;

                    //TODO: leading Skill

                    soSkill.phaseList = new List<PhaseSkill>();
                    foreach (PhaseSkillData phaseSkill in phaseList)
                    {
                        PhaseSkill phase = new PhaseSkill();
                        phase.basePower = phaseSkill.baseDamage;
                        phase.confficient = phaseSkill.coefficient;
                        phase.hitDelay = phaseSkill.hitDelay;
                        phase.duration = phaseSkill.duration;

                        //TODO: Skill Object

                        //TODO: bonus 
                        //soSkill.
                        soSkill.phaseList.Add(phase);
                    }
                    assetPath = $"Assets/10.ScriptableObjects/Resources/Skills/{jobPath}/";
                    assetPath = assetPath + $"{data.skillKeycode}.asset";
                    // �̹� �����ϴ� ������ �ִ� ��� ���� �� �����
                    var existingAsset = AssetDatabase.LoadAssetAtPath<SO_ActiveSkillData>(assetPath);
                    if (existingAsset != null)
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }

                    AssetDatabase.CreateAsset(soSkill, assetPath);
                    Debug.Log("Active Skills imported from JSON");
                } //   end   foreach (ActiveSkillData data 
            } //  end  foreach (KeyValuePair<JOB_ID, List<ActiveSkillData>>  

            AssetDatabase.Refresh();
        }


        private void ConvertSkillData_Passive(SkillData data)
        {


        }


        public void CreateFolder(string path)
        {
            string folderPath = "10.ScriptableObjects/Resources/Skills/" + path;

            // ��ȿ�� �������� Ȯ��
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.Log("������ �̹� �����մϴ�.");
                return;
            }
            else
            {
                // ��� ���� �� ������ ���������� ����
                string[] splitPath = folderPath.Split('/');
                string currentPath = "Assets";

                foreach (string folder in splitPath)
                {
                    string tempPath = Path.Combine(currentPath, folder);
                    if (!AssetDatabase.IsValidFolder(tempPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folder);
                    }
                    currentPath = tempPath;
                }

                Debug.Log($"������ �����Ǿ����ϴ�: {folderPath}");

                AssetDatabase.Refresh();
            }
        }

       
    }  // class end 

}// namespace end; 



//[MenuItem("Tools/Import Skills from CSV")]
//static void ImportSkillsFromCSV()
//{
//    string path = "Assets/SkillData.csv"; // CSV ���� ��� ����
//    string[] csvLines = File.ReadAllLines(path);

//    foreach (var line in csvLines)
//    {
//        string[] values = line.Split(',');

//        // ScriptableObject ����
//        SO_SkillData skillData = ScriptableObject.CreateInstance<SO_SkillData>();
//        skillData.id = int.Parse(values[0]);
//        skillData.skillName = values[1];
//        skillData.skillDescription = values[2];
//        // ��Ÿ �Ӽ� ���� ...

//        AssetDatabase.CreateAsset(skillData, $"Assets/Skills/{skillData.skillName}.asset");
//    }
//    AssetDatabase.SaveAssets();
//}


//[MenuItem("Tools/Import Skills from json")]