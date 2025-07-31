
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ObjectPooler))]
public class ObjectPoolerEditor : Editor
{
    const string INFO = "풀링한 오브젝트에 다음을 적으세요 \nvoid OnDisable()\n{\n" +
        "    ObjectPooler.ReturnToPool(gameObject);    // 한 객체에 한번만 \n" +
        "    CancelInvoke();    // Monobehaviour에 Invoke가 있다면 \n}";

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(INFO, MessageType.Info);
        base.OnInspectorGUI();
    }
}
#endif

public partial class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;
    void Awake() => Instance = this;

    [Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        public Transform parentTransform;
    }
    readonly string INFO = " 오브젝트에 다음을 적으세요 \nvoid OnDisable()\n{\n" +
        "    ObjectPooler.ReturnToPool(gameObject);    // 한 객체에 한번만 \n" +
        "    CancelInvoke();    // Monobehaviour에 Invoke가 있다면 \n}";



    [SerializeField] private SO_PlayerObjects playerObjectSo;
    [SerializeField] private SO_NPCObjects npcObjectSo;
    [SerializeField] private List<Pool> pools;

    private List<GameObject> spawnObjects;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    // 부모 관리용 딕셔너리 추가
    private Dictionary<string, Transform> parentDictionary = new Dictionary<string, Transform>();
    public static event Action OnPoolInitialized;

    #region SpawnFromPool
    public static GameObject SpawnFromPool(string tag, Transform trasnform) =>
        Instance._SpawnFromPool(tag, trasnform.position, trasnform.rotation);

    public static GameObject SpawnFromPool(string tag, Vector3 position) =>
        Instance._SpawnFromPool(tag, position, Quaternion.identity);

    public static GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation) =>
        Instance._SpawnFromPool(tag, position, rotation);

    public static T SpawnFromPool<T>(string tag, Vector3 position) where T : Component
    {
        GameObject obj = Instance._SpawnFromPool(tag, position, Quaternion.identity);
        if (obj.TryGetComponent(out T component))
            return component;
        else
        {
            obj.SetActive(false);
            throw new Exception($"Component not found");
        }
    }

    public static T SpawnFromPool<T>(string tag, Vector3 position, Quaternion rotation) where T : Component
    {
        GameObject obj = Instance._SpawnFromPool(tag, position, rotation);
        if (obj.TryGetComponent(out T component))
            return component;
        else
        {
            obj.SetActive(false);
            throw new Exception($"Component not found");
        }
    }

    public static T SpawnFromPool<T>(string tag, Transform trasnform) where T : Component
    {
        GameObject obj = Instance._SpawnFromPool(tag, trasnform.position, trasnform.rotation);
        if (obj.TryGetComponent(out T component))
            return component;
        else
        {
            obj.SetActive(false);
            throw new Exception($"Component not found");
        }
    }
    #endregion

    #region DeferedSpawn
    public static GameObject DeferedSpawnFromPool(string tag, Transform trasnform) =>
       Instance._DeferedSpawnFromPool(tag, trasnform.position, trasnform.rotation);

    public static GameObject DeferedSpawnFromPool(string tag, Vector3 position) =>
        Instance._DeferedSpawnFromPool(tag, position, Quaternion.identity);

    public static GameObject DeferedSpawnFromPool(string tag, Vector3 position, Quaternion rotation) =>
        Instance._DeferedSpawnFromPool(tag, position, rotation);

    public static T DeferedSpawnFromPool<T>(string tag, Vector3 position) where T : Component
    {
        GameObject obj = Instance._DeferedSpawnFromPool(tag, position, Quaternion.identity);
        if (obj.TryGetComponent(out T component))
            return component;
        else
        {
            obj.SetActive(false);
            throw new Exception($"Component not found");
        }
    }

    public static T DeferedSpawnFromPool<T>(string tag, Vector3 position, Quaternion rotation) where T : Component
    {
        GameObject obj = Instance._DeferedSpawnFromPool(tag, position, rotation);
        if (obj.TryGetComponent(out T component))
            return component;
        else
        {
            obj.SetActive(false);
            throw new Exception($"Component not found");
        }
    }

    public static T DeferedSpawnFromPool<T>(string tag, Transform trasnform) where T : Component
    {
        GameObject obj = Instance._DeferedSpawnFromPool(tag, trasnform.position, trasnform.rotation);
        if (obj.TryGetComponent(out T component))
            return component;
        else
        {
            obj.SetActive(false);
            throw new Exception($"Component not found");
        }
    }
    #endregion

    #region GetAllPools
    public static List<GameObject> GetAllPools(string tag)
    {
        if (!Instance.poolDictionary.ContainsKey(tag))
            throw new Exception($"Pool with tag {tag} doesn't exist.");

        return Instance.spawnObjects.FindAll(x => x.name == tag);
    }

    public static List<T> GetAllPools<T>(string tag) where T : Component
    {
        List<GameObject> objects = GetAllPools(tag);

        if (!objects[0].TryGetComponent(out T component))
            throw new Exception("Component not found");

        return objects.ConvertAll(x => x.GetComponent<T>());
    }

    #endregion

    public static void ReturnToPool(GameObject obj)
    {
        if (!Instance.poolDictionary.ContainsKey(obj.name))
            throw new Exception($"Pool with tag {obj.name} doesn't exist.");

        Instance.poolDictionary[obj.name].Enqueue(obj);
    }

    [ContextMenu("GetSpawnObjectsInfo")]
    private void GetSpawnObjectsInfo()
    {
        foreach (var pool in pools)
        {
            int count = spawnObjects.FindAll(x => x.name == pool.tag).Count;
            Debug.Log($"{pool.tag} count : {count}");
        }
    }

    private GameObject _SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
            throw new Exception($"Pool with tag {tag} doesn't exist.");

        // 큐에 없으면 새로 추가
        Queue<GameObject> poolQueue = poolDictionary[tag];
        if (poolQueue.Count <= 0)
        {
            Pool pool = pools.Find( x => x.tag == tag);
            //var obj = CreateNewObject(pool.tag, pool.prefab);
            GameObject obj = CreateNewObjectSetParent(pool.tag, pool.prefab);
            ArrangePool(tag, obj);
        }

        // 큐에서 꺼내서 사용
        GameObject objectToSpawn = poolQueue.Dequeue();
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        return objectToSpawn;
    }

    // Defered Spawn 
    private GameObject _DeferedSpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
            throw new Exception($"Pool with tag {tag} doesn't exist.");

        // 큐에 없으면 새로 추가
        Queue<GameObject> poolQueue = poolDictionary[tag];
        if (poolQueue.Count <= 0)
        {
            Pool pool = pools.Find(x => x.tag == tag);
            //var obj = CreateNewObject(pool.tag, pool.prefab);
            GameObject obj = CreateNewObjectSetParent(pool.tag, pool.prefab);
            ArrangePool(tag, obj);
        }

        // 큐에서 꺼내서 사용
        GameObject objectToSpawn = poolQueue.Peek();
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        return objectToSpawn;
    }

    public static void FinishSpawn(GameObject obj)
    {
        FinishSpawn(obj.name);
    }

    public static void FinishSpawn(string tag)
    {
        if (!Instance.poolDictionary.ContainsKey(tag))
            throw new Exception($"Pool with tag {tag} doesn't exist.");

        Queue<GameObject> poolQueue = Instance.poolDictionary[tag];
        GameObject objectToSpawn = poolQueue.Dequeue();
        objectToSpawn.SetActive(true);
    }


    private IEnumerator Start()
    {
        spawnObjects = new List<GameObject>();
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        if (npcObjectSo != null)
            CreatePoolsFromScriptableObject(npcObjectSo, false);

        //Start_CreatePool_Origin();

        yield return StartCoroutine(Start_CreatePoolHierachy());
        OnPoolInitialized?.Invoke(); // 초기화 완료 알림
    }

    private void CreatePoolsFromScriptableObject(SO_CharacterObjects objectSo, bool bIsPlayer = false)
    {
        foreach (var co in objectSo.list)
        {
            string tag = $"NPC_{co.id}";
            if (bIsPlayer)
                tag = $"PC_{co.id}";

            Pool pool = new();
            pool.tag = tag;
            pool.prefab = co.obj;
            pool.size = 5;
            pool.parentTransform = null;
            pools.Add(pool);
        }
    }

    private void Start_CreatePool_Origin()
    {
        // 미리 생성
        foreach (Pool pool in pools)
        {
            poolDictionary.Add(pool.tag, new Queue<GameObject>());
            for (int i = 0; i < pool.size; i++)
            {
                var obj = CreateNewObject(pool.tag, pool.prefab);
                ArrangePool(obj);
            }

            // OnDisable에 ReturnToPool 구현여부와 중복구현 검사
            if (poolDictionary[pool.tag].Count <= 0)
                Debug.LogError($"{pool.tag}{INFO}");
            else if (poolDictionary[pool.tag].Count != pool.size)
                Debug.LogError($"{pool.tag}에 ReturnToPool이 중복됩니다");
        }
    }

    GameObject CreateNewObject(string tag, GameObject prefab)
    {
        // Pooler를 부모로 하여금 해당 오브젝트 생성
        var obj = Instantiate(prefab, transform);
        obj.name = tag;
        obj.SetActive(false); // 비활성화시 ReturnToPool을 하므로 Enqueue가 됨
        return obj;
    }

    void ArrangePool(GameObject obj)
    {
        // 추가된 오브젝트 묶어서 정렬
        bool isFind = false;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (i == transform.childCount - 1)
            {
                obj.transform.SetSiblingIndex(i);
                spawnObjects.Insert(i, obj);
                break;
            }
            else if (transform.GetChild(i).name == obj.name)
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