using System;
using UnityEngine;


[ModuleCategory("Common/Camera Shake")]
[Serializable]
public class Module_CameraShake : SkillModule
{
    [Header("Camera Shake")]
    public Vector3 impulseDirection;

    [Tooltip("Cinemachine NoiseSettings asset")]
    public Unity.Cinemachine.NoiseSettings settings;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (MovableCameraShaker.Instance != null)
            MovableCameraShaker.Instance.Play_Impulse(settings);
    }
}