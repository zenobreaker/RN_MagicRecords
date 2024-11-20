using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISlowable 
{
    void ApplySlow(float duration, float slowFactor);
    void ResetSpeed();

    IEnumerator ResetSpeedAfterDelay(float duration);
}
