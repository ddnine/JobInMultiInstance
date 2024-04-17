namespace JobInMultiInstance.Model;

public class JobModel
{
    public string? JobKey { get; set; }
    public string? Content { get; set; }
    // 0. 未执行 1. 执行中 2. 执行成功 3. 执行失败 4. 超时 5. 任务停止
    public int JobStatus { get; set; }
    public DateTime? StartTime { get; set;}
}