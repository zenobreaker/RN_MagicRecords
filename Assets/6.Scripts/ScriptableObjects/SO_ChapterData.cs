using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[Serializable]
public sealed class ChapterData
{
    public int chapterNumber;

    public string chapterName;

    [Header("등장 가능한 테마")]
    public List<SO_Theme> possibleThemes = new();
}


[CreateAssetMenu(fileName = "SO_ChapterData", menuName = "Scriptable Objects/SO_ChapterData")]
public class SO_ChapterData : ScriptableObject
{
    [SerializeField]
    private List<ChapterData> chapters = new();

    private Dictionary<string, SO_Theme> themeDict = new(); 

    public void Init()
    {
        foreach (var chapter in chapters)
        {
            foreach (var theme in chapter.possibleThemes)
            {
                if (theme == null) continue; 
                themeDict.TryAdd(theme.GetThemeName, theme);
            }
        }
    }

    /// <summary>
    /// 해당 챕터의 데이터를 가져옵니다.
    /// </summary>
    public ChapterData GetChapterData(int chapter)
    {
        return chapters.Find(x => x.chapterNumber == chapter);
    }

    /// <summary>
    /// 해당 챕터의 이름을 가져옵니다.
    /// </summary>
    public string GetChapterName(int chapter)
    {
        ChapterData data = GetChapterData(chapter);

        return data != null
            ? data.chapterName
            : string.Empty;
    }

    /// <summary>
    /// 해당 챕터에서 등장 가능한 바이옴 목록을 가져옵니다.
    /// </summary>
    public List<SO_Theme> GetPossibleThemes(int chapter)
    {
        ChapterData data = GetChapterData(chapter);

        return data?.possibleThemes;
    }

    /// <summary>
    /// 해당 챕터에서 랜덤 바이옴을 선택합니다.
    /// </summary>
    public SO_Theme GetRandomTheme(int chapter)
    {
        List<SO_Theme> theme = GetPossibleThemes(chapter);

        if (theme == null || theme.Count == 0)
            return null;

        return theme[UnityEngine.Random.Range(0, theme.Count)];
    }

    /// <summary>
    /// 해당 테마의 room obj 반환 
    /// </summary>
    /// <param name="themeName"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    public GameObject GetTargetThemeObj(string themeName, int idx)
    {
        if (themeDict.ContainsKey(themeName))
            return themeDict[themeName].GetRoom(idx);

        return null;
    }

    public GameObject GetTargetThemeRandObj(string themeName)
    {
        if(themeDict.ContainsKey(themeName))
        {
            int maxCount = themeDict[themeName].GetPossiblePrefabs.Count;

            int targetIdx = UnityEngine.Random.Range(0, maxCount);
            return themeDict[themeName].GetRoom(targetIdx);
        }

        return null;
    }

    /// <summary>
    /// 해당 테마의 스프라이트 반환
    /// </summary>
    /// <param name="themeName"></param>
    /// <returns></returns>
    public Sprite GetThemeSprite(string themeName)
    {
        if(themeDict.TryGetValue(themeName, out var theme))
        {
            return theme.GetThemeSprite;
        }

        return null;
    }
}
