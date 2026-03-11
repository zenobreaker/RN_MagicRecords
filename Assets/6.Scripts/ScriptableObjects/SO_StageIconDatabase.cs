using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct StageIconMapping
{
    public StageType type;
    public Sprite icon;
}

[CreateAssetMenu(fileName = "StageIconDatabase", menuName = "Data/StageIconDatabase")]
public class SO_StageIconDatabase : ScriptableObject
{
    [SerializeField] private List<StageIconMapping> mappings = new List<StageIconMapping>();
    private Dictionary<StageType, Sprite> dict;

    public Sprite GetIcon(StageType type)
    {
        if (dict == null)
        {
            dict = new Dictionary<StageType, Sprite>();
            foreach (var m in mappings) dict[m.type] = m.icon;
        }

        return dict.TryGetValue(type, out var icon) ? icon : null;
    }
}