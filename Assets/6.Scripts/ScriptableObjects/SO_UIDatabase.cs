using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIDatabase", menuName = "Scriptable Objects/UIDatabase")]
public class UIDatabase : ScriptableObject
{
    // 여기에 모든 UI 프리팹을 그냥 때려 넣습니다. (Enum 불필요)
    public List<UiBase> uiPrefabs;

    // 타입으로 프리팹을 찾아주는 도우미 함수
    public UiBase GetPrefab<T>() where T : UiBase
    {
        return uiPrefabs.Find(prefab => prefab is T);
    }
}