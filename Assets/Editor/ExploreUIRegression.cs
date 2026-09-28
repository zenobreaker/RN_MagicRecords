using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class ExploreUIRegression
{
    [MenuItem("Tools/Explore UI/Validate Popup Assets")]
    public static void ValidateAssets()
    {
        var database = AssetDatabase.LoadAssetAtPath<UIDatabase>("Assets/10.ScriptableObjects/UIDatabase.asset");
        var popup = database != null ? database.GetPrefab<UICharacterStatusPopup>() : null;
        Require(popup != null, "Character Status Popup is registered in UIDatabase");
        var serialized = new SerializedObject(popup);
        var text = serialized.FindProperty("statusText").objectReferenceValue as TextMeshProUGUI;
        var scroll = serialized.FindProperty("scrollRect").objectReferenceValue as ScrollRect;
        Require(text != null && text.font != null, "Status text and project font are linked");
        Require(scroll != null && scroll.content == text.rectTransform && scroll.vertical, "Status text is the vertical scroll content");
        var pause = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/UI/PopUp/StagePausePopUp.prefab").GetComponent<UIPopUpPause>();
        Require(new SerializedObject(pause).FindProperty("statusButton").objectReferenceValue is Button, "Pause menu Status button is linked");
        Require(popup.GetComponentsInChildren<Button>(true).Length > 0, "Popup has its existing close button");
        var rewards = database.GetPrefab<UIRewardCardPopUp>();
        Require(rewards != null, "Existing reward-card popup is registered");
        var rewardFields = new SerializedObject(rewards);
        Require(rewardFields.FindProperty("titleText").objectReferenceValue is TMP_Text &&
            rewardFields.FindProperty("continueText").objectReferenceValue is TMP_Text,
            "Chapter title and receipt label are linked");
        bool receiptButton = false;
        foreach (var button in rewards.GetComponentsInChildren<Button>(true))
            for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
                receiptButton |= button.onClick.GetPersistentMethodName(index) == nameof(UIRewardCardPopUp.OnContinueButton);
        Require(receiptButton, "Reward receipt button is wired");
        Debug.Log("Explore UI asset checks passed.");
    }

    [MenuItem("Tools/Explore UI/Check Current Run")]
    public static void CheckCurrentRun()
    {
        Require(EditorApplication.isPlaying && UIManager.Instance != null && UIManager.Instance.IsInGame(),
            "Run this check during an exploration battle in Play Mode.");
        ValidateAssets();
        var ui = UIManager.Instance;
        var previousPause = UnityEngine.Object.FindAnyObjectByType<UIPopUpPause>();
        var previousStatus = UnityEngine.Object.FindAnyObjectByType<UICharacterStatusPopup>();
        try
        {
            ui.OpenPausePopUp();
            var pause = UnityEngine.Object.FindAnyObjectByType<UIPopUpPause>();
            Require(pause != null, "Pause menu opened");
            var expected = ExploreBuildViewData.ReadCharacterStatus();
            var button = new SerializedObject(pause).FindProperty("statusButton").objectReferenceValue as Button;
            button.onClick.Invoke();
            var popup = UnityEngine.Object.FindAnyObjectByType<UICharacterStatusPopup>();
            Require(popup != null && popup.gameObject.activeInHierarchy, "Status button opened the popup");
            var text = new SerializedObject(popup).FindProperty("statusText").objectReferenceValue as TextMeshProUGUI;
            Require(text.text == expected, "Popup displays the current character snapshot");
            CheckCharacterSelection(popup);
            popup.CloseUI();
            Require(pause.gameObject.activeInHierarchy, "Closing Status preserves the pause menu");
            button.onClick.Invoke();
            Require(text.text == ExploreBuildViewData.ReadCharacterStatus(), "Reopening reads current data again");
            var inventory = pause.GetComponentInChildren<UIRecordInventory>(true);
            Require(inventory != null, "Existing Record inventory is reused");
            inventory.RefreshUI();
            int visibleCards = 0;
            foreach (var card in inventory.GetComponentsInChildren<RecordCard>(true))
                if (card.gameObject.activeInHierarchy) visibleCards++;
            Require(visibleCards == ExploreBuildViewData.ReadInventory().Count, "Record inventory includes job passives");
            Debug.Log("Explore UI current-run button, popup, refresh and inventory checks passed.");
        }
        finally
        {
            if (previousStatus == null) UnityEngine.Object.FindAnyObjectByType<UICharacterStatusPopup>().SafeInvoke(popup => popup.CloseUI());
            if (previousPause == null) UnityEngine.Object.FindAnyObjectByType<UIPopUpPause>().SafeInvoke(popup => popup.CloseUI());
        }
    }

    [MenuItem("Tools/Explore UI/Check Node Scene")]
    public static void CheckNodeScene()
    {
        Require(EditorApplication.isPlaying &&
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "StageSelectScene",
            "Run this check in the node selection scene in Play Mode.");
        ValidateAssets();
        Button statusButton = null;
        foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
                if (button.onClick.GetPersistentMethodName(index) == nameof(StageUIController.OnCharacterStatusButton))
                    statusButton = button;
        Require(statusButton != null, "Node scene Status button is wired");
        var previous = UnityEngine.Object.FindAnyObjectByType<UICharacterStatusPopup>();
        try
        {
            statusButton.onClick.Invoke();
            var popup = UnityEngine.Object.FindAnyObjectByType<UICharacterStatusPopup>();
            Require(popup != null, "Node scene Status button opens the existing popup");
            var text = new SerializedObject(popup).FindProperty("statusText").objectReferenceValue as TextMeshProUGUI;
            Require(text != null && text.text == ExploreBuildViewData.ReadCharacterStatus(),
                "Node popup displays the current exploration character");
            CheckCharacterSelection(popup);
            Debug.Log("Explore UI node-scene button and popup checks passed.");
        }
        finally
        {
            if (previous == null) UnityEngine.Object.FindAnyObjectByType<UICharacterStatusPopup>().SafeInvoke(popup => popup.CloseUI());
        }
    }

    private static void CheckCharacterSelection(UICharacterStatusPopup popup)
    {
        var ids = ExploreBuildViewData.ReadCharacterIds();
        var bar = popup.transform.Find("CharacterSelection");
        Require(bar != null && ids.Count > 0, "Character selection bar was created");
        var buttons = bar.GetComponentsInChildren<Button>();
        Require(buttons.Length == ids.Count, "Only exploration participants are displayed");
        var text = new SerializedObject(popup).FindProperty("statusText").objectReferenceValue as TextMeshProUGUI;
        Require(text.text == ExploreBuildViewData.ReadCharacterStatus(ids[0]), "First participant is selected automatically");
        for (int index = 0; index < ids.Count; index++)
        {
            var portrait = buttons[index].transform.Find("Portrait").GetComponent<Image>();
            var info = PlayerManager.Instance.GetCharacterInfo(ids[index]);
            Require(portrait.sprite == info.charSprite && portrait.enabled, "Character portrait is displayed");
            buttons[index].onClick.Invoke();
            Require(text.text == ExploreBuildViewData.ReadCharacterStatus(ids[index]), "Selection refreshes the displayed character");
        }
        Require(!System.Text.RegularExpressions.Regex.IsMatch(text.text, @"\d+[.,]\d+"), "Stats contain no decimal fractions");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
