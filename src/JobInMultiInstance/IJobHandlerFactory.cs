namespace JobInMultiInstance;

public interface IJobHandlerFactory
{
    Type? GetJobHandlerType(string handlerName);
}