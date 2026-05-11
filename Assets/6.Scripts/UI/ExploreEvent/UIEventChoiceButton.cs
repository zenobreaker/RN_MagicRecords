using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIEventChoiceButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI choiceText;

    // (선택사항) 비활성화 시 자물쇠 아이콘이나 배경색을 바꾸고 싶다면 추가!
    // [SerializeField] private Image background; 
    // [SerializeField] private GameObject lockIcon; 

    private EventChoice choiceData;
    private Action<EventChoice> onClickCallback;

    public void Setup(EventChoice data, Action<EventChoice> callback)
    {
        choiceData = data;
        onClickCallback = callback;

        // 1. 텍스트 세팅
        choiceText.text = LocalizationManager.Instance.GetText(data.TextKey);

        // 2. 조건 확인: 이 선택지를 고를 자격(비용)이 충분한가?
        bool isAvailable = CheckCanAfford(data.CostType, data.CostValue);

        // 3. 버튼 활성화 / 비활성화 
        button.interactable = isAvailable;

        // 4. 시각적 피드백 (비활성화 시 텍스트를 어둡게)
        if (isAvailable)
        {
            choiceText.color = Color.white; // 기본 색상
            // if (lockIcon != null) lockIcon.SetActive(false);
        }
        else
        {
            choiceText.color = new Color(0.4f, 0.4f, 0.4f, 1f); // 어두운 회색

            // 💡 (꿀팁) 왜 안 되는지 이유를 덧붙여주면 좋습니다. 
            // choiceText.text += " <color=red>(골드 부족)</color>";
            // if (lockIcon != null) lockIcon.SetActive(true);
        }

        // 5. 클릭 이벤트 연결 (활성화 상태일 때만)
        button.onClick.RemoveAllListeners();
        if (isAvailable)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    // 💡 실제 게임의 재화 매니저들과 연결하는 곳입니다!
    private bool CheckCanAfford(EventCostType costType, int costValue)
    {
        switch (costType)
        {
            case EventCostType.GOLD:
                // 예: 현재 골드가 요구량보다 크거나 같은가?
                return CurrencyManager.Instance.GetCurrency(CurrencyType.GOLD) >= costValue;

            case EventCostType.HP_PERCENT:
                // 예: 현재 체력이 비용(%)으로 깎여도 죽지 않고 살아남을 수 있는가?
                 //int hpCost = PlayerManager.Instance.MaxHP * costValue / 100;
                 //return PlayerManager.Instance.CurrentHP > hpCost;
                return true; 

            case EventCostType.NONE:
            default:
                return true; // 비용이 없으면 무조건 통과
        }
    }

    private void OnClick()
    {
        onClickCallback?.Invoke(choiceData);
    }
}