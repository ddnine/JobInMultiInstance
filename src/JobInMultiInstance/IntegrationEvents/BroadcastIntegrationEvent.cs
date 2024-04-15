using JobEventBus.Abstractions;
using JobEventBus.Events;
using JobInMultiInstance.Services;
using Microsoft.Extensions.Logging;

namespace JobInMultiInstance.IntegrationEvents;

/// <summary>
/// 当该实例需要执行一个正在运行且不是该实例的Job时，发送该事件,通知所有实例执行该Job
/// 所有实例都会收到该事件，但只有一个实例会执行该Job
/// </summary>
public record BroadcastIntegrationEvent : JobIntegrationEvent 
{
    public string Key { get; set; } = null!;    
    
    public string MethodName { get; set; } = null!;
    
    public string Message { get; set; } = "";
    
    public string HandlerName { get; set; }="";
}

public class BroadcastIntegrationEventHandler : IJobIntegrationEventHandler<BroadcastIntegrationEvent>
{
    private readonly ILogger<BroadcastIntegrationEventHandler> _logger;
    private readonly IMultiInstanceJobService _multiInstanceJobService;

    public BroadcastIntegrationEventHandler(
        ILogger<BroadcastIntegrationEventHandler> logger, 
        IMultiInstanceJobService multiInstanceJobService
        )
    {
        _logger = logger;
        _multiInstanceJobService = multiInstanceJobService;
    }

    public Task Handle(BroadcastIntegrationEvent @event)
    {
        // 执行Job,如果该Job的不是该实例的,则不发送广播事件
        _multiInstanceJobService.ExecuteJob(@event.Key, @event.Message, @event.MethodName, @event.HandlerName, false);

        return Task.CompletedTask;
    }
}
