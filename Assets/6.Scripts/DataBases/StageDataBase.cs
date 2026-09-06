using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class ChapterInfoJson : InfoJson
{
    public string possibleBiomes;
    public float difficultScalar;
}

public sealed class ChapterInfo
{
    public int id;
    public List<int> possibleBiomes;
}

[System.Serializable]
public sealed class ChapterInfoAllData
{
    public List<ChapterInfoJson> chapterInfoJson;
}

[System.Serializable]
public sealed class BiomesInfoJson : InfoJson
{
    public string environmentData;
}

public sealed class BiomesInfo : InfoBase
{
    public string environmentData;
}

[System.Serializable]
public sealed class BiomesInfoAllData
{
    public List<BiomesInfoJson> biomeInfoJson;
}

[System.Serializable]
public class StageInfoJson
{
    public int id;
    public int stageType;
    public int chapter;
    public string theme;
    public int mapIndex;
    public string groupIds;
    public int clearRewardId;
    public int wave;
    public string battleBGM;
}

[System.Serializable]
public sealed class StageInfoJsonAllData
{
    public List<StageInfoJson> stageInfoJson;
}


public sealed class StageDataBase : DataBase
{
    [Header("Chpater Data Json")]
    [SerializeField] private TextAsset chapterJson;

    [Header("Biomes Data Json")]
    [SerializeField] private TextAsset biomeJson;

    [Header("Stage Data Json")]
    [SerializeField] private TextAsset stageJson;

    [Header("Chapter SO")]
    [SerializeField] private SO_ChapterData chapterData;

    [Header("Theme SO")]
    [SerializeField]
    private List<SO_Theme> ThemeList = new();

    private Dictionary<int, ChapterInfo> chapterInfoTable = new();
    private Dictionary<int, BiomesInfo> biomeInfoTable = new();

    // 스테이지 정보 테이블 - key : id,  value : stage info 
    private Dictionary<int, StageInfo> stageInfoTable = new();
    // 스테이지 챕터별 테이블 - key : chapter,  value : 해당 챕터에 등장하는 stage id  
    private Dictionary<int, List<int>> stageChapterTable = new();
    // 보스 스테이지 테이블  - key : boss chatper, value : 해당 챕터에 등장하는 boss stage list 
    private Dictionary<int, List<int>> bossChapterTable = new();

    public override void Initialize()
    {
        InitializeChapterData();

        InitializeBiomeData();

        InitializeStageData();

        if (chapterData != null)
            chapterData.Init();
    }


    private void InitializeChapterData()
    {
        if (chapterJson == null) return;

        Debug.Log("Chapter Database Init");


        JsonLoader.LoadJsonList<ChapterInfoAllData, ChapterInfoJson, ChapterInfo>
            (
                chapterJson,
                root => root.chapterInfoJson,
                json =>
                {
                    var chapter = new ChapterInfo
                    {
                        id = json.id,
                        possibleBiomes = JsonLoader.ParseIntList(json.possibleBiomes),
                    };
                    return chapter;
                },

                chapter =>
                {
                    chapterInfoTable.TryAdd(chapter.id, chapter);
                }
            );

        // Complete Message 
        Debug.Log("===================================================");
        Debug.Log($"Complete Message => chapterInfoTable : {chapterInfoTable.Count}, " +
            $"chapterInfoTable : {chapterInfoTable.Count}");
    }

    private void InitializeBiomeData()
    {
        if (biomeJson == null) return;

        Debug.Log("Biome Database Init");


        JsonLoader.LoadJsonList<BiomesInfoAllData, BiomesInfoJson, BiomesInfo>
            (
                biomeJson,
                root => root.biomeInfoJson,
                json =>
                {
                    var theme = new BiomesInfo
                    {
                        id = json.id,
                        environmentData = json.environmentData,
                    };
                    return theme;
                },

                theme =>
                {
                    biomeInfoTable.TryAdd(theme.id, theme);
                }
            );

        // Complete Message 
        Debug.Log("===================================================");
        Debug.Log($"Complete Message => biomeInfoTable: {biomeInfoTable.Count}, " +
            $"biomeInfoTable : {biomeInfoTable.Count}");
    }

    private void InitializeStageData()
    {
        if (stageJson == null) return;

        Debug.Log("Stage Database Init");

        JsonLoader.LoadJsonList<StageInfoJsonAllData, StageInfoJson, StageInfo>
          (
              stageJson,
              root => root.stageInfoJson,

              json =>
              {
                  var stage = new StageInfo
                  {
                      id = json.id,
                      type = (StageType)json.stageType,
                      groupIds = JsonLoader.ParseIntList(json.groupIds),
                      theme = json.theme,
                      clearRewardId = json.clearRewardId,
                      chapter = json.chapter,
                      mapIndex = json.mapIndex,
                      wave = json.wave,
                      battleBGM = json.battleBGM,
                  };
                  return stage;
              },

              stage =>
              {
                  // Stage Info 
                  stageInfoTable.TryAdd(stage.id, stage);

                  if (stage.type == StageType.Boss_Combat)
                  {
                      if (bossChapterTable.TryGetValue(stage.chapter, out List<int> bossIds))
                          bossIds.Add(stage.id);
                      else
                      {
                          bossChapterTable.Add(stage.chapter, new List<int>());
                          bossChapterTable[stage.chapter].Add(stage.id);
                      }
                      return;
                  }

                  // Stage Chapter
                  if (stageChapterTable.TryGetValue(stage.chapter, out List<int> ids))
                      ids.Add(stage.id);
                  else
                  {
                      stageChapterTable.Add(stage.chapter, new List<int>());
                      stageChapterTable[stage.chapter].Add(stage.id);
                  }
              }
          );


        // Complete Message 
        Debug.Log("===================================================");
        Debug.Log($"Complete Message => stageInfoTable : {stageInfoTable.Count}, " +
            $"stageChapterTable : {stageChapterTable.Count}, " +
            $"bossChapterTable : {bossChapterTable.Count}");
    }

    //public string GetThemeName(int chapter)
    //{
    //    if (chapterInfoTable != null &&
    //        chapterInfoTable.TryGetValue(chapter, out var info))
    //    {
    //        if (info.possibleBiomes != null)
    //        {
    //            int maxCount = info.possibleBiomes.Count;
    //            int biomeResultIdx = Random.Range(0, maxCount);

    //            int biomeId = info.possibleBiomes[biomeResultIdx];

    //            if (biomeInfoTable != null &&
    //                biomeInfoTable.TryGetValue(biomeId, out var theme))
    //                return theme.environmentData;

    //            return "Test";
    //        }
    //    }

    //    return "Test";
    //}

    public string GetRandThemeNameByChapter(int chapter)
    {
        if (chapterData == null) return "Test";

        var theme = chapterData.GetRandomTheme(chapter);
        if (theme == null) return "Test";

        return theme.GetThemeName;
    }

    public SO_Theme SelectRandomTheme(int chapter)
    {
        return GetRandomTheme(chapter);
    }

    public SO_Theme GetRandomTheme(int chapter)
    {
        List<SO_Theme> theme = GetPossibleThemes(chapter);

        if (theme == null || theme.Count == 0)
            return null;

        int randomIndex = Random.Range(0, theme.Count);

        return theme[randomIndex];
    }

    public Sprite GetThemeBgSptByTheme(string themeName)
    {
        if (string.IsNullOrEmpty(themeName)) return null;

        return chapterData.GetThemeSprite(themeName);
    }

    public GameObject GetTargetThemeObj(string themeName, int idx)
    {
        if (chapterData == null) return null; 

        return chapterData.GetTargetThemeObj(themeName, idx);
    }

    public GameObject GetRandomThemeObj(string themeName)
    {
        if (chapterData == null) return null;

        return chapterData.GetTargetThemeRandObj(themeName);
    }

    public List<SO_Theme> GetPossibleThemes(int chapter)
    {
        if (chapterData == null) return null;

        return chapterData.GetPossibleThemes(chapter);
    }

    public string GetTestBiome() { return "Test"; }

    public List<int> GetIdsByChapter(int chapter)
    {
        if (stageChapterTable.TryGetValue(chapter, out List<int> ids))
            return ids;
        else
            return null;
    }

    public int GetRandomStageIDByChater(int chapter)
    {
        if (stageChapterTable.TryGetValue(chapter, out var ids))
        {
            if (ids.Count == 0) return -1;

            int randID = Random.Range(0, ids.Count);
            return ids[randID];
        }

        return -1;
    }

    public StageInfo GetStageInfo(int stageID)
    {
        if (stageInfoTable.TryGetValue(stageID, out StageInfo stageInfo))
            return stageInfo.Copy();
        else
            return null;
    }

    public StageInfo GetBossStageInfo(int chapter, int stageID)
    {
        if (bossChapterTable.TryGetValue(chapter, out List<int> ids))
        {
            foreach (var id in ids)
            {
                if (id == stageID)
                    return GetStageInfo(stageID);
            }
        }
        return null;
    }

    public int GetRandomBossStageId(int chapter)
    {
        if (bossChapterTable.TryGetValue(chapter, out List<int> ids))
        {
            if (ids.Count == 0) return -1;
            int randID = Random.Range(0, ids.Count);
            return ids[randID];
        }
        return -1;
    }
}
