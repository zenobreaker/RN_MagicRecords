using System.Collections.Generic;
using UnityEngine;



public class LocalizationManager : Singleton<LocalizationManager>
{
    [Header("Settings")]
    public LanguageType currentLanguage = LanguageType.KR;
    public TextAsset stringJson; // StringData.json 할당

    // Key: "name_biome_forest", Value: "울창한 숲"
    private Dictionary<string, string> textTable = new Dictionary<string, string>();

    protected override void Awake()
    {
        base.Awake();
        InitializeStringData();
    }

    public void InitializeStringData()
    {
        if (stringJson == null) return;
        textTable.Clear();

        // 💡 개발자님이 쓰시던 JsonLoader 스타일 (또는 직관적인 파싱)
        var rootData = JsonUtility.FromJson<StringDataAllData>(stringJson.text);
        if (rootData == null || rootData.stringDataJson == null) return;

        foreach (var data in rootData.stringDataJson)
        {
            string value = currentLanguage == LanguageType.KR ? data.kr : data.en;

            // 데이터가 비어있으면 키값을 그대로 노출해서 버그 찾기 쉽게 함
            if (string.IsNullOrEmpty(value)) value = $"[{data.key}]";

            textTable.TryAdd(data.key, value);
        }

        Debug.Log($"<color=cyan>[Localization]</color> 번역 데이터 로드 완료: {textTable.Count}개");
    }

    // 💡 다른 스크립트에서 번역이 필요할 때 호출하는 핵심 함수
    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";

        if (textTable.TryGetValue(key, out string result))
            return result;

        return key; // 딕셔너리에 없으면 name_어쩌고를 그대로 리턴
    }
}
