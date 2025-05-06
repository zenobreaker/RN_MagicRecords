using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class SplitView : TwoPaneSplitView
{
    [System.Obsolete]
    public new class UxmlFactory : UxmlFactory<SplitView, TwoPaneSplitView.UxmlTraits> { }
}
