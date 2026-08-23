using UnityEngine;

public class WarningSign_Circle : WarningSign
{
    public override void Setup(IWarningData data, float duration)
    {
        base.Setup(data, duration);

        subPlane.localPosition = mainPlane.localPosition;
    }
}
