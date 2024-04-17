using JobInMultiInstance.TaskExecutors;
using Microsoft.Extensions.DependencyInjection;

namespace JobInMultiInstance;

/// <summary>
/// 负责响应RPC请求，调度任务执行器的工厂类
/// </summary>
public class TaskExecutorFactory
{
    private readonly IServiceProvider _provider;

    private readonly Dictionary<string, ITaskExecutor> _memoryCache = new();
    public TaskExecutorFactory(IServiceProvider provider)
    {
        this._provider = provider;
        Initialize();
    }

    private void Initialize()
    {
        var executors =  this._provider.GetServices(typeof(ITaskExecutor));
            
        if (executors is not ITaskExecutor[] taskExecutors || !taskExecutors.Any()) return;
            
        foreach (var item in taskExecutors)
        {
            _memoryCache.Add(item.GlueType,item);
        }
    }

    public ITaskExecutor? GetTaskExecutor(string glueType)
    {
        return _memoryCache.GetValueOrDefault(glueType);
    }
}