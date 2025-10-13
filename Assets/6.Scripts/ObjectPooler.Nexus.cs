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

            // ���� ������Ʈ ���� �� �θ� ����
            for(int i = 0; i< pool.size; i++)
            {
                // Ver 25.05.04 : �ϳ��� �Լ��� ó���ϰ� 
                GameObject obj = CreateNewObjectSetParent(pool.tag, pool.prefab, pool.parentTransform);
                ArrangePool(pool.parentTransform, pool.tag, obj);
                yield return null;
            }

            // OnDisable�� ReturnToPool �������ο� �ߺ����� �˻�
            if (poolDictionary[pool.tag].Count <= 0)
                Debug.LogError($"{pool.tag}{INFO}");
            else if (poolDictionary[pool.tag].Count != pool.size)
                Debug.LogError($"{pool.tag}�� ReturnToPool�� �ߺ��˴ϴ�");
        }
    }

    private GameObject CreateNewObjectNoParent(string tag, GameObject prefab)
    {
        // Version up : 2024 11 03 => Ǯ���� ����� �θ� ������ �� �ű⿡ ����� ��ġ��Ŵ
        var obj = Instantiate(prefab);
        obj.name = tag;
        obj.SetActive(false); // ��Ȱ��ȭ�� ReturnToPool�� �ϹǷ� Enqueue�� ��
        return obj;
    }


    // �θ� ������Ʈ ���� �Ǵ� ��������
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

    // ������Ʈ ���� �� �θ� �Ҵ�
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
        // �ش� �±��� �θ� ������Ʈ ã�� 
        Transform parent = null;
        // �켱 parentTransform ������ ���
        if (parentTransform != null)
        {
            parent = parentTransform;
        }
        else
        {
            // Pool Ŭ�������� ��������
            var pool = pools.FirstOrDefault<Pool>(p => p.tag == tag);
            if (pool != null && pool.parentTransform != null)
                parent = pool.parentTransform;
        }

        // ���� null�̸� GetOrCreateParent ���
        if (parent == null)
            parent = GetOrCreateParent(tag);

        // �߰��� ������Ʈ ��� ����
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
