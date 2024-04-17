using JobInMultiInstance.Model;
using Microsoft.Extensions.Options;

namespace JobInMultiInstance;

public class DefaultJobHandlerFactory:IJobHandlerFactory
{
    private readonly JobHandlerRegisterInfo _registerInfo;
    public DefaultJobHandlerFactory(IOptions<JobHandlerRegisterInfo> registerOptions)
    {
        _registerInfo = registerOptions.Value;
    }

        
    public Type? GetJobHandlerType(string handlerName)
    {
        return _registerInfo.EventTypes.TryGetValue(handlerName, value: out var handler) ? handler : null;
    }
}