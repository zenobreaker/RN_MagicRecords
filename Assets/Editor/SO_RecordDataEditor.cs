using UnityEditor;

[CustomEditor(typeof(SO_RecordData))]
public sealed class SO_RecordDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        TheWheeler.Editor.EditorLocalization.DrawPreview(serializedObject);
        DrawDefaultInspector();
    }
}
