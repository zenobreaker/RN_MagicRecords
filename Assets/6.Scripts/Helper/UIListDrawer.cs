using System;
using System.Collections.Generic;


// Ui List 에 아이템들을 배치하게 해주는 Util 함수 
// 각 요소별로 캐시나 플래그 등의 유연한 속성이 필요해지면 static 해제 후 클래스화 할 수 있음
public static class UIListDrawer 
{
    public static void DrawList<TSlot, TData>(
        List<TData> items,
        Action<TSlot, TData, int> onSetupSlot,
        Action<TSlot> onClearSlot, 
        Action<int> initReplaceContent,
        Action<Action<TSlot>> setContentCallback) where TData : class
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

}
