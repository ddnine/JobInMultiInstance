using RabbitMQ.Client;

namespace JobEventBusRabbitMq
{
    /// <summary>
    /// RabbitMQ 连接接口
    /// </summary>
    public interface IJobRabbitMqConnection
         : IDisposable
    {
        bool IsConnected { get; }

        bool TryConnect();

        IModel CreateModel();
    }
}
