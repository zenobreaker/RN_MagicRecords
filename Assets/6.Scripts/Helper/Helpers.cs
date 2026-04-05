using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LayerMaskExtensions
{
    public static bool Contains(this LayerMask layerMask, int layer)
    {
        return (layerMask.value & (1 << layer)) != 0; 
    }

    public static bool Contains(this LayerMask layerMask, GameObject obj)
    {
        return (layerMask.value & (1 << obj.layer)) != 0;
    }
}

public static class Extend_TransformHelpers
{
    public static Transform FindChildByName(this Transform transform, string name)
    {
        Transform[] trasnforms = transform.GetComponentsInChildren<Transform>();

        foreach (Transform t in trasnforms)
        {
            if (t.gameObject.name.Equals(name))
                return t;
        }

        return null;
    }

    public static Transform FindChildByNameDeeper(this Transform transform, string name)
    {
        Transform[] trasnforms = transform.GetComponentsInChildren<Transform>();

        foreach (Transform t in trasnforms)
        {
            if (t.gameObject.name.Equals(name))
                return t;

            FindChildByNameDeeper(t, name);
        }

        return null;
    }
    public static GameObject[] FindChildrenByComponentType<T>(this Transform transform) where T : Component
    {
        T[] trasnforms = transform.GetComponentsInChildren<T>();

        List<GameObject> gameObjects = new List<GameObject>();

        foreach (T t in trasnforms)
        {
            gameObjects.Add(t.gameObject);
        }

        return gameObjects.ToArray();
    }

    public static Vector3 FindGreaterBounds(this Transform transform)
    {
        Renderer[] renderers = transform.GetComponentsInChildren<Renderer>();

        Vector3 result = Vector3.zero;
        foreach (Renderer r in renderers)
        {
            Vector3 size = r.bounds.size;
            if (size.magnitude > result.magnitude)
                result = size;
        }

        return result;
    }
}

public static class Extend_Component
{
    public static bool TryComponentInChildren<T>(this GameObject go, out T component, bool includeInactive = false)
        where T : Component
    {
        if (go.TryGetComponent<T>(out component))
            return true;

        component = go.GetComponentInChildren<T>(includeInactive);
        return component != null;
    }

    public static bool TryComponentInChildren<T>(this Component c, out T component, bool includeInactive = false)
        where T : Component
    {
        return c.gameObject.TryComponentInChildren<T>(out component, includeInactive);
    }

    public static bool TryComponentInParent<T>(this GameObject go, out T component, bool includeInactive = false)
    {
        if(go.TryGetComponent<T>(out component)) return true;

        component = go.GetComponentInParent<T>(includeInactive);
        return component != null;   
    }

    public static bool TryComponentInParent<T>(this Component c, out T component, bool includeInactive = false)
    {
        return c.gameObject.TryComponentInParent<T>(out component, includeInactive);
    }
}

public static class Extend_List
{
    public static void Unique<T>(this List<T> list, T item)
    {
        if (list.Contains(item)) return;
        list.Add(item);
    }
}

public static class Extend_Vector3
{
    public static float GetAngle(Vector3 Start, Vector3 End)
    {
        Vector3 direction = End - Start;

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

}

public static class UIHelpers
{
    public static Canvas CreateBillboardCanvas(string resourceName, Transform transform, Camera camera)
    {
        GameObject prefab = Resources.Load<GameObject>(resourceName);
        GameObject obj = GameObject.Instantiate<GameObject>(prefab, transform);

        Canvas canvas = obj.GetComponent<Canvas>();
        canvas.worldCamera = camera;

        return canvas;
    }

}


public static class CameraHelpers
{
    public static bool GetCursorLocation(out Vector3 position, float distance, LayerMask mask)
    {

        Vector3 normal;

        return GetCursorLocation(out position, out normal, distance, mask);

    }

    public static bool GetCursorLocation(float distance, LayerMask mask)
    {
        Vector3 position;
        Vector3 normal;

        return GetCursorLocation(out position, out normal, distance, mask);
    }

    public static bool GetCursorLocation(out Vector3 position, out Vector3 normal, float distance, LayerMask mask)
    {
        position = Vector3.zero;
        normal = Vector3.zero;

        //Input.mousePosition 주의사항 => 인풋시스템에선 안먹음.. 인풋시스템에 따로잇ㅇㅁ??
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, distance, mask))
        {
            position = hit.point;
            normal = hit.normal;

            return true;
        }

        return false;
    }
}


public static class MathHelpers
{
    public static bool IsNearlyEqual(float a, float b, float tolarmace = 1e-6f)
    {
        return Mathf.Abs(a - b) <= tolarmace;
    }

    public static bool IsNearlyZero(float a, float b, float tolarmace = 1e-6f)
    {
        return Mathf.Abs(a) <= tolarmace;
    }

}

public static class PositionHelpers
{
    /// <summary>
    /// 중심점 기준으로 반지름만큼 떨어진 위치들을 각도 간격으로 생성
    /// </summary>
    /// <param name="center">중심 좌표</param>
    /// <param name="radius">반지름</param>
    /// <param name="count">포인트 개수 (기본값 8)</param>
    /// <param name="angleOffset">회전 오프셋 (기본값 0)</param>
    /// <param name="randomize">무작위 섞기 여부</param>
    public static List<Vector3> GenerateCirclePoints(Vector3 center, float radius, int count = 8, float angleOffset = 0f, bool randomize = false)
    {
        List<Vector3> points = new List<Vector3>();
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = angleOffset + angleStep * i;
            float radian = angle * Mathf.Deg2Rad;
            float x = Mathf.Cos(radian) * radius;
            float z = Mathf.Sin(radian) * radius;

            points.Add(center + new Vector3(x, 0, z));
        }

        if(randomize)
        {
            points = points.OrderBy(_ => UnityEngine.Random.value).ToList();
        }

        return points;
    }

    // 2. 방향 벡터를 각도로 변환 (XZ 평면 기준)
    public static float VectorToAngle(Vector3 direction)
    {
        return Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
    }

    // 3. 각도를 방향 벡터로 변환
    public static Vector3 AngleToVector(float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radian), 0, Mathf.Sin(radian));
    }

    public static Quaternion GetDirection(Transform owner, int index, int totalCount,
        float angleBetween, float offset)
    {
        float currentAngle = offset;
        if (totalCount > 1)
            currentAngle += (index - (totalCount - 1) * 0.5f) * angleBetween;
        return owner.rotation * Quaternion.Euler(0, currentAngle, 0);
    }

    /// <summary>
    /// 난사(Spray) 패턴을 위한 무작위 집탄율 방향을 반환합니다.
    /// </summary>
    /// <param name="baseRotation">발사 중심 방향</param>
    /// <param name="maxSpreadAngle">최대 흩뿌림 각도 (예: 30도면 -15도 ~ +15도 사이로 튐)</param>
    /// <returns>랜덤하게 틀어진 최종 회전값</returns>
    public static Quaternion GetRandomSpread(Quaternion baseRotation, float maxSpreadAngle)
    {
        // 1. 탑다운 뷰이므로 Y축(좌우 방향) 기준으로 랜덤 각도를 뽑습니다.
        // 최대 각도가 30도라면, 중심을 기준으로 좌로 15도, 우로 15도 내에서 튀어야 합니다.
        float halfAngle = maxSpreadAngle * 0.5f;
        float randomYaw = UnityEngine.Random.Range(-halfAngle, halfAngle);

        // 2. 랜덤한 회전값을 생성
        Quaternion spreadOffset = Quaternion.Euler(0, randomYaw, 0);

        // 3. 원래 바라보던 방향(baseRotation)에 랜덤 회전값(spreadOffset)을 더해줍니다(곱셈).
        return baseRotation * spreadOffset;
    }
}

//////////////////////////////////////////////////////////////////////////////
public static class AnimatorLayerCache
{
    private static readonly Dictionary<Animator, Dictionary<string, int>> cache
        = new();

    public static int GetLayerIndex(Animator animator, string layerName)
    {
        if (string.IsNullOrEmpty(layerName))
            return 0; // 기본 레이어 인덱스는 0

        if (!cache.TryGetValue(animator, out var animatorCache))
        {
            animatorCache = new Dictionary<string, int>();
            cache[animator] = animatorCache;
        }

        if (!animatorCache.TryGetValue(layerName, out var index))
        {
            index = animator.GetLayerIndex(layerName);
            animatorCache[layerName] = index;
        }

        return index;
    }
}

//////////////////////////////////////////////////////////////////////////////

public static class Extend_Skill
{
    public static T GetValue<T>(this Dictionary<string, object> dict, string key
        , T defaultValue = default)
    {
        if(dict != null && dict.TryGetValue(key, out var value) && value is T castedValue )
        {
            return castedValue;
        }

        return defaultValue;
    }

    public static void SetValue(this Dictionary<string, object> dict, string key, 
        object value)
    {
        if (dict == null) return;

        dict[key] = value;
    }
}

//////////////////////////////////////////////////////////////////////////////
