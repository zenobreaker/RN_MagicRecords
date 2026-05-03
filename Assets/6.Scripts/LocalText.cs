using UnityEngine;
using TMPro; // TextMeshPro 사용 시

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizeText : MonoBehaviour
{
    [Tooltip("번역할 데이터의 Key값을 넣으세요 (예: name_biome_forest)")]
    public string textKey;

    private TextMeshProUGUI uiText;

    private void Start()
    {
        uiText = GetComponent<TextMeshProUGUI>();
        ApplyLocalization();
    }

    // 키값에 맞춰 즉시 번역 텍스트를 꽂아줍니다.
    public void ApplyLocalization()
    {
        if (string.IsNullOrEmpty(textKey) || LocalizationManager.Instance == null) return;

        uiText.text = LocalizationManager.Instance.GetText(textKey);
    }
}