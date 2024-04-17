namespace JobInMultiInstance.Model;

public class JobParam
{
    public string JobKey { get; set; }
        
    public string Content { get; set; }
        
    public string ExecutorHandler { get; set; }
        
    public string ExecutorParams{ get; set; }
        
    public string ExecutorBlockStrategy{ get; set; }
        
    public int ExecutorTimeout{ get; set; }
        
    public long LogId { get; set; }

    public string GlueType{ get; set; } = "Default";
}