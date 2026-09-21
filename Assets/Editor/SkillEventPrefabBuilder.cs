#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SkillEventPrefabBuilder
{
    public const string Folder = "Assets/5.Prefabs/UI/PopUp/";
    private static TMP_FontAsset font;
    private static Sprite panelSprite, buttonSprite;

    [MenuItem("Tools/Event/Rebuild Skill Event Prefabs")]
    public static void Build()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/99.Fonts/DNFBitBitTTF SDF.asset");
        panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/7.Sprites/UI/news/base.png");
        buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/7.Sprites/UI/news/button.png");
        BuildSkillPopup(); BuildConfirmation();
        var db = AssetDatabase.LoadAssetAtPath<UIDatabase>("Assets/10.ScriptableObjects/UIDatabase.asset");
        if (db != null)
        {
            foreach (var path in new[] { "UIRecordSkillUpPopUp.prefab", "UISkillEventConfirmation.prefab" })
            {
                var ui = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + path).GetComponent<UiBase>();
                if (!db.uiPrefabs.Contains(ui)) db.uiPrefabs.Add(ui);
            }
            EditorUtility.SetDirty(db);
        }
        AssetDatabase.SaveAssets();
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = pos; return rect;
    }
    private static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static Image Background(RectTransform rect, bool button = false)
    {
        var image = rect.gameObject.AddComponent<Image>(); image.sprite = button ? buttonSprite : panelSprite;
        image.type = button ? Image.Type.Simple : Image.Type.Sliced;
        if (!button) image.pixelsPerUnitMultiplier = 6;
        return image;
    }
    private static TMP_Text Label(string name, Transform parent, string text, Vector2 size, Vector2 pos, int fontSize = 24)
    {
        var label = Rect(name, parent, size, pos).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.color = Color.white; label.fontSize = fontSize;
        label.enableAutoSizing = true; label.fontSizeMin = 14; label.fontSizeMax = fontSize;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }
    private static Button Button(string name, Transform parent, string text, Vector2 size, Vector2 pos)
    {
        var rect = Rect(name, parent, size, pos); var image = Background(rect, true);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Label("Text", rect, text, size - new Vector2(26, 14), Vector2.zero, 22);
        return button;
    }
    private static void Assign(Object target, string name, Object value)
    { var so = new SerializedObject(target); so.FindProperty(name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static GameObject Root(string name)
    {
        var root = Rect(name, null, new Vector2(1920,1080), Vector2.zero).gameObject;
        Stretch((RectTransform)root.transform); root.SetActive(false);
        var image = root.AddComponent<Image>(); image.color = new Color(0,0,0,.78f);
        root.AddComponent<CanvasGroup>(); return root;
    }
    private static void BuildSkillPopup()
    {
        var root = Root("UIRecordSkillUpPopUp");
        var ui = root.AddComponent<UIRecordSkillUpPopUp>();
        var panel = Rect("Panel", root.transform, new Vector2(1520,880), Vector2.zero); Background(panel);
        Assign(ui,"popupArea",panel);
        Label("Title",panel,"스킬 기록 정비",new Vector2(600,60),new Vector2(-365,365),36);
        Assign(ui,"currencyText",Label("ExploreCurrency",panel,"탐사 재화 : 0",new Vector2(480,60),new Vector2(450,365),26));

        // Keep the source group's name and panel roles so designers can compare
        // this popup with UIExplorationSetup/SkillTreeGroup in the Inspector.
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"UIExplorationSetup.prefab");
        var reference = source == null ? null : source.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t=>t.name=="SkillTreeGroup");
        var group = Rect(reference != null ? reference.name : "SkillTreeGroup",panel,new Vector2(1420,650),new Vector2(0,5));
        Label("SkillsHeading",group,"장착 스킬 및 교체 후보",new Vector2(840,44),new Vector2(-220,285),24);
        var viewport = Rect("SkillTree",group,new Vector2(860,510),new Vector2(-220,0));
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        var content = Rect("Content",viewport,new Vector2(840,510),Vector2.zero);
        content.anchorMin = new Vector2(0,1);content.anchorMax = new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,0);
        var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(266,122);grid.spacing=new Vector2(14,14);
        grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=3;grid.padding=new RectOffset(10,10,10,10);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport=viewport;scroll.content=content;
        Assign(ui,"skillContent",content);
        var template=Button("SkillButtonTemplate",content,"스킬\nLv.1",new Vector2(266,122),Vector2.zero);
        var text=template.GetComponentInChildren<TMP_Text>();text.rectTransform.sizeDelta=new Vector2(166,80);text.rectTransform.anchoredPosition=new Vector2(35,0);text.fontSizeMax=20;
        var icon=Rect("Icon",template.transform,new Vector2(62,62),new Vector2(-87,0)).gameObject.AddComponent<Image>();icon.raycastTarget=false;
        template.gameObject.SetActive(false);Assign(ui,"skillButtonTemplate",template);

        var detail=Rect("SkillDetailPannel",group,new Vector2(410,555),new Vector2(485,0));Background(detail);
        var description=Label("Description",detail,"스킬을 선택하세요.",new Vector2(340,330),new Vector2(0,60),24);description.alignment=TextAlignmentOptions.TopLeft;
        Assign(ui,"detailText",description);
        Assign(ui,"replaceButton",Button("Replace",detail,"선택 슬롯에 교체",new Vector2(350,65),new Vector2(0,-145)));
        Assign(ui,"upgradeButton",Button("Upgrade",detail,"레벨 업",new Vector2(350,65),new Vector2(0,-220)));
        var so=new SerializedObject(ui);var slots=so.FindProperty("slotButtons");slots.arraySize=4;
        for(int i=0;i<4;i++)slots.GetArrayElementAtIndex(i).objectReferenceValue=Button("Slot"+(i+1),panel,(i+1)+"\n-",new Vector2(198,85),new Vector2(-550+i*211,-350));
        so.ApplyModifiedPropertiesWithoutUndo();
        Assign(ui,"statusText",Label("Status",panel,"교체할 슬롯과 스킬을 선택하세요.",new Vector2(1300,48),new Vector2(0,-275),21));
        Assign(ui,"closeButton",Button("ApplyAndClose",panel,"적용 후 닫기",new Vector2(350,85),new Vector2(485,-350)));
        root.SetActive(true);PrefabUtility.SaveAsPrefabAsset(root,Folder+root.name+".prefab");Object.DestroyImmediate(root);
    }
    private static void BuildConfirmation()
    {
        var root=Root("UISkillEventConfirmation");var ui=root.AddComponent<UISkillEventConfirmation>();
        var panel=Rect("Panel",root.transform,new Vector2(780,450),Vector2.zero);Background(panel);Assign(ui,"popupArea",panel);
        Assign(ui,"titleText",Label("Title",panel,"확인",new Vector2(640,60),new Vector2(0,150),32));
        Assign(ui,"messageText",Label("Message",panel,"변경사항을 적용하시겠습니까?",new Vector2(630,110),new Vector2(0,45),24));
        var toggleRect=Rect("SkipConfirmation",panel,new Vector2(510,46),new Vector2(0,-65));
        var toggle=toggleRect.gameObject.AddComponent<Toggle>();
        var box=Rect("Box",toggleRect,new Vector2(34,34),new Vector2(-225,0)).gameObject.AddComponent<Image>();box.color=new Color(.15f,.25f,.35f);
        var check=Rect("Checkmark",box.transform,new Vector2(22,22),Vector2.zero).gameObject.AddComponent<Image>();check.color=Color.cyan;
        toggle.targetGraphic=box;toggle.graphic=check;
        Label("Label",toggleRect,"이번 실행 동안 다시 묻지 않기",new Vector2(430,46),new Vector2(30,0),22);
        Assign(ui,"skipToggle",toggle);
        Assign(ui,"confirmButton",Button("Confirm",panel,"확인",new Vector2(275,70),new Vector2(-155,-155)));
        Assign(ui,"cancelButton",Button("Cancel",panel,"취소",new Vector2(275,70),new Vector2(155,-155)));
        root.SetActive(true);PrefabUtility.SaveAsPrefabAsset(root,Folder+root.name+".prefab");Object.DestroyImmediate(root);
    }

    [MenuItem("Tools/Event/Test Skill Upgrade Popup (Play Mode)")]
    public static void OpenTest()
    {
        if (!Application.isPlaying || UIManager.Instance == null) { Debug.LogWarning("탐사를 시작한 Play Mode에서 실행하세요."); return; }
        EventActionProcessor.Execute(new EventChoice { ActionType=EventActionType.RECORD_SKILL_UP, ActionParam=EventActionParam.DRAFT_3, ActionValue=3 });
    }
}
#endif
