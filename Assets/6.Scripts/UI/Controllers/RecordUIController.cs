using System.Collections.Generic;
using UnityEngine;

public class RecordUIController : MonoBehaviour
{
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private RecordCard recordCardPrefab;

    private List<RecordCard> cardPool = new();

    private void Start()
    {

        ManagerWaiter.RegisterManagerEvent<AppManager>(this,
            onRegister: manager =>
            {
                manager.OnShowRecordUI += ShowUI;
            },
            onUnregister: manager =>
            {
                manager.OnShowRecordUI -= ShowUI;
            });
        visualRoot.SetActive(false);
    }

    private void ShowUI(List<RecordData> options)
    {
        visualRoot.SetActive(true);

        // 1. 모든 카드를 일단 비활성화 
        foreach (var card in cardPool)
            card.gameObject.SetActive(false);

        //2. 전달받은 데이터 수만큼 카드 배치 
        for (int i = 0; i < options.Count; i++)
        {
            RecordCard card = GetOrCreateCard(i);
            card.gameObject.SetActive(true);

            // 데이터 설정 및 클릭 이벤트 연결
            RecordData data = options[i];
            card.Setup(data, () => OnCardClicked(data));
        }

    }

    private RecordCard GetOrCreateCard(int index)
    {
        // 풀에 있으면 재사용, 없으면 새로 생성 
        if (index < cardPool.Count)
        {
            return cardPool[index];
        }
        else
        {
            RecordCard newCard = Instantiate(recordCardPrefab, visualRoot.transform);
            cardPool.Add(newCard);
            return newCard;
        }
    }

    private void OnCardClicked(RecordData selectedData)
    {
        visualRoot.SetActive(false);
        AppManager.Instance?.OnRecordSelected(selectedData);
    }
}
