using System;
using System.Collections.Generic;

public static class UIRegistry
{
    private static Dictionary<Type, object> uiControllers = new();

    public static void Register<T>(T controller) where T : class
    {
        uiControllers[typeof(T)] = controller;
    }

    public static T Get<T>() where T : class
    {
        uiControllers.TryGetValue(typeof(T), out var controller);
        return controller as T;
    }

    public static void Clear() => uiControllers.Clear();
}
