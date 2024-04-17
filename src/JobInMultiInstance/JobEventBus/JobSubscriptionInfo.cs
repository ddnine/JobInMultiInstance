namespace EventBus;

/// <summary>
/// 订阅信息
/// </summary>
public class JobSubscriptionInfo
{
    /// <summary>
    /// eventName-EventHandlerTypes
    /// </summary>
    public Dictionary<string, List<Type>> EventHandlerTypes { get; private set;} = new();

    /// <summary>
    /// EventName-EventType
    /// </summary>
    public Dictionary<string,Type> EventTypes { get; private set;} = new();
}