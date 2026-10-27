using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordCard : MonoBehaviour
{
    public event Action<RecordData> OnRecordData;
    public event Action<RecordCard, RecordData> OnRecordData_Two;

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image memoryIconImage;
    [SerializeField] private Image selectFrame;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button lockButton;
    [SerializeField] private TextMeshProUGUI lockText;

    [Header("Description / Keywords")]
    [SerializeField] private List<string> keywords = new();
    [SerializeField] private Button keywordToggleButton;
    [SerializeField] private TextMeshProUGUI keywordToggleText;
    [SerializeField] private ScrollRect keywordScroll;
    [SerializeField] private GridLayoutGroup keywordLayout;
    [SerializeField] private Image keywordTemplate;
    [SerializeField] private TextMeshProUGUI emptyKeywordText;

    private readonly List<Image> keywordItems = new();
    public IReadOnlyList<string> Keywords
    {
        get
        {
            return keywords;
        }
    }
    public bool IsShowingKeywords
    {
        get;
        private set;
    }

    private void Awake()
    {
        if (keywordToggleButton != null)
        {
            keywordToggleButton.onClick.AddListener(ToggleKeywords);
        }

        SetKeywords(keywords);
    }

    private void OnEnable()
    {
        SetKeywordView(false);
    }

    private void OnDestroy()
    {
        if (keywordToggleButton != null)
        {
            keywordToggleButton.onClick.RemoveListener(ToggleKeywords);
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateKeywordCellSize();
    }

    /// <summary>Creates one badge per non-empty text value or localization key.</summary>
    public void SetKeywords(IEnumerable<string> values)
    {
        // Copy first: callers may pass this card's own list.
        var next = new List<string>();
        if (values != null)
        {
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    next.Add(value);
                }
            }
        }

        keywords = next;

        if (keywordTemplate != null && keywordLayout != null)
        {
            keywordTemplate.gameObject.SetActive(false);
            for (int i = 0; i < keywords.Count; i++)
            {
                if (i >= keywordItems.Count)
                {
                    keywordItems.Add(Instantiate(keywordTemplate, keywordLayout.transform));
                }

                var item = keywordItems[i];
                item.gameObject.SetActive(true);
                var label = item.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetText(keywords[i]) : keywords[i];
                }
            }
            for (int i = keywords.Count; i < keywordItems.Count; i++)
            {
                keywordItems[i].gameObject.SetActive(false);
            }
        }
        if (emptyKeywordText != null)
        {
            emptyKeywordText.gameObject.SetActive(keywords.Count == 0);
        }

        UpdateKeywordCellSize();
        SetKeywordView(false);
    }

    public void ToggleKeywords()
    {
        SetKeywordView(IsShowingKeywords == false);
    }

    public void SetKeywordView(bool showKeywords)
    {
        // Older card variants without a keyword panel keep their description visible.
        bool hasKeywords = keywords != null && keywords.Count > 0 && keywordScroll != null;
        if (keywordToggleButton != null)
        {
            keywordToggleButton.interactable = hasKeywords;
            keywordToggleButton.gameObject.SetActive(hasKeywords);
        }
        IsShowingKeywords = showKeywords && hasKeywords;
        if (descText != null)
        {
            descText.gameObject.SetActive(IsShowingKeywords == false);
        }

        if (keywordScroll != null)
        {
            keywordScroll.gameObject.SetActive(IsShowingKeywords);
            if (IsShowingKeywords)
            {
                UpdateKeywordCellSize();
                LayoutRebuilder.ForceRebuildLayoutImmediate(keywordScroll.content);
                keywordScroll.StopMovement();
                keywordScroll.verticalNormalizedPosition = 1f;
            }
        }
        if (keywordToggleText != null)
        {
            keywordToggleText.text = IsShowingKeywords ? "설명" : "키워드";
        }
    }

    private void UpdateKeywordCellSize()
    {
        if (keywordLayout == null || keywordScroll == null || keywordScroll.viewport == null)
        {
            return;
        }

        keywordLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        keywordLayout.constraintCount = 3;
        float width = keywordScroll.viewport.rect.width - keywordLayout.padding.horizontal
            - keywordLayout.spacing.x * 2f;
        keywordLayout.cellSize = new Vector2(Mathf.Max(1f, width / 3f), 44f);
    }

    public RecordData myData
    {
        get;
        private set;
    } // 부모가 읽을 수 있게 프로퍼티로 변경

    public void Setup(RecordData data,
        System.Action onClickAction = null,
        System.Action onlockAction = null,
        bool canReroll = true)
    {
        myData = data;
        SetKeywords(data.keywords);
        DrawLockText();

        Debug.Assert(LocalizationManager.Instance != null);

        // 1. UI 텍스트 업데이트
        nameText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(data.recordName) : data.recordName;
        descText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(data.description) : data.description;
        SetIcon(data.icon, data.type == RecordType.AUGMENT);

        // 2. 버튼 리스너 초기화 및 재할당
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() =>
        {
            onClickAction?.Invoke();
            OnRecordData?.Invoke(myData);
            OnRecordData_Two?.Invoke(this, myData);
        });

        // 3. 등급(Rarity)에 따른 카드 테두리 색상 변경 등 연출 추가 가능
        // SetRarityColor(data.rarity);

        // 4. 잠금 기능
        if (lockButton != null)
        {
            lockButton.onClick.RemoveAllListeners();
            lockButton.onClick.AddListener(() =>
            {
                onlockAction?.Invoke();
                DrawLockText();
            });

            lockButton.gameObject.SetActive(canReroll);
        }
    }

    public void SetupBuildEntry(BuildEntryViewData entry, Action onClick)
    {
        myData = null;
        SetKeywords(entry.Keywords);
        ClearEvent();
        if (nameText != null)
        {
            nameText.text = entry.Name;
        }

        if (descText != null)
        {
            descText.text = $"[{entry.Label}]\n{entry.Description}";
        }

        SetIcon(entry.Icon, entry.Type == RecordType.AUGMENT);
        if (selectFrame != null)
        {
            selectFrame.gameObject.SetActive(false);
        }

        if (lockButton != null)
        {
            lockButton.gameObject.SetActive(false);
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    /// <summary>Displays the icon in the prefab slot for its record type.</summary>
    private void SetIcon(Sprite sprite, bool isMemory)
    {
        bool useMemory = isMemory && memoryIconImage != null;
        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(useMemory == false);
            iconImage.sprite = useMemory ? null : sprite;
            iconImage.enabled = useMemory == false && sprite != null;
        }
        if (memoryIconImage != null)
        {
            memoryIconImage.gameObject.SetActive(useMemory);
            memoryIconImage.sprite = useMemory ? sprite : null;
            memoryIconImage.enabled = useMemory && sprite != null;
        }
    }

    public void ClearEvent()
    {
        selectButton.SafeInvoke(button => button.onClick.RemoveAllListeners());
        lockButton.SafeInvoke(button => button.onClick.RemoveAllListeners());
    }

    public void Refresh(bool isSelected)
    {
        selectFrame.gameObject.SetActive(isSelected);
    }

    public void ShowCard()
    {
        gameObject.SetActive(true);
    }

    private void DrawLockText()
    {
        if (lockText == null)
        {
            return;
        }

        lockText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(myData.isLocked ? "ui_record_unlock" : "ui_record_lock") : (myData.isLocked ? "Unlock" : "Lock");
    }

    public void SetDisabled(bool disabled)
    {
        //selectButton.interactable = !disabled;

        // 시각 효과
        selectFrame.gameObject.SetActive(disabled == false);
    }
}
