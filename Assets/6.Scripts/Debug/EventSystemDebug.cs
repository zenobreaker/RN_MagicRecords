using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class EventSystemDebug : MonoBehaviour
{
    private void Start()
    {
        var es = EventSystem.current;
        var module = es.currentInputModule as InputSystemUIInputModule;

        Debug.Log($"EventSystem : {es}");
        Debug.Log($"InputModule : {module}");

        if (module != null)
        {
            Debug.Log($"Point     : {module.point?.action?.name}");
            Debug.Log($"Point Enabled : {module.point?.action?.enabled}");

            Debug.Log($"LeftClick : {module.leftClick?.action?.name}");
            Debug.Log($"LeftClick Enabled : {module.leftClick?.action?.enabled}");
        }
    }

    private void Update()
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem.current == NULL");
            return;
        }

        Debug.Log(
            $"EventSystem={EventSystem.current.name}, " +
            $"CurrentInputModule={EventSystem.current.currentInputModule?.GetType().Name}"
        );
    }
}