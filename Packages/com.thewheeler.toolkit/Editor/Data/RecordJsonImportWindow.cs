using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheWheeler.Editor
{
    public sealed class RecordJsonImportWindow : EditorWindow
    {
        private string jsonFolder = RecordJsonImporter.DefaultJsonFolder;
        private string assetFolder = RecordJsonImporter.DefaultAssetFolder;
        private bool registerAddressables = true;
        private bool bindCatalogue = true;
        private string prefabPath = "Assets/5.Prefabs/AppManager_Instance.prefab";
        private string result;

        [MenuItem("Tools/TheWheeler/Data/Record JSON Importer")]
        public static void Open()
        {
            GetWindow<RecordJsonImportWindow>("Record JSON Importer");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Record JSON → ScriptableObjects", EditorStyles.boldLabel);
            jsonFolder = EditorGUILayout.TextField("JSON folder", jsonFolder);
            assetFolder = EditorGUILayout.TextField("Record asset folder", assetFolder);
            registerAddressables = EditorGUILayout.Toggle("Register RecordData label", registerAddressables);
            bindCatalogue = EditorGUILayout.Toggle("Update record catalogue", bindCatalogue);
            if (bindCatalogue)
            {
                prefabPath = EditorGUILayout.TextField("Catalogue prefab", prefabPath);
            }

            EditorGUILayout.HelpBox("Matches existing assets by ID and preserves their GUIDs. Missing records are created, including pending effect definitions. Existing common IDs 1–7 retain their identity and icons.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Import JSON and create/update records"))
                {
                    try
                    {
                        var report = RecordJsonImporter.Import(jsonFolder, assetFolder);
                        if (registerAddressables)
                        {
                            RecordJsonImporter.RegisterAddressables(report.paths);
                        }

                        if (bindCatalogue)
                        {
                            RecordJsonImporter.BindPrefab(prefabPath, report.paths);
                        }

                        result = JsonUtility.ToJson(report, true);
                        Debug.Log(result);
                    }
                    catch (Exception e)
                    {
                        result = e.Message;
                        Debug.LogException(e);

                    }
                }
            if (string.IsNullOrEmpty(result) == false)
            {
                EditorGUILayout.HelpBox(result, MessageType.None);
            }
        }
        public static void ImportDefault()
        {
            var report = RecordJsonImporter.Import();
            RecordJsonImporter.RegisterAddressables(report.paths);
            RecordJsonImporter.BindPrefab("Assets/5.Prefabs/AppManager_Instance.prefab", report.paths);
            Directory.CreateDirectory("Library/RecordImport");
            File.WriteAllText("Library/RecordImport/result.json", JsonUtility.ToJson(report, true));
            Debug.Log($"Record import: {report.created} created, {report.updated} updated, {report.pending} pending.");
        }
    }

    // Never imports on startup automatically; an explicit local request is required.
    [InitializeOnLoad]
    internal static class RecordJsonImportRequest
    {
        const string Request = "Library/TheWheelerRecordImport.request";
        static RecordJsonImportRequest()
        {
            EditorApplication.update += Tick;
        }
        static void Tick()
        {
            if (File.Exists(Request) == false || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            try
            {
                File.ReadAllText(Request);
                File.Delete(Request);
            }
            catch (IOException)
            {
                return;

            }
            try
            {
                RecordJsonImportWindow.ImportDefault();
            }
            catch (Exception e)
            {
                Directory.CreateDirectory("Library/RecordImport");
                File.WriteAllText("Library/RecordImport/error.txt", e.ToString());
                Debug.LogException(e);
            }
        }
    }
}
