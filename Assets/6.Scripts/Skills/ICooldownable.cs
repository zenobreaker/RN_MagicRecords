using System;
using UnityEngine;

public interface ICooldownable
{
    bool IsOnCooldown { get; }
}