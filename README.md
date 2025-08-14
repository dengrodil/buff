# Buff System

A `Buff` or Status Effect system that can be used in any game. 

I wrote this way back in 2017. Since it's been a while, I think I can do better than what I wrote before. I'll be updating this sometime.

`Actual code can be found in Assets/Sylpheed/Buff`

- Works on any type of buff or status effect you could think of. I have used this in multiple released titles and prototypes.
- Extensive buff stacking system inspired by Guild Wars 2.
- Performant. This is built with performance in mind. Buffs are pooled and reused if applicable.
- Taggable. You can attach tags to `Buffs`.
- Dispatches events so your game and UI can react to buff changes.
- Can view a summary of all buffs via the `BuffReceiver` component.
- JSON serializable. Buffs can be persisted. We used this to apply monetization buffs in our game (eg. XP bonus for 1 month).


# Components
## Buff
- An actual instance of a `Buff` or a status effect.
- This sits in the scene as a MonoBehaviour (planning to convert this to a ScriptableObject).
- Inherit from the base class `Buff` to implement your custom buff / status effects.

## BuffReceiver
- Marks the GameObject as something that can receive `Buffs`. Attach this as a component to the GameObject.

# Todo
- Convert into a UPM-friendly plugin
- Convert Buff to a ScriptableObject
- Remove dependency on external libraries
- More improvements...
