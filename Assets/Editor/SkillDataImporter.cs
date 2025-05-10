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
            Common = 0,     // 공용
            Shooter = 1,    // 사수
            MAX = Shooter,
        }


        private MasterSkillDataGroup masterskillDataGroupData;
        private ActiveSkillDataGroup activeSkillDataGroupData;
        private PhaseSkillDataGroup phaseSkillDataGroupData;
        private SkillSpawnDataGroup skillSpawnDataGroupData;
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
                Debug.LogError(" Master Skill Data JSON 파일 경로가 잘못되었습니다.");
                return;
            }

            string masterSkillJson = File.ReadAllText(masterSkillFilePath);
            masterskillDataGroupData = JsonUtility.FromJson<MasterSkillDataGroup>(masterSkillJson);

            if (!File.Exists(activeSkillFilePath))
            {
                Debug.LogError(" Active Skill Data JSON 파일 경로가 잘못되었습니다.");
                return;
            }


            string activeSkillJson = File.ReadAllText(activeSkillFilePath);
            activeSkillDataGroupData = JsonUtility.FromJson<ActiveSkillDataGroup>(activeSkillJson);

            if (!File.Exists(phaseSkillFilePath))
            {
                Debug.LogError(" Phase Skill Data JSON 파일 경로가 잘못되었습니다.");
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
                            // 기존에 있다면 삭제하고 추가 
                            activeSkillTable[job].Remove(activeSkill);
                            activeSkillTable[job].Add(activeSkill);
                        }
                    }

                    // 정렬
                    if (activeSkillTable[job].Count > 0)
                        activeSkillTable[job].Sort((a, b) => a.id.CompareTo(b.id));

                } // if end 

                //TODO: 패시브 추가 

            }// for end 

            Debug.Log("데이터 로드 완료");
        }

    

        // 마스터 스킬 테이블에서 스킬 정보를 ID로 구분 지어서 가져옴 
        private List<SkillData> LoadSkillDataFromJobID(JOB_ID id)
        {
            // 데이터가 null이 아닌지 확인
            if (masterskillDataGroupData == null || masterskillDataGroupData.MasterSkillDataJson == null)
            {
                Debug.LogWarning("Master skill data group or JSON data is null. Returning empty list.");
                return new List<SkillData>();
            }

            // jobID가 id와 일치하는 데이터를 필터링
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
                    // 폴더 생성
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
                        phase.baseDamage = phaseSkill.baseDamage;
                        phase.confficient = phaseSkill.coefficient;
                        phase.SetDamageData(phaseSkill.baseDamage, phaseSkill.coefficient);
                        phase.hitDelay = phaseSkill.hitDelay;
                        phase.duration = phaseSkill.duration;
                        
                        //skillActionAnimation 
                        phase.skillActionPrefix = phaseSkill.skillActionPrefix;
                        phase.skillActionAnimation = phaseSkill.skillActionAnimation;
                        
                        // Skill Object
                        phase.objectName = phaseSkill.objectName;

                        //TODO: skill create pos <= 다른 캐릭터가 생성되면 위치값은 캐릭터에 따라
                        // 다를 수 있으므로 추후에 조정..

                        // Sound
                        phase.soundName = phaseSkill.skillSound;

                        //TODO: bonus 
                        //soSkill.
                        soSkill.phaseList.Add(phase);
                    }

                    assetPath = $"Assets/10.ScriptableObjects/Resources/Skills/{jobPath}/";
                    assetPath = assetPath + $"{data.skillKeycode}.asset";
                    // 이미 존재하는 에셋이 있는 경우 삭제 후 재생성
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

            // 유효한 폴더인지 확인
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.Log("폴더가 이미 존재합니다.");
                return;
            }
            else
            {
                // 경로 상의 각 폴더를 순차적으로 생성
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

                Debug.Log($"폴더가 생성되었습니다: {folderPath}");

                AssetDatabase.Refresh();
            }
        }

       
    }  // class end 

}// namespace end; 



//[MenuItem("Tools/Import Skills from CSV")]
//static void ImportSkillsFromCSV()
//{
//    string path = "Assets/SkillData.csv"; // CSV 파일 경로 설정
//    string[] csvLines = File.ReadAllLines(path);

//    foreach (var line in csvLines)
//    {
//        string[] values = line.Split(',');

//        // ScriptableObject 생성
//        SO_SkillData skillData = ScriptableObject.CreateInstance<SO_SkillData>();
//        skillData.id = int.Parse(values[0]);
//        skillData.skillName = values[1];
//        skillData.skillDescription = values[2];
//        // 기타 속성 설정 ...

//        AssetDatabase.CreateAsset(skillData, $"Assets/Skills/{skillData.skillName}.asset");
//    }
//    AssetDatabase.SaveAssets();
//}


//[MenuItem("Tools/Import Skills from json")]