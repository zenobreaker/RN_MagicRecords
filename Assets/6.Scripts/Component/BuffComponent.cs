using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffComponent : MonoBehaviour
{
    [SerializeField]
    private float tickInterval = 0.1f;

    private Character owner;

    private Dictionary<string, BaseBuff> activeBuffs = new Dictionary<string, BaseBuff>();
    private List<BaseBuff> expiredBuffs = new List<BaseBuff>();

    private void Awake()
    {
        owner = GetComponent<Character>();
        Debug.Assert(owner != null);
    }

    private void Update()
    {
        if (activeBuffs.Count == 0) return;

        expiredBuffs.Clear();

        foreach (BaseBuff buff in activeBuffs.Values)
        {
            if (buff.NeedTick && buff.IsExpired == false)
                buff.OnUpdate(Time.deltaTime);

            if (buff.IsExpired)
                expiredBuffs.Add(buff);
        }

        foreach(BaseBuff buff in expiredBuffs)
            RemoveBuff(buff);
    }

    public void ApplyBuff(BaseBuff newBuff)
    {
        if (newBuff == null) return;

        if (activeBuffs.TryGetValue(newBuff.BuffID, out var existingBuff))
        {
            switch (newBuff.StackPolicy)
            {
                case BuffStackPolicy.REFRESH_ONLY:
                    existingBuff.ResetDuration();
                    break;
                case BuffStackPolicy.STACKABLE:
                    existingBuff.AddStack();
                    break;
                case BuffStackPolicy.IGNOREIFEXSIST:
                    return;
            }
        }
        else
        {
            activeBuffs.Add(newBuff.BuffID, newBuff);
            newBuff.TickInterval = tickInterval;
            newBuff.OnApply(owner);
        }
    }

    public void RemoveBuff(BaseBuff buff)
    {
        if (buff == null) return;
        RemoveBuff(buff.BuffID);
    }

    public void RemoveBuff(string buffID)
    {
        if (string.IsNullOrEmpty(buffID)) return;

        if (activeBuffs.TryGetValue(buffID, out BaseBuff baseBuff))
        {
            baseBuff.OnRemove();
            activeBuffs.Remove(buffID);
        }
    }
}
