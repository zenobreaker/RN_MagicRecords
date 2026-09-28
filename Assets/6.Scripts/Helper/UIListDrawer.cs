using System;
using System.Collections.Generic;
using UnityEngine;


// Ui List 에 아이템들을 배치하게 해주는 Util 함수 
// 각 요소별로 캐시나 플래그 등의 유연한 속성이 필요해지면 static 해제 후 클래스화 할 수 있음
public static class UIListDrawer 
{
    public static void DrawList<TSlot, TData>(
        List<TData> items,
        Action<TSlot, TData, int> onSetupSlot,
        Action<TSlot> onClearSlot, 
        Action<int> initReplaceContent = null,
        Action<Action<TSlot>> setContentCallback = null) where TData : class
    {
        if (items == null)
            return;

        initReplaceContent?.Invoke(items.Count);

        int index = 0;

        setContentCallback(slot =>
        {
            if (index < items.Count)
            {
                onSetupSlot?.Invoke(slot, items[index], index);
                index++;
            }
            else
            {
                onClearSlot?.Invoke(slot);
            }
        });
    }

    /// <summary>
    /// 중첩 스크롤뷰 등 특정 부모(targetParent)와 프리팹(prefab)을 명시하여 리스트를 그립니다.
    /// 객체 풀링(재사용)을 기본적으로 지원합니다.
    /// </summary>
    public static void DrawListToTarget<TSlot, TData>(
        Transform targetParent,
        GameObject prefab,
        List<TData> items,
        Action<TSlot, TData, int> onSetupSlot) where TSlot : Component
    {
        if (items == null || targetParent == null || prefab == null) return;
        if (!prefab.TryGetComponent<TSlot>(out _)) return;

        // 부모 아래의 원본 템플릿은 숨기고, 실제 슬롯만 재사용 목록에 포함합니다.
        var slots = new List<TSlot>();
        for (int i = 0; i < targetParent.childCount; i++)
        {
            var child = targetParent.GetChild(i).gameObject;
            if (child == prefab)
            {
                child.SetActive(false);
                continue;
            }
            if (child.TryGetComponent<TSlot>(out var slot)) slots.Add(slot);
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (i >= slots.Count)
            {
                var clone = UnityEngine.Object.Instantiate(prefab, targetParent);
                slots.Add(clone.GetComponent<TSlot>());
            }
            slots[i].gameObject.SetActive(true);
            onSetupSlot?.Invoke(slots[i], items[i], i);
        }

        // 생성된 슬롯만 풀링하며, 템플릿이나 다른 장식 오브젝트는 슬롯으로 세지 않습니다.
        for (int i = items.Count; i < slots.Count; i++)
            slots[i].gameObject.SetActive(false);
    }

}
