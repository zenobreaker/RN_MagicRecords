using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemDebug : MonoBehaviour
{
    private void Start()
    {
        Debug.Log($"Mouse = {Mouse.current}");
        Debug.Log($"Keyboard = {Keyboard.current}");

        foreach (var device in InputSystem.devices)
        {
            Debug.Log($"Device : {device.displayName} / {device.layout}");
        }
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            Debug.LogError("Mouse.current == NULL");
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("NEW INPUT SYSTEM : LEFT CLICK");
        }
    }
}