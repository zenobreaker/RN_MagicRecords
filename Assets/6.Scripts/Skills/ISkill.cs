using System;
using UnityEngine;

public interface ISkill
{
    bool IsOnCooldown { get; }
}