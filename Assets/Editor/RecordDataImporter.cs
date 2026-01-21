using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;

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

            // 2. DTO 역직렬화
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
                // ID 일치 여부 확인
                var data = dataGroup.recordDataJson.Find(d => d.id == info.id);
                if (data == null)
                {
                    Debug.LogWarning($"[Importer] ID {info.id}에 해당하는 데이터가 DataJson에 없어 건너뜁니다.");
                    continue;
                }

                // ScriptableObject 인스턴스 생성
                SO_RecordData so = CreateInstance<SO_RecordData>();

                // --- Info 데이터 할당 ---
                so.id = info.id;
                so.recordName = info.namekeycode;
                so.description = info.description;
                // so.icon = Resources.Load<Sprite>(info.iconPath); // 필요 시 활성화

                // --- Data 데이터 할당 (Enum 캐스팅 포함) ---
                // recordType과 calcType은 int로 들어오므로 직접 캐스팅
                so.type = (RecordType)data.recordType;
                so.valueType = (ModifierValueType)data.calcType;
                so.rarity = (RecordRarity)data.rarity;
                so.effectValue = data.value;
                so.className = data.className;

                // stat과 targetFilter는 string으로 들어올 수 있으므로 Enum.TryParse 활용
                if (System.Enum.TryParse(data.stat, out StatusType sType)) so.status = sType;
                if (System.Enum.TryParse(data.targetFilter, out TargetFilterType tFilter)) so.targetFilter = tFilter;

                // 5. 에셋 저장 경로 설정
                string fileName = $"{info.id}_{info.namekeycode}.asset";
                string fullPath = $"Assets/10.ScriptableObjects/{saveFolderName}/{fileName}";

                // 기존 에셋 존재 시 교체
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
            string folderPath = "10.ScriptableObjects/Resources/Skills/" + path;
            string currentPath = "Assets";

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