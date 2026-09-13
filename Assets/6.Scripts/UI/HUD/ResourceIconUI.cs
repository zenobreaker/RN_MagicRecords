using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ResourceIconUI : MonoBehaviour
{
    [SerializeField] private Image uiImage;
    public void SetAppearance(Sprite sprite, Color color)
    {
        if (uiImage == null) uiImage = GetComponent<Image>();
        uiImage.sprite = sprite;
        uiImage.color = color;
    }
}
