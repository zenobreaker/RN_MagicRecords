using System;
using UnityEngine;
using UnityEngine.UI;

public class UIMapNode 
    : MonoBehaviour
{
    [SerializeField] private MapNode mapNode;
    public MapNode Node { get { return mapNode; } }
    [SerializeField] private StageInfo stageInfo;

    public event Action<StageInfo> OnClicked;

    private void Start()
    {
        
    }

    public void Init(MapNode mapNode)
    {
        this.mapNode = mapNode;
        SetStage(mapNode.stageID);

        Refresh();
    }

    public void SetStage(int id)
    {
        stageInfo = AppManager.Instance.GetStageInfo(id);
    }

    public void OnClick()
    {
        OnClicked?.Invoke(stageInfo); 
    }

    public void Refresh()
    {
        // ���⼭ �� �� �ִ� ���� �� �� �ֵ��� ó�� 
        if (gameObject.TryGetComponent<Button>(out Button button))
        {
            // �� �� ���� ���̸� ��Ȱ��ȭ UI  
            button.enabled = AppManager.Instance.EnableNode(mapNode.id);
        }

        if(stageInfo?.bIsCleared == true)
        {
            //TODO : Ŭ����� ���ó��
        }
    }
}
