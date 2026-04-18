using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public interface IStoppable 
{
    void Regist_MovableStopper(); // MovableStopper¿¡ µî·Ï 
    UniTask Start_FrameDelay(int frame, CancellationToken token);
}
