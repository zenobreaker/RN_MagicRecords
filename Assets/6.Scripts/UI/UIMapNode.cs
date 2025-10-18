using System;
using UnityEngine;
using UnityEngine.UI;

public interface UINode
{
    void Init(MapNode mapNode);
}

public class UIMapNode 
    : MonoBehaviour, UINode
{
    [SerializeField] private MapNode mapNode;
    public MapNode Node { get { return mapNode; } }
 

    private void Start()
    {
        
    }

    public virtual void Init(MapNode mapNode)
    {
        this.mapNode = mapNode;
        Refresh();
    }

    public virtual void Refresh()
    {

    }
}
