using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using Newtonsoft.Json.Linq;
using System;

public class BuffReceiver : MonoBehaviour
{
    // Events
    public BuffActivated BuffActivated = new BuffActivated();
    public BuffDeactivated BuffDeactivated = new BuffDeactivated();
    public BuffTicked BuffTicked = new BuffTicked();
    public BuffExpired BuffExpired = new BuffExpired();
    public BuffStacksChanged BuffStacksChanged = new BuffStacksChanged();

    private List<Buff> buffs = new List<Buff>();
    public IEnumerable<Buff> Buffs { get { return buffs; } }

    protected GameObject container;

    private void Awake()
    {
        container = new GameObject("Buffs");
        container.transform.parent = transform;
    }

    /// <summary>
    /// Applies the buff to this game object. The buff will be automatically handled depending
    /// on the BuffApplication mode.
    /// </summary>
    /// <param name='buff'>
    /// Buff.
    /// </param>
    public void ApplyBuff(Buff buff, GameObject source)
    {
        switch (buff.Application)
        {
            case BuffApplication.Unique:
                ApplyUnique(buff, source);
                break;
            case BuffApplication.Override:
                ApplyOverride(buff, source);
                break;
            case BuffApplication.StackingIntensity:
                ApplyStackingIntensity(buff, source);
                break;
            case BuffApplication.StackingDuration:
                ApplyStackingDuration(buff, source);
                break;
        }
    }

    /// <summary>
    /// Removes all buffs of the same type
    /// </summary>
    /// <param name='type'>
    /// Type.
    /// </param>
    public void RemoveBuff(string id)
    {
        IEnumerable<Buff> result = GetBuffs(id);
        foreach (Buff buff in result)
        {
            buff.Deactivate();
        }
    }

    public void RemoveAllBuffs(bool deletePersistentBuffs = false)
    {
        foreach (Buff buff in buffs.ToList())
        {
            buff.Deactivate();
        }
    }

    /// <summary>
    /// Gets the buff given the id
    /// </summary>
    /// <param name="id"></param>
    /// <returns>If there are more than one buff of the same type, it returns the buff with higher time remaining</returns>
    public Buff GetBuff(string id)
    {
        return buffs.Where(b => b.Id == id).OrderByDescending(b => b.TimeRemaining).FirstOrDefault();
    }

    public IEnumerable<Buff> GetBuffs(string id)
    {
        return buffs.Where(b => b.Id == id).ToList();
    }

    public int GetStacks(string id)
    {
        return buffs.Where(b => b.Id == id).Sum(b => b.Stacks);
    }

    void ActivateBuff(Buff buff, GameObject source)
    {
        buffs.Add(buff);
        buff.Activate(source, this);
        buff.gameObject.name = buff.Name;
        buff.transform.parent = container.transform;
        buff.Deactivated.AddListener(OnBuffDeactivated);
    }

    void ApplyUnique(Buff buff, GameObject source)
    {
        Assert.IsTrue(buff.Application == BuffApplication.Unique, "Buff application must be unique");
        if (!GetBuff(buff.Id)) ActivateBuff(buff, source);
        else buff.Deactivate();
    }

    void ApplyOverride(Buff buff, GameObject source)
    {
        Assert.IsTrue(buff.Application == BuffApplication.Override, "Buff application must be override");

        // Override buff based on rules if an existing buff with the same id exists
        Buff existing = buffs.Where(b => b.Id == buff.Id).SingleOrDefault();
        if (existing)
        {
            if (buff.ShouldOverride(existing))
            {
                existing.Deactivate();
                ActivateBuff(buff, source);
            }
            else
            {
                buff.Deactivate();
            }
        }
        // Buff doesn't exist yet, apply buff directly
        else
        {
            ActivateBuff(buff, source);
        }
    }

    void ApplyStackingIntensity(Buff buff, GameObject source)
    {
        Assert.IsTrue(buff.Application == BuffApplication.StackingIntensity, "Buff application must be stacking intensity");
        Assert.IsTrue(buff.Stacks > 0, "Buff of StackType.Intensity should have greater than 0 stacks");
        Assert.IsTrue(buff.MaxStacks >= 0, "Max stacks should be equal or greater than 0");

        // Apply buff if there is no stack limit
        if (buff.MaxStacks == 0)
        {
            ActivateBuff(buff, source);
            return;
        }

        // Apply buff if there's still room for the new stacks
        int projectedStacks = GetStacks(buff.Id) + buff.Stacks;
        if (projectedStacks <= buff.MaxStacks)
        {
            ActivateBuff(buff, source);
            return;
        }

        // Try to override. Buffs with the shortest duration left are evaluated first.
        List<Buff> existingBuffs = buffs.Where(b => b.Id == buff.Id).OrderBy(b => b.TimeRemaining).ToList();
        foreach (Buff b in existingBuffs)
        {
            // Check if we can override the buff
            if (!buff.ShouldOverride(b)) continue;

            // Determine the number of stacks to be removed. Deactivate the buff it requires equal or more than the required stack
            int stacksToRemove = projectedStacks - buff.MaxStacks;
            if (stacksToRemove >= b.Stacks)
            {
                projectedStacks -= b.Stacks;
                b.Deactivate();
            }
            else
            {
                projectedStacks -= stacksToRemove;
                b.Stacks -= stacksToRemove;
            }

            // We already have enough room for the buff. Stop overriding and apply the new buff
            if (projectedStacks <= buff.MaxStacks)
            {
                ActivateBuff(buff, source);
                return;
            }
        }

        // Scrape off some excess stacks from the new buff since it wasn't able to override enough existing buff to make room for the new buff.
        int excessStacks = projectedStacks - buff.MaxStacks;
        if (buff.Stacks > excessStacks)
        {
            buff.Stacks -= excessStacks;
            ActivateBuff(buff, source);
            return;
        }

        // We don't have room for a single stack of this buff. Buff wasn't able to override any existing buff. Ignore the new buff.
        buff.Deactivate();
    }

    void ApplyStackingDuration(Buff buff, GameObject source)
    {
        Assert.IsTrue(buff.Application == BuffApplication.StackingDuration, "Buff application must be stacking duration");
        Assert.IsTrue(buff.Duration > 0, "Buff stacking in duration must have a duration set");

        // Carry over previous buff duration and deactivate it.
        Buff existing = buffs.Where(b => b.Id == buff.Id).SingleOrDefault();
        if (existing)
        {
            buff.Duration += existing.Duration;
            existing.Deactivate();
        }

        // Apply buff
        ActivateBuff(buff, source);
    }

    void OnBuffDeactivated(Buff buff)
    {
        buff.Deactivated.RemoveListener(OnBuffDeactivated);
        buffs.Remove(buff);
    }
}
