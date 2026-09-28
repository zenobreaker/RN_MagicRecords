using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UICharacterStatusPopup : UiBase
{
    [SerializeField] private RectTransform characterBar;
    [SerializeField] private UICharacterSelectButton characterButtonPrefab;

    private List<UICharacterSelectButton> characterButtons = new();

    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private ScrollRect scrollRect;

    private List<int> characterIds = new();
    private int selectedCharacterId = -1;

    protected override void Awake()
    {
        base.Awake();

        if (characterBar == null)
            characterBar = GameObject.Find("CharacterBar").SafeInvoke(v=>v.GetComponent<RectTransform>());
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        selectedCharacterId = -1;
        RefreshUI();
    }

    public override void RefreshUI()
    {
        if (statusText == null) return;
        characterIds = ExploreBuildViewData.ReadCharacterIds();
        if (!characterIds.Contains(selectedCharacterId))
            selectedCharacterId = characterIds.Count > 0 ? characterIds[0] : -1;
        DrawCharacterButtons();
        statusText.text = ExploreBuildViewData.ReadCharacterStatus(selectedCharacterId);
        Canvas.ForceUpdateCanvases();
        statusText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, statusText.preferredHeight + 40f);
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    public void SelectCharacter(int characterId)
    {
        if (!characterIds.Contains(characterId)) return;
        selectedCharacterId = characterId;
        RefreshUI();
    }

    private void DrawCharacterButtons()
    {
        if (characterBar == null)
            return;

        Debug.Assert(characterButtonPrefab != null, $"프리팹 데이터 없음");

        while (characterButtons.Count < characterIds.Count)
        {
            var button = Instantiate(characterButtonPrefab);
            button.transform.SetParent(characterBar, false);
            button.gameObject.layer = gameObject.layer;
            
            characterButtons.Add(button);
        }

        for (int index = 0; index < characterButtons.Count; index++)
        {
            var button = characterButtons[index];
            button.gameObject.SetActive(index < characterIds.Count);
            
            if (index >= characterIds.Count) continue;
            
            int id = characterIds[index];
            var info = PlayerManager.Instance.SafeInvoke(players => players.GetCharacterInfo(id));

            bool selected = id == selectedCharacterId;
            button.Setup(id, info, selected, SelectCharacter);
        }
    }

    public override void CloseUI()
    {
        if (UIManager.Instance != null) UIManager.Instance.CloseSpecificUI(this);
        else base.CloseUI();
    }
}
