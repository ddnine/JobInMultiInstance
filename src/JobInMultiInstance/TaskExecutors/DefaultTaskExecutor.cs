using System.Diagnostics;
using JobInMultiInstance.Model;
using Microsoft.Extensions.DependencyInjection;

namespace JobInMultiInstance.TaskExecutors;

/// <summary>
/// 实现 IJobHandler的执行器
/// </summary>
public class DefaultTaskExecutor : ITaskExecutor
{
    private readonly IJobHandlerFactory _handlerFactory;
    
    private readonly IServiceProvider _serviceProvider;

    public DefaultTaskExecutor(IJobHandlerFactory handlerFactory, IServiceProvider serviceProvider)
    {
        _handlerFactory = handlerFactory;
        _serviceProvider = serviceProvider;
    }

    public string GlueType { get; } = Constants.GlueType.Default;

    public async Task<JobResult> Execute(JobParam jobParam, CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = new JobExecuteContext(jobParam.ExecutorParams, cancellationToken);
        var handlerType =  _handlerFactory.GetJobHandlerType(jobParam.ExecutorHandler);
        if (handlerType is null) JobResult.Failed($"job handler [{jobParam.ExecutorHandler} not found.");
        Debug.Assert(handlerType != null, nameof(handlerType) + " != null");
        var scopeHandler = scope.ServiceProvider.GetRequiredService(handlerType);
        if (scopeHandler == null) JobResult.Failed($"job handler [{jobParam.ExecutorHandler} not found.");
                
                
        //通过反射调用ExcelHelper.ImportExcel方法,并将type作为泛型参数
        var method = handlerType.GetMethod("Execute");
        await Task.Yield();
        return await (Task<JobResult>)method?.Invoke(scopeHandler, new [] {context});
    }
}