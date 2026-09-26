using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SO_PassiveSkillData))]
public class SO_PassiveSkillDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Passive Skill Editor 열기", GUILayout.Height(28f)))
            PassiveSkillEditorWindow.Open((SO_PassiveSkillData)target);
        DrawDefaultInspector();
    }
}
