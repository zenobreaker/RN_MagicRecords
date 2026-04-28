using UnityEngine;
using System.Collections.Generic;

public sealed class BiomeManager : Singleton<BiomeManager>
{
    [Header("Target Rendering")]
    public Material backgroundMaterial;

    [Header("Chapter Biomes")]
    public SO_ChapterData chapterData;


    // 이름으로 빠르게 찾기 위한 사전(Dictionary)
    private Dictionary<string, SO_Biome> biomeDict = new Dictionary<string, SO_Biome>();


    protected override void Awake()
    {
        base.Awake();

        if (chapterData == null) return;

        foreach (var biome in chapterData.possibleBiomes)
        {
            biomeDict[biome.biomeName] = biome;
        }
    }

    public SO_Biome GetBiomeData(string biomeName)
    {
        if(string.IsNullOrEmpty(biomeName) == false
            && biomeDict.TryGetValue(biomeName, out var biome))
            return biome;

        return null;
    }

    /// <summary>
    /// 이름(ID)으로 바이옴을 변경합니다. (예: "Desert", "Snow")
    /// </summary>
    public void ChangeBiome(string biomeName)
    {
        if (string.IsNullOrEmpty(biomeName) == false && biomeDict.TryGetValue(biomeName, out SO_Biome targetBiome))
        {
            ApplyBiome(targetBiome);
        }
        else
        {
            Debug.LogWarning($"[BiomeManager] {biomeName}이라는 이름의 바이옴을 찾을 수 없습니다!");
        }
    }

    private void ApplyBiome(SO_Biome data)
    {
        if (backgroundMaterial == null) return;

        // 셰이더 프로퍼티 업데이트
        backgroundMaterial.SetTexture("_BaseTex", data.baseTex);
        backgroundMaterial.SetTexture("_Layer1Tex", data.layer1Tex);
        backgroundMaterial.SetTexture("_Layer2Tex", data.layer2Tex);
        backgroundMaterial.SetTexture("_SplatMap", data.splatMap);

        backgroundMaterial.SetFloat("_TextureTiling", data.textureTiling);
        backgroundMaterial.SetFloat("_MaskTiling", data.maskTiling);

        Debug.Log($"<color=yellow>[BiomeManager]</color> 테마가 <b>{data.biomeName}</b>(으)로 전환되었습니다.");
    }
}