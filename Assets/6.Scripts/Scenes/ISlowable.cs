using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public interface ISlowable 
{
    void ApplySlow(float duration, float slowFactor);
    void ResetSpeed();

    UniTask ResetSpeedAfterDelay(float duration, CancellationToken token);
}
