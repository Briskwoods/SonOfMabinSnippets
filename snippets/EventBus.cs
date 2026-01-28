// @title: Event Bus
// @description: Combined simple and complex event bus for Event Driven Architecture
// @category: systems, patterns
// @tags: Event Driven Architecture, Observer Pattern

public enum EventType
{
    LoadCharacters,
    PlayerDied,
    PlayerLevelUp,
    ItemPickedUp,
    GamePaused,
    GameResumed,
    AbilitySelected,
    TargetSelected,
    UseStoredAbility,
    ResetCombatMenu,
    LoadAbilitiesOntoMenu,
    EndCurrentTurn,
    OnRoundStart,
    OnRoundEnd,
    OnMoveForwardEnd
}

public static class EventBus
{
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////// SIMPLE EVENT BUS //////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    

    // Action Dictionary - where they're stored
    private static Dictionary<EventType, Action> simpleActions = new();

    // This is what triggers the event (aka Raise)
    public static void Raise(EventType eventType)
    {
        if (simpleActions.TryGetValue(eventType, out Action existingAction))
        {
            existingAction?.Invoke();
        }
    }

    // Used to subscribe to events globally
    public static void Subscribe(EventType eventType, Action action)
    {
        switch (simpleActions.ContainsKey(eventType))
        {
            case true:
                simpleActions[eventType] += action;
                break;
            case false:
                simpleActions[eventType] = action;
                break;
        }
    }

    // Used to unsubscribe to events globally
    public static void Unsubscribe(EventType eventType, Action action)
    {
        if (simpleActions.ContainsKey(eventType))
        {
            simpleActions[eventType] -= action;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////// ADVANCED EVENT BUS ////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    // Action Dictionary - where they're stored
    private static Dictionary<Type, Delegate> advancedActions = new();

    // This is what triggers the event (aka Raise)
    public static void Raise<T>(T eventData)
    {
        Type type = typeof(T);

        if (advancedActions.TryGetValue(type, out Delegate existingAction))
        {
            (existingAction as Action<T>)?.Invoke(eventData);
        }
    }

    // Used to subscribe to events globally
    public static void Subscribe<T>(Action<T> action)
    {

        Type type = typeof(T);

        switch (advancedActions.ContainsKey(type))
        {
            case true:
                advancedActions[type] = Delegate.Combine(advancedActions[type], action);
                break;
            case false:
                advancedActions[type] = action;
                break;
        }

    }

    // Used to unsubscribe to events globally
    public static void Unsubscribe<T>(Action<T> action)
    {
        Type type = typeof(T);

        if (advancedActions.ContainsKey(type))
        {
            advancedActions[type] = Delegate.Remove(advancedActions[type], action);
        }
    }
}