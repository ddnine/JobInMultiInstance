namespace JobInMultiInstance.Model;

public class JobHandlerRegisterInfo
{
    public Dictionary<string, Type> EventTypes { get; } = new();
}
