using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Damage", menuName = "Scriptable Objects/SO_Damage")]
public class SO_Damage : ScriptableObject
{
    public List<DamageData> damageDatas = new List<DamageData>();
}
