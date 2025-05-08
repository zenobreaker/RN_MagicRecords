using System.Collections;
using UnityEngine;

public class Enemy
    : Character
    , IDamagable
{

    [Header("Material Settings")]
    [SerializeField] private string[] surfaceNames;
    [SerializeField] private Color damageColor;
    [SerializeField] private float changeColorTime = 0.15f;

    private Color[] originColors;
    private Material[] skinMaterials;

    private DamageHandleComponent damageHandle;
    protected override void Awake()
    {
        base.Awake();

        int index = 0;
        skinMaterials = new Material[surfaceNames.Length];
        originColors = new Color[surfaceNames.Length];
        foreach (string name in surfaceNames)
        {
            Transform surface = transform.FindChildByName(name);
            if (surface == null)
                continue;

            if (surface.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer skin))
            {
                skinMaterials[index] = skin.material;
                originColors[index] = skin.material.color;
            }

            index++;
        }

        damageHandle = GetComponent<DamageHandleComponent>();
    }

    protected override void Start()
    {
        base.Start();
    }


    public void OnDamage(GameObject attacker,
        Weapon causer, Vector3 hitPoint, DamageEvent damageEvent)
    {
        if (healthPoint != null && healthPoint.Dead)
            return;

        if (damageHandle != null)
        {
            damageHandle.OnDamage(damageEvent);
        }

        StartCoroutine(Change_Color(changeColorTime));

        if (healthPoint.Dead == false)
        {
            state?.SetDamagedMode();

            return;
        }

        // Dead..
        state?.SetDeadMode();
        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;
        rigidbody.isKinematic = true;

        animator.SetTrigger("Dead");
        MovableStopper.Instance.Delete(this);
        MovableSlower.Instance.Delete(this);
        Destroy(gameObject, 5);
    }


    private IEnumerator Change_Color(float time)
    {
        foreach (Material mat in skinMaterials)
        {
            mat.color = damageColor;
        }

        yield return new WaitForSeconds(time);

        int index = 0;
        foreach (Material mat in skinMaterials)
        {
            mat.color = originColors[index];
            index++;
        }

        //TODO: 임시로 여기에 호출한다.
        End_Damaged();
    }

    protected override void End_Damaged()
    {
        base.End_Damaged();

        state?.SetIdleMode();
    }
}
