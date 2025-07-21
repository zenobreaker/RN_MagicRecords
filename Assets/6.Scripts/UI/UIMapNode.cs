using System;
using UnityEngine;

public class UIMapNode 
    : MonoBehaviour
{
    [SerializeField] private MapNode mapNode;
    [SerializeField] private Stage stage;

    public event Action<Stage> OnClicked;

    private void Start()
    {
        
    }

    public void Init(MapNode mapNode)
    {
        this.mapNode = mapNode;
    }

    public void OnClick()
    {
        if(mapNode != null)
            OnClicked?.Invoke(stage); 
    }
}
