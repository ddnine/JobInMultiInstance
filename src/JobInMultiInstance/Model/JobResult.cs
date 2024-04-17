using System.Runtime.Serialization;

namespace JobInMultiInstance.Model;

public class JobResult
{
    public const int SUCCESS_CODE = 200;
    public const int FAIL_CODE = 500;

    public static readonly JobResult SUCCESS = new JobResult(SUCCESS_CODE, null);
    public static readonly JobResult FAIL = new JobResult(FAIL_CODE, null);
    public static readonly JobResult FAIL_TIMEOUT = new JobResult(502, null);
        
    public JobResult() { }

    public JobResult(int code, string msg)
    {
        Code = code;
        Msg = msg;
    }
        
        
    [DataMember(Name = "code",Order = 1)]
    public  int Code { get; set; }
    [DataMember(Name = "msg",Order = 2)]
    public string Msg { get; set; }
        
    [DataMember(Name = "content",Order = 3)]
    public object Content { get; set; }
        
      

    public static JobResult Failed(string msg)
    {
        return new JobResult(FAIL_CODE, msg);
    }
    public static JobResult Success(string msg)
    {
        return new JobResult(SUCCESS_CODE, msg);
    }
        
}