using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_ChapterData", menuName = "Scriptable Objects/SO_ChapterData")]
public class SO_ChapterData : ScriptableObject
{
    public int chapterIndex; // 1챕터, 2챕터...
    public string chapterName;

    [Header("등장 가능한 테마 리스트")]
    public List<SO_Biome> possibleBiomes = new List<SO_Biome>();

    /// <summary>
    /// 챕터 시작 시 호출하여 랜덤하게 테마 하나를 결정합니다.
    /// </summary>
    public SO_Biome GetRandomBiome()
    {
        if (possibleBiomes.Count == 0) return null;
        int randomIndex = Random.Range(0, possibleBiomes.Count);
        return possibleBiomes[randomIndex];
    }
}
