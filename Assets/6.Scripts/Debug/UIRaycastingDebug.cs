using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIRaycastDebug : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem.current == NULL");
            return;
        }

        PointerEventData data = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();

        EventSystem.current.RaycastAll(data, results);

        Debug.Log("===== RAYCAST RESULT =====");

        foreach (var result in results)
        {
            Debug.Log(
                $"Hit : {result.gameObject.name} " +
                $" | Module : {result.module.GetType().Name}"
            );
        }
    }
}