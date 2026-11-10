using UnityEditor;

[CustomEditor(typeof(SO_RecordData))]
public sealed class SO_RecordDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SkillRecordIconPreview.DrawHeader(serializedObject, Repaint);
        DrawDefaultInspector();
    }
}
