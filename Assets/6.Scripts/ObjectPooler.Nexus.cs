using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class ObjectPooler : MonoBehaviour
{
    private Transform creationRoot;
    public IEnumerator Start_CreatePoolHierachy()
    {
        int batchCount = 0;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        // Register every tag before any Awake/OnEnable can reference another pool.
        foreach (Pool pool in pools)
        {
            if (pool == null || string.IsNullOrEmpty(pool.tag) || pool.prefab == null)
                throw new System.InvalidOperationException("Invalid pool entry (tag/prefab).");
            if (poolDictionary.ContainsKey(pool.tag))
                throw new System.InvalidOperationException($"Duplicate pool tag: {pool.tag}");
            poolDictionary.Add(pool.tag, new Queue<GameObject>());
        }
        foreach(Pool pool in pools)
        {

            // 기존 오브젝트 생성 및 부모 설정
            for(int i = 0; i< pool.size; i++)
            {
                // Ver 25.05.04 : 하나의 함수로 처리하게 
                GameObject obj = CreateNewObjectSetParent(pool.tag, pool.prefab, pool.parentTransform);
                ArrangePool(pool.parentTransform, pool.tag, obj);
                if (++batchCount >= Mathf.Max(1, prewarmBatchSize) || watch.Elapsed.TotalMilliseconds >= Mathf.Max(.1f, prewarmFrameBudgetMs))
                {
                    yield return null;
                    batchCount = 0;
                    watch.Restart();
                }
            }

            // OnDisable에 ReturnToPool 구현여부와 중복구현 검사
            if (pool.size > 0 && poolDictionary[pool.tag].Count <= 0)
                Debug.LogError($"{pool.tag}{INFO}");
            else if (poolDictionary[pool.tag].Count != pool.size)
                Debug.LogError($"{pool.tag}에 ReturnToPool이 중복됩니다");
        }
    }

    private GameObject CreateNewObjectNoParent(string tag, GameObject prefab)
    {
        return CreatePooledObject(tag, prefab, null);
    }

    // Instantiate under an inactive hierarchy so native agents cannot register
    // with a NavMesh before we have a chance to disable them.
    private GameObject CreatePooledObject(string tag, GameObject prefab, Transform parent)
    {
        if (prefab == null)
        {
            Debug.LogWarning("prefab is not set.");
            return null;
        }

        if (creationRoot == null)
        {
            var root = new GameObject("ObjectPooler_CreationRoot");
            root.hideFlags = HideFlags.HideInHierarchy;
            root.SetActive(false);
            creationRoot = root.transform;
        }

        GameObject obj = Instantiate(prefab, creationRoot);
        obj.name = tag;
        var agents = obj.GetComponentsInChildren<NavMeshAgent>(true);
        var enabledAgents = new List<NavMeshAgent>();
        foreach (var agent in agents)
        {
            if (!agent.enabled) continue;
            enabledAgents.Add(agent);
            agent.enabled = false;
        }

        // Preserve the existing Awake/OnEnable initialization before callers
        // configure a deferred spawn (e.g. Enemy.SetStatData).
        bool wasPrewarming = IsPrewarming;
        IsPrewarming = true;
        try
        {
            obj.transform.SetParent(parent, true);
            obj.SetActive(false);
        }
        finally { IsPrewarming = wasPrewarming; }
        foreach (var agent in enabledAgents)
        {
            if (agent != null) agent.enabled = true;
        }

        // Inactive prefabs/parents do not receive OnDisable. Register explicitly;
        // ReturnToPool also handles the existing OnDisable registration once.
        ReturnToPool(obj);
        return obj;
    }


    // 부모 오브젝트 생성 또는 가져오기
    private Transform GetOrCreateParent(string tag)
    {
        // 1. 딕셔너리에 키가 있는지 확인
        bool hasKey = parentDictionary.TryGetValue(tag, out Transform parent);

        // 2. 키가 있더라도 실제 객체가 파괴(null)되었는지 체크
        if (hasKey == false || parent == null)
        {
            // 기존에 null인 주소가 들어있었다면 제거 (메모리 정리)
            if (hasKey) parentDictionary.Remove(tag);

            GameObject parentObj = new GameObject($"{tag}_Parent");
            parent = parentObj.transform;
            parent.SetParent(transform);
            parentDictionary[tag] = parent;
        }

        return parent;
    }

    // 오브젝트 생성 후 부모에 할당
    private GameObject CreateNewObjectSetParent(string tag, GameObject prefab, Transform parentTransform = null)
    {
        Transform parent = parentTransform != null ? parentTransform : GetOrCreateParent(tag);
        return CreatePooledObject(tag, prefab, parent);
    }

    void ArrangePool(string tag, GameObject obj)
    {
        ArrangePool(null, tag, obj);
    }

    void ArrangePool(Transform parentTransform, string tag, GameObject obj)
    {
        // Creation already chose the correct parent. Avoid scanning siblings and
        // inserting into the middle of a growing global list for every object.
        if (obj != null) spawnObjects.Add(obj);
    }



}
