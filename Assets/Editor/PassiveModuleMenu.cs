using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// 전용 에디터와 인스펙터가 같은 모듈 목록/프리셋을 사용합니다.
internal static class PassiveModuleMenu
{
    public static void AddItems(GenericMenu menu, Action<PassiveModule> onSelected, Func<Type, bool> typeFilter = null)
    {
        typeFilter ??= type => (typeof(CommonRecordModule).IsAssignableFrom(type) == false);
        var types = TypeCache.GetTypesDerivedFrom<PassiveModule>()
            .Where(type => type.IsAbstract == false && type.IsGenericType == false && type.IsSerializable &&
                type.GetConstructor(Type.EmptyTypes) != null && (typeFilter == null || typeFilter(type)))
            .OrderBy(GetCategoryPath);
        foreach (var type in types)
        {
            menu.AddItem(new GUIContent(GetCategoryPath(type)), false,
            () => onSelected((PassiveModule)Activator.CreateInstance(type)));
        }

        if (typeFilter != null && typeFilter(typeof(Module_Passive_StatBonus)) == false)
        {
            return;
        }
        // 메뉴 항목만 구분하고, 저장되는 타입은 기존 공용 스탯 모듈입니다.
        menu.AddItem(new GUIContent("Passive/Stat/공격력 증가"), false,
            () => onSelected(CreateStatPreset(StatusType.ATTACK, 10f)));
        menu.AddItem(new GUIContent("Passive/Stat/치명타 확률 증가"), false,
            () => onSelected(CreateStatPreset(StatusType.CRIT_RATIO, 0.05f)));
    }

    private static Module_Passive_StatBonus CreateStatPreset(StatusType stat, float value)
    {
        return new()
        {
            targetStat = stat,
            value = value,
            valueType = ModifierValueType.FIXED
        };
    }

    public static string GetCategoryPath(Type type)
    {
        return type.GetCustomAttribute<ModuleCategoryAttribute>()?.Path ?? $"Etc/{type.Name}";
    }
}
