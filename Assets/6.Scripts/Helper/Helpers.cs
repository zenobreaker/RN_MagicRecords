using System.Collections.Generic;
using UnityEngine;

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

        foreach(T t in trasnforms)
        {
            gameObjects.Add(t.gameObject);
        }

        return gameObjects.ToArray();
    }

    public static Vector3 FindGreaterBounds(this Transform transform)
    {
        Renderer[] renderers = transform.GetComponentsInChildren<Renderer>();

        Vector3 result = Vector3.zero; 
        foreach(Renderer r in renderers)
        {
            Vector3 size = r.bounds.size;
            if(size.magnitude > result.magnitude)
                result = size;
        }

        return result;
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
