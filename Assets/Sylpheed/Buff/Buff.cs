using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using MEC;

public class Buff : MonoBehaviour, IJsonSerializable
{
    [Header("Identifier")]
    /// <summary>
	/// The identifier of this type of buff. Buff of the same identifier will be handled based on BuffApplication.
	/// </summary>
    public string Id;
    /// <summary>
	/// Display name for the buff.
	/// </summary>
    public string Name;
    /// <summary>
    /// Icon used by this buff type
    /// </summary>
    public Sprite Icon;
    /// <summary>
    /// Description template used by this buff type
    /// </summary>
    [TextArea] public string DescriptionTemplate;

    [Header("Application")]
    /// <summary>
    /// Mode of how the buff is applied.
    /// </summary>
    public BuffApplication Application;
    /// <summary>
    /// Maximum number of stacks of the same buff a receiver can have. Only relevant if Application is StackingIntensity.
    /// Set to 0 for no stack limit
    /// </summary>
    public int MaxStacks;
    /// <summary>
    /// Determines how fast this buff ticks every interval (seconds). This is useful for debuffs that damages a unit over time.
    /// Set to 0 if this buff doesn't need to take effect every tick
    /// </summary>
    public float TickInterval;
    /// <summary>
    /// Determines whether to use scaled delta time or real time
    /// </summary>
    public bool UseUnscaledDeltaTime;

    [Header("Instance Configuration")]
    /// <summary>
    /// This should default to 1 regardless of Application. 
    /// This will make it easier for derived classes to handle stacks separately.
    /// </summary>
    public int Stacks = 1;
    public float Duration;

    /// <summary>
    /// Set to True if the Buff will be displayed in the UI. 
    /// </summary>
    public bool ShouldDisplayInUI;

    [Header("Tags")]
    public List<string> Tags = new List<string>();

    /// <summary>
    /// The unit who applied the buff. Set this if needed.
    /// </summary>
    public GameObject Source { get; private set; }
    /// <summary>
    /// The unit whom this buff is applied.
    /// </summary>
    public BuffReceiver Target { get; private set; }
    public bool Active { get; private set; }
    public float TimeElapsed { get; set; }
    public float TimeRemaining { get { return Duration - TimeElapsed; } }
    public float DurationNormalized { get { return Duration > 0 ? TimeElapsed / Duration : 1.0f; } }
    /// <summary>
    /// Description used by this buff type
    /// </summary>
    public virtual string Description { get { return DescriptionTemplate; } }

    // Events
    public BuffActivated Activated = new BuffActivated();
    public BuffDeactivated Deactivated = new BuffDeactivated();
    public BuffTicked Ticked = new BuffTicked();
    public BuffExpired Expired = new BuffExpired();
    public BuffStacksChanged StacksChanged = new BuffStacksChanged();

    #region Overridables
    protected virtual void OnActivate() { }
    protected virtual void OnDeactivate() { }
    protected virtual void OnBuffUpdate(float dt) { }
    protected virtual void OnTick() { }
    /// <summary>
    /// Determines which buff gets prioritized when stacked
    /// By default, old buff with lesser time remaining than the new buff will be overriden
    /// </summary>
    /// <param name="other">Buff to be stacked (new)</param>
    /// <returns>True if buff should be overriden</returns>
    protected virtual bool OnOverriding(Buff other) { return TimeRemaining > other.TimeRemaining; }
    protected virtual void OnStacksChanged(int prevStacks) { }

    #endregion

    private CoroutineHandle UpdateTick;

    public void Activate(GameObject source, BuffReceiver target)
    {
        Assert.IsFalse(Active, "Buff is already activated");
        Assert.IsNotNull(target);
        Assert.IsTrue(Stacks > 0, "Stacks must be at least 1");

        Active = true;
        Source = source;
        Target = target;

        OnActivate();
        Activated.Invoke(this);
        Target.BuffActivated.Invoke(this);

        UpdateTick = Timing.RunCoroutine(UpdateTask().CancelWith(target.gameObject));

        if (TickInterval > 0)
        {
            if (UseUnscaledDeltaTime)
                Timing.RunCoroutine(TickTask().CancelWith(target.gameObject), Segment.RealtimeUpdate);
            else
                Timing.RunCoroutine(TickTask().CancelWith(target.gameObject));
        }
    }

    IEnumerator<float> UpdateTask()
    {
        int previousStacks = Stacks;

        while (Active)
        {
            float dt = UseUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime;
            OnBuffUpdate(dt);

            // Buff timer
            if (Duration > 0)
            {
                if (TimeElapsed < Duration)
                {
                    TimeElapsed += dt;
                }
                else
                {
                    Expired.Invoke(this);
                    Target.BuffExpired.Invoke(this);
                    Deactivate();
                }
            }

            // Stack monitoring
            if (Application == BuffApplication.StackingIntensity && previousStacks != Stacks)
            {
                OnStacksChanged(previousStacks);
                StacksChanged.Invoke(this, previousStacks);
                Target.BuffStacksChanged.Invoke(this, previousStacks);
            }
            previousStacks = Stacks;

            yield return Timing.WaitForOneFrame;
        }
    }

    IEnumerator<float> TickTask()
    {
        while (Active)
        {
            yield return Timing.WaitForSeconds(TickInterval);
            Tick();
        }
    }

    public void Deactivate()
    {
        bool prevActivated = Active;
        Active = false;

        if (prevActivated)
        {
            OnDeactivate();
            Deactivated.Invoke(this);
            Target.BuffDeactivated.Invoke(this);
            TimeElapsed = 0;
        }
        Timing.KillCoroutines(UpdateTick);
        this.PoolOrDestroy();
    }

    public bool ShouldOverride(Buff other)
    {
        Assert.IsTrue(other.Id == Id, "ShouldOverride should only be called on buff of the same identifier");
        Assert.IsTrue(other != this, "Cannot be the same buff");

        return OnOverriding(other);
    }

    protected void Tick()
    {
        OnTick();
        Ticked.Invoke(this);
        Target.BuffTicked.Invoke(this);
    }

    public void AddTags(params string[] tags)
    {
        foreach (string tag in tags)
        {
            if (Tags.Contains(tag)) return;
            Tags.Add(tag);
        }
    }

    public bool HasTags(params string[] tags)
    {
        return Tags.Intersect(tags).Count() == tags.Count();
    }

    public override string ToString()
    {
        return Name + "[" + Id + "]: Application = " + Application + " Max Stacks = " + MaxStacks + " Tick Interval = " + TickInterval + "s";
    }

    public JToken Serialize()
    {
        JObject json = new JObject();
        json["id"] = Id;
        json["stacks"] = Stacks;
        json["duration"] = Duration;
        json["time-elapsed"] = TimeElapsed;

        return json;
    }

    public void Deserialize(JToken json)
    {
        Assert.IsTrue((string)json["id"] == Id);
        Stacks = (int)json["stacks"];
        TimeElapsed = (float)json["time-elapsed"];

        // Handle duration
        if (json["duration"] != null) Duration = (float)json["duration"];
    }
}

public enum BuffApplication {
    Unique,             // Applies the buff if it doesn't exist yet. Unique buffs cannot be overridden no matter what.
    Override,           // Replaces the old buff of the same type based on buff overriding rules (default = buff with higher time remaining will override the other)
    StackingIntensity,  // Keeps a stack count which increases the intensity depending on the effect of the buff.
    StackingDuration    // Stacks the duration of an existing buff
}

public enum BuffEffectDirection {
    Increasing,
    Decreasing
}

#region Events
public class BuffActivated : UnityEvent<Buff> {}
public class BuffDeactivated : UnityEvent<Buff> {}
public class BuffExpired : UnityEvent<Buff> {}
public class BuffTicked : UnityEvent<Buff> {}
/// <summary>
/// Buff stacks changed.
/// 2nd param = previous stacks
/// </summary>
public class BuffStacksChanged : UnityEvent<Buff, int> {}
#endregion