using System.Collections.Concurrent;
using JobInMultiInstance.Model;
using JobInMultiInstance.TaskExecutors;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace JobInMultiInstance.Queue;

public class JobTaskQueue : IDisposable
{
    private readonly ILogger<JobTaskQueue> _logger;
    private readonly ConcurrentQueue<JobParam> _taskQueue = new();
    private readonly ConcurrentDictionary<long, byte> _idInQueue = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _runTask;
    private readonly IDistributedCache _cache;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executor"></param>
    /// <param name="logger"></param>
    /// <param name="cache"></param>
    public JobTaskQueue(ITaskExecutor executor, ILogger<JobTaskQueue> logger, IDistributedCache cache)
    {
        Executor = executor;
        _logger = logger;
        _cache = cache;
    }

    public ITaskExecutor Executor { get; }
        
    public bool IsRunning()
    {
        return _cancellationTokenSource != null;
    }


    /// <summary>
    /// 覆盖之前的队列
    /// </summary>
    /// <param name="jobParam"></param>
    /// <returns></returns>
    public JobResult Replace(JobParam jobParam)
    {
        while (!_taskQueue.IsEmpty)
        {
            _taskQueue.TryDequeue(out _);
        }
        Stop();
        _idInQueue.Clear();

        return Push(jobParam);
    }

    public JobResult Push(JobParam jobParam)
    {
        if (!_idInQueue.TryAdd(jobParam.LogId, 0))
        {
            _logger.LogWarning("repeat job task,logId={LogId},jobKey={JobKey}", jobParam.LogId, jobParam.JobKey);
            return JobResult.Failed("repeat job task!");
        }
            
        _taskQueue.Enqueue(jobParam);
        StartTask();
        return JobResult.SUCCESS;
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        //wait for task completed
        _runTask?.GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        while (!_taskQueue.IsEmpty)
        {
            _taskQueue.TryDequeue(out _);
        }
        _idInQueue.Clear();
        Stop();
    }

    private void StartTask()
    {
        if (_cancellationTokenSource != null)
        {
            return; //running
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var ct = _cancellationTokenSource.Token;

        _runTask = Task.Factory.StartNew(async () =>
        {
            //ct.ThrowIfCancellationRequested();

            while (!ct.IsCancellationRequested)
            {
                if (_taskQueue.IsEmpty)
                {
                    //_logger.LogInformation("task queue is empty!");
                    break;
                }
                    
                if (_taskQueue.TryDequeue(out var triggerParam))
                {
                    try
                    {
                        if (!_idInQueue.TryRemove(triggerParam.LogId, out _))
                        {
                            _logger.LogWarning("remove queue failed,logId={LogId},jobKey={JobKey},exists={Exists}", triggerParam.LogId, triggerParam.JobKey, _idInQueue.ContainsKey(triggerParam.LogId));
                        }

                        _logger.LogInformation("<br>----------- job execute start -----------<br>----------- Param:{ExecutorParams}", triggerParam.ExecutorParams);
                        AddToCache(triggerParam);
                        var exectorToken = ct;
                        CancellationTokenSource? timeoutCts = null;
                        if (triggerParam.ExecutorTimeout > 0)
                        {
                            timeoutCts = new CancellationTokenSource(triggerParam.ExecutorTimeout * 1000);
                            exectorToken = CancellationTokenSource.CreateLinkedTokenSource(exectorToken, timeoutCts.Token).Token;
                        }

                        var result = await Executor.Execute(triggerParam, exectorToken);
                        if (timeoutCts != null && timeoutCts.IsCancellationRequested)
                        {
                            result = JobResult.FAIL_TIMEOUT;
                            timeoutCts.Dispose();
                            timeoutCts = null;
                            SetJobStatus(triggerParam, 4);
                        }

                        _logger.LogInformation("<br>----------- job execute end(finish) -----------<br>----------- Result" +
                            ":{Code}", result.Code);
                        SetJobStatus(triggerParam, 2);
                    }
                    catch (Exception ex)
                    {
                        if ( ex is OperationCanceledException || ex.InnerException is OperationCanceledException)
                        {
                            _logger.LogInformation("JobThread Cancelled");
                            SetJobStatus(triggerParam, 5);
                            return;
                        }
                        SetJobStatus(triggerParam, 3);
                        _logger.LogInformation("<br>----------- JobThread Exception:{Message}<br>----------- job execute end(error) -----------", ex.Message);
                    }
                }
                else
                {
                    _logger.LogWarning("Dequeue Task Failed");
                }
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }, _cancellationTokenSource.Token);
            
    }
        
    private void AddToCache(JobParam jobParam)
    {
        var key = $"job:{jobParam.ExecutorHandler}:{jobParam.JobKey}";
        var value = JsonConvert.SerializeObject(jobParam);
        var jobModel = new JobModel()
        {
            JobKey = jobParam.JobKey,
            StartTime = DateTime.Now,
            Content = value,
            JobStatus = 1
                
        };
        var jobModelString = JsonConvert.SerializeObject(jobModel);
        _cache.SetString(key, jobModelString, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(jobParam.ExecutorTimeout)
        });
    }
        
    /// <summary>
    /// 0. 未执行 1. 执行中 2. 执行成功 3. 执行失败 4. 超时 5. 任务停止
    /// </summary>
    /// <param name="jobParam"></param>
    /// <param name="status"></param>
    private void SetJobStatus(JobParam jobParam, int status)
    {
        var key = $"job:{jobParam.ExecutorHandler}:{jobParam.JobKey}";
        var value = _cache.GetString(key);
        if (value == null)
        {
            return;
        }
        var jobModel = JsonConvert.DeserializeObject<JobModel>(value);
            
        if (jobModel == null)
        {
            return;
        }

        jobModel.JobStatus = status;
        var jobModelString = JsonConvert.SerializeObject(jobModel);
        _cache.SetString(key, jobModelString, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(jobParam.ExecutorTimeout)
        });
    }
        
    private void RemoveFromCache(JobParam jobParam)
    {
        var key = $"job:{jobParam.ExecutorHandler}:{jobParam.JobKey}";
        _cache.Remove(key);
    }
}