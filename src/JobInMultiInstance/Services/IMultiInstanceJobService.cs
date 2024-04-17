using JobInMultiInstance.Model;

namespace JobInMultiInstance.Services;

public interface IMultiInstanceJobService
{
    Task RunJobAsync<T>(string key, string message, bool isBroadcast = true) where T : IJobHandler;
    Task KillJobAsync(string key, string handlerName, bool isBroadcast = true);

    Task RunJobAsync(string key, string message, string handlerName, bool isBroadcast = true);
    Task ExecuteJob(string key, string message, string methodName, string handlerName, bool isBroadcast = true);

    /// <summary>
    /// 将Key转换成MD5Key进行存储
    /// </summary>
    /// <param name="key"></param>
    /// <param name="message"></param>
    /// <param name="handlerName"></param>
    /// <param name="isBroadcast"></param>
    Task RunJobAsMd5KeyAsync(string key, string message, string handlerName, bool isBroadcast = true );

    Task KillJobAsMd5KeyAsync(string key, string handlerName, bool isBroadcast = true);
    Task ExecuteJobAsMd5Key(string key, string message, string methodName, string handlerName, bool isBroadcast = true);
    Task RunJobAsMd5KeyAsync<T>(string key, string message, bool isBroadcast = true) where T : IJobHandler;
    Task KillJobAsync<T>(string key, bool isBroadcast = true) where T : IJobHandler;
    Task KillJobAsMd5KeyAsync<T>(string key, bool isBroadcast = true) where T : IJobHandler;
    Task<JobModel?> GetJobAsMd5Key<T>(string key)  where T : IJobHandler;
    Task<JobModel?> GetJobAsMd5Key(string key, string handlerName);
    Task<JobModel?> GetJob<T>(string key)  where T : IJobHandler;
    Task<JobModel?> GetJob(string key ,string handlerName);
}