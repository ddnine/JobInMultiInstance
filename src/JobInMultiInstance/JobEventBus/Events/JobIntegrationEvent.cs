using Newtonsoft.Json;

namespace JobEventBus.Events;

public record JobIntegrationEvent
{
    public JobIntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }

    [System.Text.Json.Serialization.JsonConstructor]
    public JobIntegrationEvent(Guid id, DateTime createDate)
    {
        Id = id;
        CreationDate = createDate;
    }

    [JsonProperty]
    public Guid Id { get; private init; }

    [JsonProperty]
    public DateTime CreationDate { get; private init; }
}