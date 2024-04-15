using JobEventBus.Events;

namespace JobEventBus.Abstractions;

/// <summary>
/// 事件中心服务
/// </summary>
public interface IJobEventBus
{
    /// <summary>
    /// 发布
    /// </summary>
    /// <param name="event"></param>
    void Publish(JobIntegrationEvent @event);
}