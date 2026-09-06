using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Theme", menuName = "Scriptable Objects/SO_Theme")]
public class SO_Theme : ScriptableObject
{
    [SerializeField] private string themeName = "";
    [SerializeField] private Sprite themeBgSpt; 

    [Header("이 테마에서 나올 수 있는 맵 구조들")]
    [SerializeField] private List<GameObject> possibleRoomPrefabs;

    public string GetThemeName => themeName;
    public Sprite GetThemeSprite => themeBgSpt;

    public List<GameObject> GetPossiblePrefabs => possibleRoomPrefabs;

    public GameObject GetRoom(int idx)
    {
        if (idx >= possibleRoomPrefabs.Count)
            return null; 
        
        return possibleRoomPrefabs[idx]; 
    }
}
