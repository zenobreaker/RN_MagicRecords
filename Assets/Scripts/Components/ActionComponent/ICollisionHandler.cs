using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICollisionHandler 
{
    public void Begin_Collision(AnimationEvent e);

    public void End_Collision();
}
