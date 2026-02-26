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

        int requiredCount = items.Count;
        int childCount = targetParent.childCount;

        // 필요한 개수와 이미 있는 자식 개수 중 더 큰 값만큼 반복
        for (int i = 0; i < Mathf.Max(requiredCount, childCount); i++)
        {
            if (i < requiredCount)
            {
                GameObject slotObj;
                // 1. 기존에 생성된 UI가 있으면 재사용 (SetActive(true))
                if (i < childCount)
                {
                    slotObj = targetParent.GetChild(i).gameObject;
                }
                // 2. 모자라면 새로 Instantiate
                else
                {
                    slotObj = UnityEngine.Object.Instantiate(prefab, targetParent);
                }

                slotObj.SetActive(true);

                // 3. 콜백을 통해 데이터 세팅
                if (slotObj.TryGetComponent<TSlot>(out var slot))
                {
                    onSetupSlot?.Invoke(slot, items[i], i);
                }
            }
            else
            {
                // 4. 데이터보다 초과해서 남는 UI 객체들은 꺼둠 (오류 방지 및 풀링)
                targetParent.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

}
