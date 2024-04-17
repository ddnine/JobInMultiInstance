using System.Collections.Concurrent;
using JobInMultiInstance.Model;
using JobInMultiInstance.Queue;
using JobInMultiInstance.TaskExecutors;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace JobInMultiInstance;

/// <summary>
/// 负责实际的JOB轮询
/// </summary>
public class JobDispatcher
{
    private readonly TaskExecutorFactory _executorFactory;

    private readonly ConcurrentDictionary<string,JobTaskQueue> RUNNING_QUEUE = new();

    private readonly IDistributedCache _cache;

    private readonly ILogger<JobTaskQueue> _jobQueueLogger;
    public JobDispatcher(
        TaskExecutorFactory executorFactory,
        ILoggerFactory loggerFactory, 
        IDistributedCache cache)
    {
        _executorFactory = executorFactory;
        _cache = cache;

        _jobQueueLogger =  loggerFactory.CreateLogger<JobTaskQueue>();
    }
    
     
    /// <summary>
    /// 尝试移除JobTask
    /// </summary>
    /// <param name="jobKey"></param>
    /// <returns></returns>
    public bool TryRemoveJobTask(string jobKey)
    {
        if (RUNNING_QUEUE.TryGetValue(jobKey, out var jobQueue))
        {
            jobQueue.Stop();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 执行队列，并快速返回结果
    /// </summary>
    /// <param name="jobParam"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public JobResult Execute(JobParam jobParam)
    {

        var executor = _executorFactory.GetTaskExecutor(jobParam.GlueType);
        if (executor == null)
        {
            return JobResult.Failed($"glueType[{jobParam.GlueType}] is not supported ");
        }
            
        // 1. 根据JobId 获取 TaskQueue; 用于判断是否有正在执行的任务
        if (RUNNING_QUEUE.TryGetValue(jobParam.JobKey, out var taskQueue))
        {
            if (taskQueue.Executor != executor) //任务执行器变更
            {
                return ChangeJobQueue(jobParam, executor);
            }
        }

        if (taskQueue != null) //旧任务还在执行，判断执行策略
        {
            //丢弃后续的
            if (Constants.ExecutorBlockStrategy.DISCARD_LATER == jobParam.ExecutorBlockStrategy)
            {
                //存在还没执行完成的任务
                if (taskQueue.IsRunning())
                {
                    return JobResult.Failed($"block strategy effect：{jobParam.ExecutorBlockStrategy}");
                }
                //否则还是继续做
            }
            //覆盖较早的
            if (Constants.ExecutorBlockStrategy.COVER_EARLY == jobParam.ExecutorBlockStrategy)
            {
                return taskQueue.Replace(jobParam);
            }
        }
            
        return PushJobQueue(jobParam, executor);
           
    }


    /// <summary>
    /// IdleBeat
    /// </summary>
    /// <param name="jobKey"></param>
    /// <returns></returns>
    public JobResult IdleBeat(string jobKey)
    {
        if (RUNNING_QUEUE.TryGetValue(jobKey, out var jobQueue))
        {
            if(jobQueue.IsRunning())
                return JobResult.SUCCESS;
            if (RUNNING_QUEUE.TryRemove(jobKey, out var oldJobTask))
            { 
                oldJobTask.Dispose(); //释放原来的资源
            }
                
        }
            
        return JobResult.Failed("job thread is not running.");            
        return RUNNING_QUEUE.ContainsKey(jobKey) ? 
                JobResult.SUCCESS:
                new JobResult(JobResult.FAIL_CODE, "job thread is running or has trigger queue.") 
            ;
    }
        
      
    private JobResult PushJobQueue(JobParam jobParam, ITaskExecutor executor)
    { 
            
        if (RUNNING_QUEUE.TryGetValue(jobParam.JobKey,out var jobQueue))
        {
            return jobQueue.Push(jobParam);
        }
            
        //NewJobId
        jobQueue = new JobTaskQueue( executor, _jobQueueLogger, _cache);
        if (RUNNING_QUEUE.TryAdd(jobParam.JobKey, jobQueue))
        {
            return jobQueue.Push(jobParam);
        }
        return JobResult.Failed("add running queue executor error");
    }
        
    private JobResult ChangeJobQueue(JobParam jobParam, ITaskExecutor executor)
    {
           
        if (RUNNING_QUEUE.TryRemove(jobParam.JobKey, out var oldJobTask))
        { 
            oldJobTask.Dispose(); //释放原来的资源
        }
            
        JobTaskQueue jobQueue = new JobTaskQueue ( executor, _jobQueueLogger, _cache);
        if (RUNNING_QUEUE.TryAdd(jobParam.JobKey, jobQueue))
        {
            return jobQueue.Push(jobParam);
        }
        return JobResult.Failed(" replace running queue executor error");
    }
}