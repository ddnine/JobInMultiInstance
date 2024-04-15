using System.Security.Cryptography;
using System.Text;
using JobEventBus.Abstractions;
using JobEventBus.Events;
using JobInMultiInstance.IntegrationEvents;
using JobInMultiInstance.Model;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace JobInMultiInstance.Services;

public class MultiInstanceJobService : IMultiInstanceJobService
{
    private readonly JobProvider _jobProvider;
    private readonly IDistributedCache _cache;
    private readonly IJobEventBus _eventBus;

    /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
    public MultiInstanceJobService(
        JobProvider jobProvider, 
        IDistributedCache cache, 
        IJobEventBus eventBus)
    {
        _jobProvider = jobProvider;
        _cache = cache;
        _eventBus = eventBus;
    }
    
    public async Task RunJobAsync<T>(string key, string message, bool isBroadcast = true) where T : IJobHandler
    {
        var handler = typeof(T).Name;
        
        await RunJobAsync(key,message,handler, isBroadcast);
    }
    
    public async Task RunJobAsync(string key, string message, string handlerName, bool isBroadcast = true)
    {
        var jobExists = await RunningJobExists(key, handlerName);
        
        if (handlerName == "")
        {
            //throw new Exception("Handler not found");
            return;
        }
        
        var param = new JobParam
        {
            JobKey = key,
            ExecutorTimeout = 43200,
            ExecutorBlockStrategy = "COVER_EARLY",
            Content = message,
            ExecutorParams = message,
            ExecutorHandler = handlerName
        };
        
        // 如果任务已在执行中,则要判断是否是该实例的
        if (jobExists)
        {
            // 如果任务已在执行中,则需要先调用idlebeat方法,判断任务是否是该实例的.
            // 如果不是该实例的,则需要发送广播事件
            var result = _jobProvider.Handle("idlebeat", param);
            if (result.Code == JobResult.FAIL_CODE)
            {
                if(isBroadcast)
                {
                    _eventBus.Publish(new BroadcastIntegrationEvent
                    {
                        Key = key,
                        Message = message,
                        MethodName = "run",
                        HandlerName = handlerName
                    });
                }
                return;
            }
            // 如果是该实例的,则直接调用run方法
            _jobProvider.Handle("run", param);
            return;
        }
        // 如果任务不在执行中,则直接调用runNew方法
        _jobProvider.Handle("runNew", param);
    }
    
    /// <summary>
    /// 将Key转换成MD5Key进行存储
    /// </summary>
    /// <param name="key"></param>
    /// <param name="message"></param>
    /// <param name="handlerName"></param>
    /// <param name="isBroadcast"></param>
    public async Task RunJobAsMd5KeyAsync(string key, string message, string handlerName, bool isBroadcast = true )
    {
        var md5Key = GetJobKey(key);
        await RunJobAsync(md5Key, message, handlerName, isBroadcast);
    }
    
    public async Task RunJobAsMd5KeyAsync<T>(string key, string message, bool isBroadcast = true) where T : IJobHandler
    {
        var md5Key = GetJobKey(key);
        await RunJobAsync<T>(md5Key,message, isBroadcast);
    }
    public async Task KillJobAsync<T>(string key, bool isBroadcast = true) where T : IJobHandler
    {
        var handler = typeof(T).Name;
        
        await KillJobAsync(key,handler, isBroadcast);
    }
    
    public async Task KillJobAsync(string key, string handlerName, bool isBroadcast = true)
    {
        var jobExists = await RunningJobExists(key, handlerName);
        if (!jobExists)
        {
            return;
        }
        var param = new JobParam
        {
            JobKey = key,
            ExecutorTimeout = 43200,
        };
        // 判断任务是否为该实例的
        var result = _jobProvider.Handle("idlebeat", param);
        if (result.Code == JobResult.FAIL_CODE)
        {
            if(isBroadcast)
            {
                _eventBus.Publish(new BroadcastIntegrationEvent
                {
                    Key = key,
                    MethodName = "kill",
                    HandlerName = handlerName
                });
            }
            return;
        }
        // 如果是该实例的,则直接调用kill方法
        _jobProvider.Handle("kill", param);
    }
    
    public async Task KillJobAsMd5KeyAsync<T>(string key, bool isBroadcast = true) where T : IJobHandler
    {
        var handler = typeof(T).Name;
        
        await KillJobAsMd5KeyAsync(key,handler, isBroadcast);
    }
    
    public async Task KillJobAsMd5KeyAsync(string key, string handlerName, bool isBroadcast = true)
    {
        var md5Key = GetJobKey(key);
        await KillJobAsync(md5Key, handlerName, isBroadcast);
    }
    
    public async Task ExecuteJob(string key, string message, string methodName, string handlerName, bool isBroadcast = true)
    {
        if ( methodName == "kill")
        {
           await KillJobAsync(key,handlerName, isBroadcast);
           return;
        }
        
        // run需要handlerName
        if(string.IsNullOrEmpty(handlerName))
            return;
        
        await RunJobAsync(key,message,handlerName,isBroadcast); 
    }
    
    public async Task ExecuteJobAsMd5Key(string key, string message, string methodName, string handlerName, bool isBroadcast = true)
    {
        var md5Key = GetJobKey(key);
        await ExecuteJob(md5Key, message, methodName,handlerName,isBroadcast);
    }
    
    public async Task<JobModel?> GetJobAsMd5Key<T>(string key)  where T : IJobHandler
    {
        var handlerName = typeof(T).Name;
        return await GetJobAsMd5Key(key,handlerName);
    }
    public async Task<JobModel?> GetJobAsMd5Key(string key, string handlerName) 
    {
        var md5Key = GetJobKey(key);
        return await GetJob(md5Key,handlerName);
    }
    
    public async Task<JobModel?> GetJob<T>(string key)  where T : IJobHandler
    {
        var handlerName = typeof(T).Name;
        return await GetJob(key,handlerName);
    }
    
    public async Task<JobModel?> GetJob(string key ,string handlerName)
    {
        var cacheValue = await _cache.GetStringAsync($"job:{handlerName}:{key}");
        if (cacheValue == null)
        {
            return null;
        }
        var jobModel = JsonConvert.DeserializeObject<JobModel>(cacheValue);
        return jobModel;
    }
    
    private async Task<bool> RunningJobExists(string key, string handlerName)
    {
        //判断任务是否已在执行中
        var cacheValue = await _cache.GetStringAsync($"job:{handlerName}:{key}");
        if (cacheValue == null)
        {
            return false;
        }
        var jobModel = JsonConvert.DeserializeObject<JobModel>(cacheValue);
        return jobModel?.JobStatus == 1;
    }
    
    private static string GetJobKey(string key)
    {
        // 通过MD5加密key, 作为任务的唯一标识
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}