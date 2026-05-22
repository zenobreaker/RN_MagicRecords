using System;
using System.Collections.Generic;
using UnityEngine;


[ModuleCategory("Common/SpawnWeaponVFX")]
[Serializable]
public class Module_SpawnWeaponVFX : SkillModule
{
    [Header("Muzzle Flash Settings")]
    public GameObject muzzleFlashPrefab;

    // 💡 1. 모든 총구를 다 쓸 것인가? 아니면 특정 총구만 쓸 것인가?
    [Tooltip("체크하면 무기에 달린 모든 총구에서 플래시가 터집니다.")]
    public bool useAllMuzzles = true;

    // 💡 2. 특정 총구만 쓴다면 몇 번 총구를 쓸 것인가? (배열)
    [Tooltip("useAllMuzzles가 꺼져있을 때 작동합니다. (예: 0 넣으면 첫 번째 총구, 0과 1 넣으면 두 개)")]
    public int[] specificMuzzleIndices;

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (owner.TryGetComponent<WeaponComponent>(out var weaponComp))
        {
            Weapon currentWeapon = weaponComp.GetCurrentWeapon();

            // 현재 들고 있는 무기가 Gun일 때만 작동
            if (currentWeapon is Gun gun)
            {
                List<Transform> muzzles = gun.GetMuzzleTransforms();
                if (muzzles == null || muzzles.Count == 0) return;

                // 💡 3. 조건에 따라 불을 뿜을 총구를 걸러냅니다.
                if (useAllMuzzles)
                {
                    // [전체 다 쏘기]
                    foreach (var muzzle in muzzles)
                    {
                        SpawnFlash(muzzle);
                    }
                }
                else
                {
                    // [특정 번호만 쏘기]
                    if (specificMuzzleIndices != null)
                    {
                        foreach (int idx in specificMuzzleIndices)
                        {
                            // 인덱스가 배열 범위를 벗어나지 않도록 안전장치(방어 코드)
                            if (idx >= 0 && idx < muzzles.Count)
                            {
                                SpawnFlash(muzzles[idx]);
                            }
                        }
                    }
                }
            }
        }
    }

    private void SpawnFlash(Transform muzzleTransform)
    {
        if (muzzleFlashPrefab != null)
        {
            // ObjectPooler가 있다면 ObjectPooler.SpawnFromPool(...) 로 교체하세요!
            GameObject.Instantiate(muzzleFlashPrefab, muzzleTransform.position, muzzleTransform.rotation);
        }
    }
}

