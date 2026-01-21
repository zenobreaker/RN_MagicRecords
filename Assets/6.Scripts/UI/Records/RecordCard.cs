using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button selectButton;

    public void Setup(RecordData data, System.Action onClickAction)
    {
        // 1. UI 텍스트 업데이트
        nameText.text = data.recordName;
        descText.text = data.description;
        // iconImage.sprite = Resources.Load<Sprite>(data.iconPath); // 예시

        // 2. 버튼 리스너 초기화 및 재할당
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onClickAction?.Invoke());

        // 3. 등급(Rarity)에 따른 카드 테두리 색상 변경 등 연출 추가 가능
        // SetRarityColor(data.rarity);
    }
}
