using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IStoppable 
{
    void Regist_MovableStopper(); // MovableStopper¿¡ µî·Ï 
    IEnumerator Start_FrameDelay(int frame);

}
