using JobInMultiInstance.Model;

namespace JobInMultiInstance;

public abstract class AJobHandler:IJobHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    public abstract Task<JobResult> Execute(JobExecuteContext context);


    public virtual void Dispose()
    {
    }
}

public interface IJobHandler:IDisposable
{
    Task<JobResult> Execute(JobExecuteContext context);
}