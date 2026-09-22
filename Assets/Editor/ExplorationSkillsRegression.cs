#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ExplorationSkillsRegression
{
    const string Request = "Library/ExplorationSkills.request";
    const string Report = "Library/ExplorationSkills-result.txt";
    static ExplorationSkillsRegression() => EditorApplication.update += Tick;
    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string action;
        try { action = File.ReadAllText(Request).Trim(); File.Delete(Request); }
        catch (IOException) { return; }
        try
        {
            if (action == "checks") CheckPlayMode();
        }
        catch (Exception e) { File.AppendAllText(Report, e + "\n"); Debug.LogException(e); }
    }
    static T Field<T>(object owner, string field) => (T)owner.GetType().GetField(field,
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(owner);
    static void Check(bool condition, string message)
    {
        File.AppendAllText(Report, (condition ? "PASS " : "FAIL ") + message + "\n");
        if (!condition) throw new Exception(message);
    }
    static Button[] SlotButtons(UISkillOnlyReplaceSlots group) => Field<Button[]>(group, "slots");

    [MenuItem("Tools/Exploration Skills/Check Preparation (Sandbox Play Mode)")]
    public static void CheckPlayMode()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")))
            throw new Exception("Use ShopRegression sandbox Play Mode to protect player saves.");
        File.WriteAllText(Report, $"Unity {Application.unityVersion} actual Play Mode / preparation prefab\n");
        var app = AppManager.Instance;
        var tree = SkillTreeManager.Instance;
        var all = tree.GetAvailableSkills(1).ToList();
        var active = all.Where(s => s.template is SO_ActiveSkillData).ToList();
        Check(active.Count >= 3, "real class data has enough active skills for regression");
        // Runtime data only; no ScriptableObject levels or user saves are edited.
        foreach (var s in all) { s.currentLevel = 1; s.isUnlocked = true; }
        active[0].currentLevel = 0;
        var first = active[1]; var second = active[2];
        for (int i = 0; i < 4; i++) app.UnequipActiveSkill(1, i);
        int balance = CurrencyManager.Instance.GetCurrency(CurrencyType.EXPOLORE_COIN);
        var ui = UIManager.Instance.OpenUI<UIExplorationSetup>();
        var character = ui.GetComponentInChildren<CharacterSelectUI>(true);
        character.SelectClass(1);
        Field<Button>(ui, "nextButton").onClick.Invoke();
        var view = ui.GetComponentInChildren<UIExplorationSkills>();
        Check(view != null && view.IsReady && view.GetComponent<CanvasGroup>().alpha == 1,
            "real character -> next -> embedded preparation page visible and ready");
        Check(view.GetComponent<UIRecordSkillUpPopUp>() == null && Field<Button>(ui, "startButton").interactable,
            "preparation does not invoke event transactions; exploration start remains available");
        var entries = Field<System.Collections.Generic.List<Button>>(view, "entries");
        var expected = all.Where(s => s.template is SO_ActiveSkillData && s.currentLevel >= 1).OrderBy(s=>s.GetSkillID()).ToList();
        Check(entries.Count(b=>b.gameObject.activeSelf) == expected.Count && expected.Select(s=>"LearnedSkill_"+s.GetSkillID()).SequenceEqual(entries.Where(b=>b.gameObject.activeSelf).Select(b=>b.name)),
            "all learned active skills in deterministic order; excludes level-zero unlocked and passive skills");
        var group = Field<UISkillOnlyReplaceSlots>(view, "equippedSlots");
        var slots = SlotButtons(group);
        var unequip = Field<Button>(view, "unequipButton");
        Button Entry(SkillRuntimeData data) => entries.First(b=>b.name == "LearnedSkill_" + data.GetSkillID());
        var context = new ExplorationSetupData { SelectedCharacterId = 1, SelectedClassId = 1 };
        Check(slots.Length == 4 && !unequip.interactable, "four existing icon slots; empty selection cannot unequip");
        slots[1].onClick.Invoke();
        Entry(first).onClick.Invoke();
        Check(app.GetEquippedActiveSkillListByCharID(1)[1] == first && unequip.interactable, "slot first -> skill equips immediately and enables unequip");
        Check(SaveManager.LoadCharInfoData().charInfoList.Single(c=>c.charId==1).equippedSkillIds[1] == first.GetSkillID(), "equipment written to existing character save immediately");
        Entry(second).onClick.Invoke();
        Check(app.GetEquippedActiveSkillListByCharID(1)[1] == first, "completed pair is consumed; next skill waits for a slot");
        slots[2].onClick.Invoke();
        Check(app.GetEquippedActiveSkillListByCharID(1)[2] == second, "skill first -> slot equips immediately");
        Entry(first).onClick.Invoke(); slots[3].onClick.Invoke();
        Check(app.GetEquippedActiveSkillListByCharID(1)[1] == null && app.GetEquippedActiveSkillListByCharID(1)[3] == first,
            "already equipped skill moves between slots without duplication");
        Check(SaveManager.LoadCharInfoData().charInfoList.Single(c=>c.charId==1).equippedSkillIds[1] == 0 &&
            SaveManager.LoadCharInfoData().charInfoList.Single(c=>c.charId==1).equippedSkillIds[3] == first.GetSkillID(), "move persists source removal and destination together");
        slots[2].onClick.Invoke(); Entry(first).onClick.Invoke();
        Check(app.GetEquippedActiveSkillListByCharID(1)[3] == null && app.GetEquippedActiveSkillListByCharID(1)[2] == first,
            "moving onto an occupied slot replaces its previous skill");
        Check(Field<TMP_Text>(view, "skillNameText").text == UIRecordSkillUpPopUp.Text(first.GetSkillName(), first.GetSkillName()) &&
            Field<TMP_Text>(view, "detailText").text.StartsWith("Lv."), "skill name has a separate text object from level and description");
        unequip.onClick.Invoke(); unequip.onClick.Invoke();
        Check(app.GetEquippedActiveSkillListByCharID(1)[2] == null && !unequip.interactable &&
            SaveManager.LoadCharInfoData().charInfoList.Single(c=>c.charId==1).equippedSkillIds[2] == 0, "unequip saves immediately; repeated click is harmless");
        Entry(second).onClick.Invoke(); slots[0].onClick.Invoke();
        var saved = SaveManager.LoadCharInfoData().charInfoList.Single(c=>c.charId==1);
        SkillManager.Instance.ResetRunTimeData();
        app.EquipSavedClassActiveSkill(1, saved.equippedSkillIds);
        view.SetContext(context);
        Check(app.GetEquippedActiveSkillListByCharID(1)[0] == second && slots[0].image.sprite == second.template.skillImage,
            "existing save read and equipment restore API restores icons and equipped skills");
        Field<Button>(ui, "prevButton").onClick.Invoke(); Field<Button>(ui, "nextButton").onClick.Invoke();
        Check(view.gameObject.activeInHierarchy && app.GetEquippedActiveSkillListByCharID(1)[0] == second, "back/next retains saved equipment and rebuilds candidates");
        var classIds = Field<System.Collections.Generic.Dictionary<int, SkillTree>>(tree, "skillByClassIdTable").Keys.ToList();
        foreach (int classId in classIds)
        {
            view.SetContext(new ExplorationSetupData { SelectedCharacterId = 1, SelectedClassId = classId });
            var eligible = tree.GetAvailableSkills(classId).Where(s=>s.template is SO_ActiveSkillData && s.currentLevel >= 1).OrderBy(s=>s.GetSkillID()).Select(s=>s.GetSkillID());
            Check(Field<System.Collections.Generic.List<SkillRuntimeData>>(view, "availableSkills").Select(s=>s.GetSkillID()).SequenceEqual(eligible), "selected class filter matches existing availability API: " + classId);
        }
        foreach (var s in active) s.currentLevel = 0;
        view.SetContext(context);
        Check(entries.All(b=>!b.gameObject.activeSelf) && Field<TMP_Text>(view, "statusText").text.Contains("없습니다"), "no learned active skills: empty list and explanatory message");
        foreach (var s in active.Skip(1)) s.currentLevel = 1;
        view.SetContext(context);
        var scroll = Field<ScrollRect>(view, "skillScroll");
        Check(scroll.vertical && !scroll.horizontal && scroll.GetComponent<RectMask2D>() != null, "vertical-only scroll with clipping");
        var placeholders = new System.Collections.Generic.List<GameObject>();
        for (int i=0;i<24;i++)
        {
            var item = new GameObject("ScrollOverflowCheck", typeof(RectTransform));
            item.transform.SetParent(scroll.content, false); placeholders.Add(item);
        }
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        Canvas.ForceUpdateCanvases();
        float beforeY = scroll.content.anchoredPosition.y;
        scroll.OnScroll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { scrollDelta = new Vector2(0,-10) });
        Check(scroll.content.rect.height > scroll.viewport.rect.height && scroll.content.anchoredPosition.y > beforeY && Mathf.Approximately(scroll.content.anchoredPosition.x, 0),
            "overflow layout grows and actual scroll handler moves content only vertically");
        foreach (var item in placeholders) Object.DestroyImmediate(item);
        view.SetContext(context);
        Check(!view.GetComponentsInChildren<Button>(true).Any(b=>b.name == "ApplyAndClose" || b.name == "Upgrade" || b.name == "Replace"), "apply/close and paid upgrade/replacement buttons removed from preparation");
        Check(CurrencyManager.Instance.GetCurrency(CurrencyType.EXPOLORE_COIN) == balance && active.Skip(1).All(s=>s.currentLevel==1), "equipping and unequipping spend no exploration currency and never level skills");
        var eventPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/UI/PopUp/UIRecordSkillUpPopUp.prefab");
        Check(eventPrefab.GetComponent<UIRecordSkillUpPopUp>() != null && eventPrefab.GetComponent<UIExplorationSkills>() == null,
            "standalone exploration event/shop prefab retains its existing controller");
        SkillEventRegression.Run();
        Check(true, "existing skill event regression completed");
        Debug.Log("EXPLORATION_PREPARATION_CHECKS_PASS");
    }

}
#endif
