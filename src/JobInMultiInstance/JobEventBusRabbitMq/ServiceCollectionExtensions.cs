using System.Reflection;
using JobEventBus.Abstractions;
using JobEventBus.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobEventBusRabbitMq;

public static class ServiceCollectionExtensions
{
    public static IJobEventBusBuilder AddJobRabbitMqEventBus(this IServiceCollection services,IConfiguration configuration, params Assembly[] assemblies)
    {
        services.AddOptions();
        services.Configure<JobEventBusRabbitMqOptions>(configuration.GetSection("EventBus"));
        var builder = new JobEventBusBuilder(services);
        builder.AddEventBusServiceDependency(assemblies);
        return builder;
    }
    
    public static IJobEventBusBuilder AddJobRabbitMqEventBus(this IServiceCollection services, Action<JobEventBusRabbitMqOptions> configAction, params Assembly[] assemblies)
    {
        services.AddOptions();
        services.Configure(configAction);
        var builder = new JobEventBusBuilder(services);
        builder.AddEventBusServiceDependency(assemblies);
        return builder;
    }
    
    private static IJobEventBusBuilder AddEventBusServiceDependency(this IJobEventBusBuilder builder,params Assembly[] assemblies)
    {
        // 注册所有的IIntegrationEventHandler
        builder.AutoSubscription(assemblies);
        builder.Services.AddSingleton<IJobRabbitMqConnection,JobRabbitMqConnection>();
        builder.Services.AddSingleton<IJobEventBus, JobEventBusRabbitMq>();
        // 当服务启动时，立即开始消费消息
        builder.Services.AddSingleton<IHostedService>(sp => (JobEventBusRabbitMq)sp.GetRequiredService<IJobEventBus>());
        return builder;
    }
    
    private class JobEventBusBuilder : IJobEventBusBuilder
    {
        public JobEventBusBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
        
}