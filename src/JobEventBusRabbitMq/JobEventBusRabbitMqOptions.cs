namespace JobEventBusRabbitMq;

/// <summary>
/// 
/// </summary>
public class JobEventBusRabbitMqOptions
{
    /// <summary>
    /// 主机地址
    /// </summary>
    public string EventBusConnection { get; set; } = "";
    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; }
    /// <summary>
    /// 用户名
    /// </summary>
    public string EventBusUserName { get; set; } = "";
    /// <summary>
    /// 密码
    /// </summary>
    public string EventBusPassword { get; set; } = "";
    /// <summary>
    /// 客户端提供的名称
    /// </summary>
    public string ClientProvidedName { get; set; } = "DefaultClientProvidedName";
    /// <summary>
    /// 交换机名称
    /// </summary>
    public string ExchangeName { get; set; } = "";
    /// <summary>
    /// 队列名称
    /// </summary>
    public string SubscriptionClientName { get; set; } = "";
    /// <summary>
    /// 交换机类型 topic, direct, fanout, headers
    /// </summary>
    public string ExchangeType { get; set; } = "topic";
    
    public int EventBusRetryCount { get; set; } = 3;
    /// <summary>
    /// 是否开启SSL
    /// </summary>
    public bool OpenSSL { get; set; } = false;
    
    public string VirtualHost { get; set; } = "/";
}