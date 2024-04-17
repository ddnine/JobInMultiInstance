using JobInMultiInstance.Model;

namespace JobInMultiInstance.TaskExecutors;

public interface ITaskExecutor
{
    string GlueType { get; }

    Task<JobResult> Execute(JobParam jobParam, CancellationToken cancellationToken);
}