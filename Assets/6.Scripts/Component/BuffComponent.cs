using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffComponent : MonoBehaviour
{
    [SerializeField]
    private float tickInterval = 0.1f; 

    private Dictionary<string, BaseBuff> activeBuffs = new Dictionary<string, BaseBuff>();
    private Character owner;

    private void Awake()
    {
        owner = GetComponent<Character>();
        Debug.Assert(owner != null);
    }

    private void Update()
    {
        if (activeBuffs.Count == 0) return;

        foreach(BaseBuff buff in activeBuffs.Values)
        {
            if(buff.NeedTick)
                buff.OnUpdate(Time.deltaTime);
        }
    }

    public void ApplyBuff(BaseBuff newBuff)
    {
        if (newBuff == null) return; 

        if(activeBuffs.TryGetValue(newBuff.BuffID,out var existingBuff))
        {
            switch(newBuff.StackPolicy)
            {
                case BuffStackPolicy.RefreshOnly:
                    existingBuff.ResetDuration();
                    break;
                case BuffStackPolicy.Stackable:
                    existingBuff.AddStack(); 
                    break;
                case BuffStackPolicy.IgnoreIfExsist:
                    return;
            }
        }
        else
        {
            activeBuffs.Add(newBuff.BuffID, newBuff);
            newBuff.TickInterval = tickInterval;
            newBuff.OnApply(owner);
            newBuff.OnExpired += RemoveBuff;
        }
    }

    public void RemoveBuff(BaseBuff buff)
    {
        if (buff == null) return;
        RemoveBuff(buff.BuffID);
    }

    public void RemoveBuff(string buffID)
    {
        if(string.IsNullOrEmpty(buffID)) return;

        if (activeBuffs.TryGetValue(buffID, out BaseBuff baseBuff))
        {
            baseBuff.OnRemove();
            activeBuffs.Remove(buffID);
        }
    }
}
