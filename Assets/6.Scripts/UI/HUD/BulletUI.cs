using UnityEngine;
using UnityEngine.UI;

public class BulletUI : MonoBehaviour
{
    [SerializeField] Image uiImage;

    private void Awake()
    {
        uiImage = GetComponent<Image>();
    }

    public void DrawBulletUI(BulletData data)
    {
        if(data.isCrit)
            uiImage.color = Color.red;
        else 
            uiImage.color = Color.yellow;
    }
}
