#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CheatConsolePrefabBuilder
{
    public const string PrefabPath = "Assets/5.Prefabs/Resources/CheatConsoleUI.prefab";
    private static TMP_FontAsset font;

    [MenuItem("Tools/Cheats/Rebuild Console Prefab")]
    public static void Build()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Build the prefab in Edit Mode.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/99.Fonts/DNFBitBitTTF SDF.asset");
        var root = Rect("CheatConsoleUI", null, Vector2.zero, Vector2.zero).gameObject;
        root.SetActive(false);
        try
        {
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();
            var console = root.AddComponent<CheatConsoleUI>();
            var overlay = Rect("Overlay", root.transform, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            Background(overlay, new Color(0, 0, 0, .72f));
            var panel = Rect("Console", overlay, new Vector2(1280, 850), Vector2.zero);
            Background(panel, new Color(.035f, .05f, .075f, .99f));
            Label("Title", panel, "CHEAT CMD", new Vector2(900, 60), new Vector2(-130, 365), 34);
            Label("Help", panel, CheatCommands.HelpText, new Vector2(1160, 230), new Vector2(0, 195), 22);
            var log = Rect("Log", panel, new Vector2(1160, 330), new Vector2(0, -110));
            Background(log, new Color(.055f, .07f, .1f));
            log.gameObject.AddComponent<RectMask2D>();
            var scroll = log.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30; scroll.viewport = log;
            var output = Label("Output", log, "명령어를 입력하고 Enter를 누르세요.", new Vector2(1160, 0), Vector2.zero, 22);
            output.rectTransform.anchorMin = new Vector2(0, 1); output.rectTransform.anchorMax = Vector2.one;
            output.rectTransform.pivot = new Vector2(.5f, 1); output.rectTransform.sizeDelta = Vector2.zero;
            output.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = output.rectTransform;
            output.alignment = TextAlignmentOptions.TopLeft;
            output.overflowMode = TextOverflowModes.Overflow;
            var inputRect = Rect("Command", panel, new Vector2(980, 64), new Vector2(-90, -330));
            var background = Background(inputRect, new Color(.1f, .14f, .2f));
            var input = inputRect.gameObject.AddComponent<TMP_InputField>();
            var viewport = Rect("Viewport", inputRect, new Vector2(940, 58), Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            var text = Label("Text", viewport, "", new Vector2(940, 58), Vector2.zero, 25);
            var placeholder = Label("Placeholder", viewport, "gain_skill 1001 1 10", new Vector2(940, 58), Vector2.zero, 25);
            placeholder.color = new Color(.5f, .6f, .7f);
            input.targetGraphic = background;
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 80;
            input.richText = false;
            var execute = Button("Execute", panel, "실행", new Vector2(150, 64), new Vector2(505, -330));
            var close = Button("Close", panel, "닫기", new Vector2(120, 52), new Vector2(520, 365));
            Label("Hint", panel, "` / Esc : 닫기     |     입력 중 전투 일시정지     |     결과 영역: 마우스 휠로 스크롤", new Vector2(1160, 40), new Vector2(0, -395), 19);
            Assign(console, "panel", overlay.gameObject);
            Assign(console, "commandInput", input);
            Assign(console, "output", output);
            Assign(console, "outputScroll", scroll);
            Assign(console, "executeButton", execute);
            Assign(console, "closeButton", close);
            overlay.gameObject.SetActive(false);
            root.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }
    private static Image Background(RectTransform rect, Color color)
    { var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image; }
    private static TextMeshProUGUI Label(string name, Transform parent, string value, Vector2 size, Vector2 position, int fontSize)
    {
        var text = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = fontSize; text.text = value;
        text.color = new Color(.8f, .92f, 1f); text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        return text;
    }
    private static Button Button(string name, Transform parent, string title, Vector2 size, Vector2 position)
    {
        var rect = Rect(name, parent, size, position);
        var image = Background(rect, new Color(.14f, .25f, .34f));
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var text = Label("Label", rect, title, size, Vector2.zero, 24);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }
    private static void Assign(Object target, string property, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
