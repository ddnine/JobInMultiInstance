using System.Reflection;
using JobEventBusRabbitMq;
using JobInMultiInstance.IntegrationEvents;
using JobInMultiInstance.Model;
using JobInMultiInstance.Services;
using JobInMultiInstance.TaskExecutors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JobInMultiInstance.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 需要对每个JobHandler进行注册
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddJobInMultiInstance(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddJobExecutorServiceDependency()
            .AddJobRabbitMqEventBus(configuration);
            
        return services;
    }

    /// <summary>
    /// 通过assemblies参数，自动注册assemblies里所有的IJobHandler实现
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IServiceCollection AddJobInMultiInstance(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
    {
        services
            .AddJobExecutorServiceDependency()
            .AddJobRabbitMqEventBus(configuration, typeof(BroadcastIntegrationEventHandler).Assembly);
            
        // 获取assemblies里所有的IJobHandler实现，并注册
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes();
            foreach (var type in types.Where(t => typeof(IJobHandler).IsAssignableFrom(t) && !t.IsAbstract))
            {
                services.AddScoped(type);
                    
                services.Configure<JobHandlerRegisterInfo>(o =>
                {
                    o.EventTypes[type.Name] = type;
                });
            }
        }
            
        return services;
    }

    /// <summary>
    /// 需要对每个JobHandler进行注册
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configAction"></param>
    /// <returns></returns>
    public static IServiceCollection AddJobInMultiInstance(this IServiceCollection services, Action<JobEventBusRabbitMqOptions> configAction)
    {
        services
            .AddJobExecutorServiceDependency()
            .AddJobRabbitMqEventBus(configAction);
            
        return services;
    }

    /// <summary>
    /// 通过assemblies参数，自动注册assemblies里所有的IJobHandler实现
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configAction"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IServiceCollection AddJobInMultiInstance(this IServiceCollection services, Action<JobEventBusRabbitMqOptions> configAction, params Assembly[] assemblies)
    {
        services
            .AddJobExecutorServiceDependency()
            .AddJobRabbitMqEventBus(configAction, typeof(BroadcastIntegrationEventHandler).Assembly);
            
        // 获取assemblies里所有的IJobHandler实现，并注册
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes();
            foreach (var type in types.Where(t => typeof(IJobHandler).IsAssignableFrom(t) && !t.IsAbstract))
            {
                services.AddScoped(type);
                    
                services.Configure<JobHandlerRegisterInfo>(o =>
                {
                    o.EventTypes[type.Name] = type;
                });
            }
        }
            
        return services;
    }
        
    private static IServiceCollection AddJobExecutorServiceDependency(this IServiceCollection services)
    { 
      
        //可在外部提前注册对应实现，并替换默认实现
        services.TryAddSingleton<IJobHandlerFactory,DefaultJobHandlerFactory >();
            
        services.AddSingleton<JobDispatcher>();
        services.AddSingleton<TaskExecutorFactory>();
        services.AddSingleton<JobProvider>();
        services.AddSingleton<ITaskExecutor, DefaultTaskExecutor>();
        services.AddScoped<IMultiInstanceJobService,MultiInstanceJobService>();
            
        return services;
    }
        
    public static IServiceCollection RegisterJobHandler<TH>(this IServiceCollection services)
        where TH : class, IJobHandler
    {
           
        services.AddScoped<TH>();

        services.Configure<JobHandlerRegisterInfo>(o =>
        {
            o.EventTypes[typeof(TH).Name] = typeof(TH);
        });
        return services;
    }
       
}