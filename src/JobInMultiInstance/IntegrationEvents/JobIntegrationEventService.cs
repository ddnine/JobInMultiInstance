using JobEventBus.Abstractions;
using JobEventBus.Events;
using Microsoft.Extensions.Logging;

namespace JobInMultiInstance.IntegrationEvents
{
    public class JobIntegrationEventService: IJobIntegrationEventService
    {
        private readonly IJobEventBus _eventBus;
        private readonly ILogger<JobIntegrationEventService> _logger;

        public JobIntegrationEventService(IJobEventBus eventBus,
            ILogger<JobIntegrationEventService> logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task PublishEventsThroughEventBusAsync(JobIntegrationEvent evt)
        {

            _logger.LogInformation("----- Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})", evt.Id, evt);

            try
            {
                //事件发布
                _eventBus.Publish(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR publishing integration event: {IntegrationEventId}", evt.Id);
            }

            return Task.CompletedTask;
        }
    }
}
