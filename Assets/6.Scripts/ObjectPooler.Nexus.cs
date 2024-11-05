using System.Collections.Generic;
using UnityEngine;

public partial class ObjectPooler : MonoBehaviour
{
    public void Start_CreatePoolHierachy()
    {
        foreach(Pool pool in pools)
        {
            poolDictionary.Add(pool.tag, new Queue<GameObject>());

            // ���� ������Ʈ ���� �� �θ� ����
            for(int i = 0; i< pool.size; i++)
            {
                // Version up : 2024 11 03 => Ǯ���� ������Ʈ�� �θ� �޾Ƽ� �ű⿡ ����
                GameObject obj = CreateNewObjectSetParent(pool.tag, pool.prefab);
                ArrangePool(obj);
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
        if (!parentDictionary.TryGetValue(tag, out Transform parent))
        {
            GameObject parentObj = new GameObject($"{tag}_Parent");
            parent = parentObj.transform;
            parent.transform.SetParent(transform);
            parentDictionary[tag] = parent;
        }
        return parent;
    }

    // ������Ʈ ���� �� �θ� �Ҵ�
    private GameObject CreateNewObjectSetParent(string tag, GameObject prefab)
    {
        GameObject newObj = Instantiate(prefab);
        newObj.name = tag;
        Transform parent = GetOrCreateParent(tag);
        newObj.transform.SetParent(parent);
        newObj.SetActive(false);
        return newObj;
    }
}
