using System;
using UnityEngine;

[ModuleCategory("Common/StartWeaponVFX")]
[Serializable]
public class Module_StartWeaponVFX : Module_SpawnWeaponVFX
{
    [Tooltip("StopWeaponVFX에 같은 ID를 지정하면 이 이펙트들을 제거합니다.")]
    public string effectId = "WeaponVFX";

    [Tooltip("생성된 이펙트가 무기의 생성 지점을 따라갑니다.")]
    public bool followEffectSpawnPoint = true;

    protected override string EffectId => effectId;
    protected override bool FollowEffectSpawnPoint => followEffectSpawnPoint;
    protected override bool ReplaceExistingEffects => true;
}
