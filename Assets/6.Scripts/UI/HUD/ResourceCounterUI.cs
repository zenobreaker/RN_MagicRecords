using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Reusable icon/count display. Other resource systems can call SetCount directly.</summary>
public class ResourceCounterUI : MonoBehaviour
{
    [Header("Optional magic bullet event source")]
    [SerializeField] private SO_SkillEventHandler handler;
    [SerializeField] private bool bindMagicBulletEvents = true;
    [Header("Resource display")]
    [FormerlySerializedAs("bulletUIObj")]
    [SerializeField] private ResourceIconUI iconPrefab;
    [SerializeField, Min(1)] private int maxVisibleIcons = 10;
    [SerializeField] private TMP_Text countText;
    private readonly List<GameObject> icons = new List<GameObject>();
    private int currentCount;
    private SO_SkillEventHandler subscribedHandler;

    public int MaxVisibleIcons
    {
        get => maxVisibleIcons;
        set { maxVisibleIcons = Mathf.Max(1, value); SetCount(currentCount); }
    }

    private void OnEnable()
    {
        if (bindMagicBulletEvents)
        {
            if (handler == null)
                handler = SkillManager.Instance.SafeInvoke(v => v.SkillEventHandler);
            subscribedHandler = handler;
            if (subscribedHandler != null)
            {
                subscribedHandler.OnUpdateMagicBulletLoad += OnCapacityChanged;
                subscribedHandler.OnChangeBullets += OnBulletsChanged;
            }
        }
        SetCount(currentCount);
    }

    private void OnDisable()
    {
        if (subscribedHandler == null) return;
        subscribedHandler.OnUpdateMagicBulletLoad -= OnCapacityChanged;
        subscribedHandler.OnChangeBullets -= OnBulletsChanged;
        subscribedHandler = null;
    }

    private void OnCapacityChanged(int capacity) => SetCount(Mathf.Min(currentCount, Mathf.Max(0, capacity)));
    private void OnBulletsChanged(Queue<BulletData> resources) => SetCount(resources == null ? 0 : resources.Count);

    public void SetCount(int count)
    {
        currentCount = Mathf.Max(0, count);
        int limit = Mathf.Max(1, maxVisibleIcons);
        bool compact = currentCount > limit;
        int visible = compact ? 1 : currentCount;
        if (iconPrefab == null) return;
        while (icons.Count < visible)
        {
            var icon = Instantiate(iconPrefab, transform).gameObject;
            icon.transform.SetSiblingIndex(icons.Count);
            icons.Add(icon);
        }
        for (int i = 0; i < icons.Count; i++) icons[i].SetActive(i < visible);
        if (countText == null && compact)
        {
            var label = new GameObject("ResourceCount", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            label.transform.SetParent(transform, false);
            countText = label.GetComponent<TextMeshProUGUI>();
            countText.fontSize = 30;
            countText.alignment = TextAlignmentOptions.MidlineLeft;
            countText.raycastTarget = false;
            label.GetComponent<LayoutElement>().preferredHeight = 40;
        }
        if (countText != null)
        {
            countText.text = compact ? $"× {currentCount}" : string.Empty;
            countText.gameObject.SetActive(compact);
            countText.transform.SetAsLastSibling();
        }
    }
}
