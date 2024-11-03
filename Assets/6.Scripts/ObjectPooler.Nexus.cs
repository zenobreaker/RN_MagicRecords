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

            // ���� ������Ʈ ���� �� �θ� ����
            for(int i = 0; i< pool.size; i++)
            {
                GameObject obj = CreateNewObjectNoParent(pool.tag, pool.prefab);
                // Version up : 2024 11 03 => Ǯ���� ������Ʈ�� �θ� �޾Ƽ� �ű⿡ ����
                obj.transform.SetParent(parent.transform);
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
}
