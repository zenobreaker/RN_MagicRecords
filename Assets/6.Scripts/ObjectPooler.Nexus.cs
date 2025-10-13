using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class ObjectPooler : MonoBehaviour
{
    public IEnumerator Start_CreatePoolHierachy()
    {
        foreach(Pool pool in pools)
        {
            poolDictionary.Add(pool.tag, new Queue<GameObject>());

            // 기존 오브젝트 생성 및 부모 설정
            for(int i = 0; i< pool.size; i++)
            {
                // Ver 25.05.04 : 하나의 함수로 처리하게 
                GameObject obj = CreateNewObjectSetParent(pool.tag, pool.prefab, pool.parentTransform);
                ArrangePool(pool.parentTransform, pool.tag, obj);
                yield return null;
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


    // 부모 오브젝트 생성 또는 가져오기
    private Transform GetOrCreateParent(string tag)
    {
        if (parentDictionary.TryGetValue(tag, out Transform parent) == false)
        {
            GameObject parentObj = new GameObject($"{tag}_Parent");
            parent = parentObj.transform;
            parent.transform.SetParent(transform);
            parentDictionary[tag] = parent;
        }
        return parent;
    }

    // 오브젝트 생성 후 부모에 할당
    private GameObject CreateNewObjectSetParent(string tag, GameObject prefab, Transform parentTransform = null)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"prefab is not set.");
            return null;
        }
        GameObject newObj = Instantiate(prefab);
        newObj.name = tag;

        Transform parent = parentTransform != null ? parentTransform : GetOrCreateParent(tag);
        newObj.transform.SetParent(parent);
        
        newObj.SetActive(false);
        return newObj;
    }

    void ArrangePool(string tag, GameObject obj)
    {
        ArrangePool(null, tag, obj);
    }

    void ArrangePool(Transform parentTransform, string tag, GameObject obj)
    {
        // 해당 태그의 부모 오브젝트 찾음 
        Transform parent = null;
        // 우선 parentTransform 있으면 사용
        if (parentTransform != null)
        {
            parent = parentTransform;
        }
        else
        {
            // Pool 클래스에서 가져오기
            var pool = pools.FirstOrDefault<Pool>(p => p.tag == tag);
            if (pool != null && pool.parentTransform != null)
                parent = pool.parentTransform;
        }

        // 아직 null이면 GetOrCreateParent 사용
        if (parent == null)
            parent = GetOrCreateParent(tag);

        // 추가된 오브젝트 묶어서 정렬
        bool isFind = false;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (i == parent.childCount - 1)
            {
                obj.transform.SetSiblingIndex(i);
                spawnObjects.Insert(i, obj);
                break;
            }
            else if (parent.GetChild(i).name == obj.name)
                isFind = true;
            else if (isFind)
            {
                obj.transform.SetSiblingIndex(i);
                spawnObjects.Insert(i, obj);
                break;
            }
        }
    }

}
