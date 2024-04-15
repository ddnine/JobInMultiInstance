using JobInMultiInstance.Model;
using Microsoft.Extensions.Logging;

namespace JobInMultiInstance;

public class JobProvider
{
    private readonly JobDispatcher _jobDispatcher;
    private readonly ILogger<JobProvider> _logger;

    public JobProvider(
        JobDispatcher jobDispatcher,
        ILogger<JobProvider> logger)
    {
        _jobDispatcher = jobDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// 多实例的版本中,调用run方法前先判断Redis里是否存在该任务.
    /// Redis存在该任务的话需要先调用idlebeat方法,判断任务是否是该实例的.
    /// 如果不是该实例的,则需要发送广播事件
    /// </summary>
    /// <param name="method"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public JobResult Handle(string method, JobParam param)
    {
        JobResult? ret = null;
            
        try
        {
            switch (method)
            {
                case "beat":
                    ret = Beat();
                    break;
                case "idlebeat":
                    ret = IdleBeat(param);
                    break;
                case "run":
                    ret = IdleBeat(param);
                    if(ret.Code == JobResult.FAIL_CODE)
                        break;
                    ret = Run(param);
                    break;
                case "runNew":
                    ret = Run(param);
                    break;
                case "kill":
                    ret = IdleBeat(param);
                    if(ret.Code == JobResult.FAIL_CODE)
                        break;
                    ret = Kill(param);
                    break;
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex,"响应出错{Message}" , ex.Message);
            ret = JobResult.Failed("执行器内部错误");
        }


        return ret ?? JobResult.Failed($"method {method}  is not impl");
    }
        
     
        
    private static JobResult Beat()
    {
        return JobResult.SUCCESS;
    }

    private JobResult IdleBeat(JobParam param)
    {
        return _jobDispatcher.IdleBeat(param.JobKey);
    }

    private JobResult Kill(JobParam param)
    {
            
        return _jobDispatcher.TryRemoveJobTask(param.JobKey) ?
            JobResult.SUCCESS
            :
            JobResult.Success("job thread already killed.");
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="jobParam"></param>
    /// <returns></returns>
    private JobResult Run(JobParam jobParam)
    {
        return _jobDispatcher.Execute(jobParam);
    }
}