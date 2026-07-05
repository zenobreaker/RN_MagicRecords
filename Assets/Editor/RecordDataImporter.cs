using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UserEditor
{
    // JSON 그룹 데이터를 담기 위한 래퍼 클래스
    [System.Serializable]
    public class RecordDataAllData
    {
        public List<RecordDataJson> recordDataJson;
    }

    public class RecordDataImporter : EditorWindow
    {
        private string infoFilePath = "Assets/98.Datas/RecordInfoJson.json";
        private string dataFilePath = "Assets/98.Datas/RecordDataJson.json";
        private string saveFolderName = "Records";

        [MenuItem("Tools/Record Data Importer")]
        public static void OpenWindow()
        {
            GetWindow<RecordDataImporter>("Record Data Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Record Data Importer", EditorStyles.boldLabel);
            infoFilePath = EditorGUILayout.TextField("Info JSON Path", infoFilePath);
            dataFilePath = EditorGUILayout.TextField("Data JSON Path", dataFilePath);
            saveFolderName = EditorGUILayout.TextField("Save Folder Name", saveFolderName);

            GUILayout.Space(10);

            if (GUILayout.Button("Import and Create Record SO", GUILayout.Height(30)))
            {
                ImportRecords();
            }
        }

        private void ImportRecords()
        {
            if (!File.Exists(infoFilePath) || !File.Exists(dataFilePath))
            {
                Debug.LogError("[Importer] JSON 파일 경로를 찾을 수 없습니다.");
                return;
            }

            // 1. JSON 읽기
            string infoRaw = File.ReadAllText(infoFilePath);
            string dataRaw = File.ReadAllText(dataFilePath);

            RecordInfoAllData infoGroup = JsonUtility.FromJson<RecordInfoAllData>(infoRaw);
            RecordDataAllData dataGroup = JsonUtility.FromJson<RecordDataAllData>(dataRaw);

            if (infoGroup == null || infoGroup.recordInfoJson == null || dataGroup == null || dataGroup.recordDataJson == null)
            {
                Debug.LogError("[Importer] JSON 데이터 파싱에 실패했습니다.");
                return;
            }

            // 3. 폴더 생성
            CreateFolder(saveFolderName);

            int importCount = 0;

            // 4. 데이터 결합 및 SO 생성
            foreach (var info in infoGroup.recordInfoJson)
            {
                var data = dataGroup.recordDataJson.Find(d => d.id == info.id);
                if (data == null) continue;

                // ScriptableObject 인스턴스 생성
                SO_RecordData so = CreateInstance<SO_RecordData>();

                // --- Info 데이터 할당 ---
                so.id = info.id;
                so.recordName = info.namekeycode;
                so.description = info.description;

                // --- Data 데이터 할당 ---
                so.type = (RecordType)data.recordType;
                so.rarity = (RecordRarity)data.rarity;
                // TargetFilter 처리 생략 (필요 시 Enum 파싱 추가)

                // 리스트 초기화 (SO 덮어쓰기 버그 방지)
                so.Stats = new List<RecordStatData>();

                // 💡 [핵심 수정] stats 파싱
                if (data.stats != null)
                {
                    foreach (var stat in data.stats)
                    {
                        if (Enum.TryParse(stat.stat, out StatusType parsedStat))
                        {
                            so.Stats.Add(new RecordStatData
                            {
                                Status = parsedStat,
                                ValueType = (ModifierValueType)stat.calcType,
                                Value = stat.value
                            });
                        }
                    }
                }

             
                // 5. 에셋 저장
                string fileName = $"{info.id}_{info.namekeycode}.asset";
                string fullPath = $"Assets/10.ScriptableObjects/{saveFolderName}/{fileName}";

                SO_RecordData existingAsset = AssetDatabase.LoadAssetAtPath<SO_RecordData>(fullPath);
                if (existingAsset != null)
                {
                    EditorUtility.CopySerialized(so, existingAsset);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    AssetDatabase.CreateAsset(so, fullPath);
                }

                importCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Importer] 총 {importCount}개의 레코드 데이터 임포트 완료!");
        }

        public void CreateFolder(string path)
        {
            string folderPath = path; // 수정: 입력받은 폴더명 그대로 생성
            string currentPath = "Assets/10.ScriptableObjects";

            foreach (string folder in folderPath.Split('/'))
            {
                string tempPath = Path.Combine(currentPath, folder);
                if (!AssetDatabase.IsValidFolder(tempPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folder);
                }
                currentPath = tempPath;
            }
        }
    }
}