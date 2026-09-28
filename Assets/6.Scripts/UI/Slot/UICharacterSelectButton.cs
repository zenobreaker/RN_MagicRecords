using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICharacterSelectButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image portrait;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image selectedFrame;

    private int characterId;

    public void Setup(
        int id,
        CharacterInfo info,
        bool selected,
        System.Action<int> onClick)
    {
        characterId = id;

        portrait.sprite = info?.charSprite;
        portrait.enabled = portrait.sprite != null;

        nameText.text =
            ExploreBuildViewData.Localize(info?.name);

        selectedFrame.gameObject.SetActive(selected);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(
            () => onClick?.Invoke(characterId)
        );
    }
}