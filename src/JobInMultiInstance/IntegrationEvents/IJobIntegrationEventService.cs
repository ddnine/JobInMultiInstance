using JobEventBus.Events;

namespace JobInMultiInstance.IntegrationEvents
{
    public interface IJobIntegrationEventService
    {
        Task PublishEventsThroughEventBusAsync(JobIntegrationEvent evt);
    }
}
