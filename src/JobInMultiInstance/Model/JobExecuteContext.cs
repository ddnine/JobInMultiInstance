namespace JobInMultiInstance.Model;

public class JobExecuteContext
{
    public JobExecuteContext(string jobParameter, CancellationToken cancellationToken)
    {
        this.JobParameter = jobParameter;
        this.CancellationToken = cancellationToken;
    }
    public string JobParameter { get; }
    public CancellationToken CancellationToken { get; }
}