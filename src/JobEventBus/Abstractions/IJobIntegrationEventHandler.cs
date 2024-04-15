using JobEventBus.Events;

namespace JobEventBus.Abstractions;

/// <summary>
/// 抽象集成事件处理器
/// </summary>
/// <typeparam name="TIntegrationEvent"></typeparam>
public interface IJobIntegrationEventHandler<in TIntegrationEvent> : IJobIntegrationEventHandler
    where TIntegrationEvent : JobIntegrationEvent
{
    Task Handle(TIntegrationEvent @event);
}

public interface IJobIntegrationEventHandler
{
}