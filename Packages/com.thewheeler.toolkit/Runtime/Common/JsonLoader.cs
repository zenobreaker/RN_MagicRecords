using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class JsonLoader
{
    public static void LoadJsonList<TJsonRoot, TJsonData, TResult>(
        TextAsset jsonText,
        Func<TJsonRoot, List<TJsonData>> extractListFunc,
        Func<TJsonData, TResult> converFunc,
        Action<TResult> onResult)
    {
        var rootData = JsonUtility.FromJson<TJsonRoot>(jsonText.text);
        if (rootData == null)
        {
            Debug.LogError("JsonLoader Create Fail rootData");
            return;
        }

        var dataList = extractListFunc(rootData);
        if (dataList == null) return; 

        foreach(var jsonData in dataList)
        {
            var result = converFunc(jsonData);
            onResult?.Invoke(result);
        }
    }

    public static List<int> ParseIntList(string csv)
    {
        return string.IsNullOrEmpty(csv) == false ? csv.Split(',')
            .Select(str => int.Parse(str.Trim()))
            .ToList() : new List<int>();
    }
}

