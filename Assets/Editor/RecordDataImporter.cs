using System.Collections.Generic;
using UnityEditor;

namespace UserEditor
{
    [System.Serializable]
    public class RecordDataAllData
    {
        public List<RecordDataJson> recordDataJson;
    }

    // Legacy menu redirect; implementation belongs to TheWheeler.Editor.
    public class RecordDataImporter : EditorWindow
    {
        [MenuItem("Tools/Record Data Importer")]
        public static void OpenWindow() => TheWheeler.Editor.RecordJsonImportWindow.Open();
    }
}