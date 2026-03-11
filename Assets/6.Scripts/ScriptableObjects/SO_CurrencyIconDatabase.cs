using System.Collections.Generic;
using UnityEngine;

// 인스펙터에서 수정할 수 있도록 직렬화된 구조체
[System.Serializable]
public struct CurrencyIconMapping
{
    public CurrencyType type;
    public Sprite icon;
}

[CreateAssetMenu(fileName = "CurrencyIconDatabase", menuName = "Data/CurrencyIconDatabase")]
public class SO_CurrencyIconDatabase : ScriptableObject
{
    [SerializeField]
    private List<CurrencyIconMapping> iconMappings = new List<CurrencyIconMapping>();

    // 런타임 검색 속도 최적화를 위한 딕셔너리 캐싱
    private Dictionary<CurrencyType, Sprite> iconDict;

    private void Initialize()
    {
        if (iconDict != null) return;

        iconDict = new Dictionary<CurrencyType, Sprite>();
        foreach (var mapping in iconMappings)
        {
            if (!iconDict.ContainsKey(mapping.type))
            {
                iconDict.Add(mapping.type, mapping.icon);
            }
        }
    }

    public Sprite GetIcon(CurrencyType type)
    {
        Initialize();

        if (iconDict.TryGetValue(type, out Sprite icon))
        {
            return icon;
        }

        Debug.LogWarning($"[CurrencyIconDatabase] {type}에 해당하는 아이콘이 없습니다!");
        return null;
    }
}