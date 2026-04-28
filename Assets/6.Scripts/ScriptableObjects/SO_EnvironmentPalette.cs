using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnvPalette", menuName = "Tools/Environment Palette")]
public class SO_EnvironmentPalette : ScriptableObject
{
    [Tooltip("배치할 환경 프리팹들을 여기에 드래그해서 넣으세요.")]
    public List<GameObject> prefabs = new List<GameObject>();
}