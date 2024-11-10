using System.Collections.Generic;
using System.IO;
using System.Linq;
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


        private SkillDataJsonAllData skillDataJsonAllData;
        private ActiveSkillDataJsonAllData activeSkillDataJsonAllData;
        private ActiveSkillPhaseJsonAllData activeSkillPhaseJsonAllData;
        private PassiveSkillDataJsonAllData passiveSkillDataJsonAllData;

        private string jsonFilePath = "Assets/98.Datas/skillDataJson.json";

        private Dictionary<JOB_ID, List<ActiveSkillDataJson>> activeSkillTable = new Dictionary<int, List<ActiveSkillDataJson>>();
        private Dictionary<SKILL_ID, List<PhaseSkillData>> activeSkillPhaseTable = new Dictionary<int, List<PhaseSkillData>>();

        [MenuItem("Tools/Skill Data Impoter")]
        public static void OpenWindow()
        {
            GetWindow<SkillDataImporter>("Skill Data Importer");
        }
        private void OnGUI()
        {
            GUILayout.Label("Skill Data Importer", EditorStyles.boldLabel);
            jsonFilePath = EditorGUILayout.TextField("JSON File Path", jsonFilePath);

            if (GUILayout.Button("Load Json Data"))
            {
                LoadJsonData();
            }

            if (GUILayout.Button("Convert Active Skill Data"))
            {

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

            if (!File.Exists(jsonFilePath))
            {
                Debug.LogError("JSON 파일 경로가 잘못되었습니다.");
                return;
            }

            string json = File.ReadAllText(jsonFilePath);
            skillDataJsonAllData = JsonUtility.FromJson<SkillDataJsonAllData>(json);


            string path = "Assets/98.Datas/activeSkillDataJson.json";
            if (!File.Exists(@path))
            {
                Debug.LogError($"Active Skill JSON 파일 경로가 잘못되었습니다.");
                return;
            }

            string activeSkillJson = File.ReadAllText(path);
            activeSkillDataJsonAllData = JsonUtility.FromJson<ActiveSkillDataJsonAllData>(activeSkillJson);


            string paseSkillJsonPath = "Assets/98.Datas/PhaseSkillDataJson.json";

            activeSkillPhaseJsonAllData = JsonUtility.FromJson<ActiveSkillPhaseJsonAllData>(paseSkillJsonPath);

            foreach (SkillDataJson data in skillDataJsonAllData.skillDataJson)
            {
                if (data.isPassive == 1)
                {
                    //   ConvertSkillData_Passive(data);
                }
                else
                {

                    if (activeSkillTable.ContainsKey(data.jobID) == false)
                    {

                    }
                }
            }

        }

        private void ConvertSkillData_Passive(SkillDataJson data)
        {
            if (passiveSkillDataJsonAllData == null)
                return;


        }

        private List<SkillDataJson> LoadSkillDataFromJobID(JOB_ID id)
        {
            return skillDataJsonAllData?.skillDataJson
                .Where(data => data.jobID == id).ToList();
        }


        private void LoadActiveSkillData(SkillDataJson skillData)
        {
            if (activeSkillDataJsonAllData == null)
                return;

            foreach (ActiveSkillDataJson data in activeSkillDataJsonAllData.activeSkillDataJson)
            {
                if (skillData.id != data.id)
                    continue; 

                SO_ActiveSkillData activeSkill = SO_ActiveSkillData.CreateInstance<SO_ActiveSkillData>();

                activeSkill.id = data.id;
                activeSkill.skillName = GetSkillNameKeycode(data.skillKeycode);
                activeSkill.skillDescription = GetSkillDecKeycode(data.skillKeycode);
                
                activeSkill.skillLevel = 1;
                activeSkill.maxLevel = data.maxLevel;
                

            }

        }



        private List<PhaseSkillData> GetPhaseSkillDataList(int skillID)
        {
            //List<PhaseSkillData> list = new List<PhaseSkillData>();
            //foreach (PhaseSkillData phaseData in activeSkillPhaseJsonAllData.PhaseSkillDataJson)
            //{
            //    if (skillID != phaseData.targetID)
            //        continue;

            //    list.Add(phaseData);
            //}

            //return list; 
            return activeSkillPhaseJsonAllData?.PhaseSkillDataJson.
                Where(phaseSkillData => phaseSkillData.targetID == skillID).ToList();
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