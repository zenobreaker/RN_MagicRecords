using System.Collections.Generic;
using UnityEngine;

public partial class ObjectPooler : MonoBehaviour
{
    public void Start_CreatePoolHierachy()
    {
        foreach(Pool pool in pools)
        {
            poolDictionary.Add(pool.tag, new Queue<GameObject>());
            GameObject parent = new GameObject(pool.tag);
            parent.transform.SetParent(transform);

            // 기존 오브젝트 생성 및 부모 설정
            for(int i = 0; i< pool.size; i++)
            {
                GameObject obj = CreateNewObjectNoParent(pool.tag, pool.prefab);
                // Version up : 2024 11 03 => 풀링될 오브젝트의 부모를 받아서 거기에 생성
                obj.transform.SetParent(parent.transform);
                ArrangePool(obj);
            }

            // OnDisable에 ReturnToPool 구현여부와 중복구현 검사
            if (poolDictionary[pool.tag].Count <= 0)
                Debug.LogError($"{pool.tag}{INFO}");
            else if (poolDictionary[pool.tag].Count != pool.size)
                Debug.LogError($"{pool.tag}에 ReturnToPool이 중복됩니다");
        }
    }

    private GameObject CreateNewObjectNoParent(string tag, GameObject prefab)
    {
        // Version up : 2024 11 03 => 풀링될 대상의 부모를 생성할 때 거기에 만들고 배치시킴
        var obj = Instantiate(prefab);
        obj.name = tag;
        obj.SetActive(false); // 비활성화시 ReturnToPool을 하므로 Enqueue가 됨
        return obj;
    }
}
