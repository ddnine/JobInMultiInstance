using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EventBus;
using JobEventBus.Abstractions;
using JobEventBus.Events;
using Microsoft.Extensions.DependencyInjection;

namespace JobEventBus.Extensions;

public static class EventBusBuilderExtensions
{
  public static IJobEventBusBuilder AutoSubscription(this IJobEventBusBuilder builder, params Assembly[] assemblies)
  {
      // 获取assemblies里所有的IJobHandler实现，并注册
      foreach (var assembly in assemblies)
      {
          var types = assembly.GetTypes();
          foreach (var type in types.Where(t => typeof(IJobIntegrationEventHandler).IsAssignableFrom(t) && !t.IsAbstract))
          {
              // 获取Type的泛型参数
              var genericArgs = type.GetInterfaces()[0].GenericTypeArguments;
              if(genericArgs == null || genericArgs.Length == 0)
              {
                  continue;
              }
              builder.Services.AddTransient(type);
              builder.Services.Configure<JobSubscriptionInfo>(o =>
              {
                  if(!o.EventHandlerTypes.ContainsKey(genericArgs[0].Name))
                  {
                      o.EventHandlerTypes[genericArgs[0].Name] = new List<Type>();
                  }
                  o.EventHandlerTypes[genericArgs[0].Name].Add(type);
                  o.EventTypes[genericArgs[0].Name] = genericArgs[0];   
              });
          }
      }
          
      return builder;
  }
  
  public static IJobEventBusBuilder AddSubscription<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TH>(this IJobEventBusBuilder builder)
      where T : JobIntegrationEvent
      where TH : class, IJobIntegrationEventHandler<T>
  {
      builder.Services.AddTransient<TH>();
      builder.Services.Configure<JobSubscriptionInfo>(o =>
      {
          o.EventHandlerTypes[typeof(T).Name].Add(typeof(TH));
          o.EventTypes[typeof(T).Name] = typeof(T);   
      });
      
          
      return builder;
  }
}
