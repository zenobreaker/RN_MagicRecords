using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Character))]
public class SkillVFXComponent : MonoBehaviour
{
    private struct EffectEntry
    {
        public ActiveSkill Skill;
        public string Id;
        public GameObject Instance;
    }

    private readonly List<EffectEntry> effects = new List<EffectEntry>();
    private Character owner;

    private void Awake()
    {
        owner = GetComponent<Character>();
    }

    private void OnEnable()
    {
        owner.OnEndDoAction += RemoveAllEffects;
    }

    private void OnDisable()
    {
        if (owner != null)
            owner.OnEndDoAction -= RemoveAllEffects;
        RemoveAllEffects();
    }

    public void RegisterEffect(ActiveSkill skill, string effectId, GameObject instance)
    {
        if (instance == null) return;
        if (!isActiveAndEnabled)
        {
            DestroyEffect(instance);
            return;
        }

        effects.RemoveAll(entry => entry.Instance == null);
        if (effects.Exists(entry => entry.Instance == instance)) return;
        effects.Add(new EffectEntry
        {
            Skill = skill,
            Id = effectId ?? string.Empty,
            Instance = instance
        });
    }

    public void RemoveEffects(ActiveSkill skill, string effectId)
    {
        effectId = effectId ?? string.Empty;
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var entry = effects[i];
            if (entry.Instance == null || (ReferenceEquals(entry.Skill, skill) && entry.Id == effectId))
            {
                effects.RemoveAt(i);
                DestroyEffect(entry.Instance);
            }
        }
    }

    public void RemoveSkillEffects(ActiveSkill skill)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(effects[i].Skill, skill)) continue;
            var instance = effects[i].Instance;
            effects.RemoveAt(i);
            DestroyEffect(instance);
        }
    }

    public void RemoveAllEffects()
    {
        // 먼저 비워서 이펙트의 OnDisable에서 다시 정리해도 안전하게 처리합니다.
        var instances = effects.ToArray();
        effects.Clear();
        foreach (var entry in instances)
            DestroyEffect(entry.Instance);
    }

    private static void DestroyEffect(GameObject instance)
    {
        if (instance == null) return;
        instance.SetActive(false);
        Destroy(instance);
    }
}
