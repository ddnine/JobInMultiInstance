using Microsoft.Extensions.DependencyInjection;

namespace JobEventBus.Abstractions;

public interface IJobEventBusBuilder
{
    public IServiceCollection Services { get; }
}
