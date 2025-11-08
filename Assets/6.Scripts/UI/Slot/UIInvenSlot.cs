using System;
using UnityEngine;
using UnityEngine.UI;

public class UIInvenSlot : UIItemSlot
{
    private void Awake()
    {
        itemImage = GetComponent<Image>();
    }
}
