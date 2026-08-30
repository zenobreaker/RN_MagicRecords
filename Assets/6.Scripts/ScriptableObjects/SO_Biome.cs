using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Biome", menuName = "Scriptable Objects/SO_Biome")]
public class SO_Biome : ScriptableObject
{
    public string biomeName = "Forest";

    [Header("이 테마에서 나올 수 있는 맵 구조들")]
    public List<GameObject> possibleRoomPrefabs; 
}
