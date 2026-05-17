using System;
using System.Collections.Generic;
using UnityEngine;

// 💡 1. 인스펙터에서 키와 이미지를 짝지어줄 데이터 구조체
[Serializable]
public struct EventImageData
{
    public string imageKey;      // 예: "evt_1001_c1"
    public Sprite imageSprite;   // 연결할 실제 2D 스프라이트
}

[CreateAssetMenu(fileName = "NewEventImagePalette", menuName = "Tools/Event Image Palette")]
public class SO_EventImagePalette : ScriptableObject
{
    [Tooltip("기획서의 imageKey와 실제 이미지를 연결해주세요.")]
    public List<EventImageData> eventImages = new List<EventImageData>();

    // 💡 런타임용 빠른 검색 캐시 (딕셔너리)
    private Dictionary<string, Sprite> imageDict;

    public void Initialize()
    {
        if (imageDict != null) return;

        imageDict = new Dictionary<string, Sprite>();
        foreach (var item in eventImages)
        {
            if (!string.IsNullOrEmpty(item.imageKey) && !imageDict.ContainsKey(item.imageKey))
            {
                imageDict.Add(item.imageKey, item.imageSprite);
            }
        }
    }

    // 외부에서 키값으로 이미지를 요청할 때 사용할 함수
    public Sprite GetImage(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (imageDict == null) Initialize();

        if (imageDict.TryGetValue(key, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[SO_EventImagePalette] 일치하는 이미지 키가 없습니다: {key}");
        return null;
    }
}