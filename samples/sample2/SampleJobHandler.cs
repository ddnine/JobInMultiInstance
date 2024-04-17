using JobInMultiInstance;
using JobInMultiInstance.Model;

namespace sample2;

public class SampleJobHandler : AJobHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Task<JobResult> Execute(JobExecuteContext context)
    {
         while (true)
         {
             // 检查CancellationToken是否已经被取消
             context.CancellationToken.ThrowIfCancellationRequested();
             Thread.Sleep(1000);
         }
         return Task.FromResult(JobResult.SUCCESS);
    }
}