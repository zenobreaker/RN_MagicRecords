using UnityEngine;

public class WarningSign_Circle : WarningSign
{
    public override void Setup(IWarningData data, float duration)
    {
        base.Setup(data, duration);

        if (data == null)
            return;

        float scale = (data.Radius * 2f) / 10f;

        SetData(scale, duration);

        subPlane.localPosition = mainPlane.localPosition;
    }
}