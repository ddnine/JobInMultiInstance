using System.Net.Sockets;
using System.Text;
using EventBus;
using JobEventBus.Abstractions;
using JobEventBus.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace JobEventBusRabbitMq
{
    public class JobEventBusRabbitMq : IJobEventBus, IDisposable, IHostedService
    {
        private readonly IJobRabbitMqConnection _connection;
        private readonly ILogger<JobEventBusRabbitMq> _logger;
        private readonly JobSubscriptionInfo _jobSubscriptionInfo;
        private readonly IServiceProvider _serviceProvider;
        private readonly int _retryCount;

        private IModel? _consumerChannel;

        private readonly string _queueName;
        private readonly string _exchangeName;
        private readonly string _exchangeType;


        public JobEventBusRabbitMq(
            IJobRabbitMqConnection connection, 
            ILogger<JobEventBusRabbitMq> logger, 
            IOptions<JobEventBusRabbitMqOptions> optionsAccessor, 
            IOptions<JobSubscriptionInfo> subscriptionOptions, 
            IServiceProvider serviceProvider)
        {
            var options = optionsAccessor.Value;

            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // 所有队列名不同,这样每个实例都是单独的队列,起到广播每个实例的作用.绑定时exclusive=true
            _queueName = $"{options.SubscriptionClientName}-{Guid.NewGuid()}";
            _exchangeName = options.ExchangeName;
            //默认topic       
            _exchangeType = string.IsNullOrEmpty(options.ExchangeType) ? "topic" : options.ExchangeType;
            _retryCount = options.EventBusRetryCount;
            _serviceProvider = serviceProvider;
            _jobSubscriptionInfo = subscriptionOptions.Value;
        }
        
        /// <summary>
        /// 发布
        /// </summary>
        /// <param name="event"></param>
        public void Publish(JobIntegrationEvent @event)
        {
            if (!_connection.IsConnected)
            {
                _connection.TryConnect();
            }

            var policy = RetryPolicy.Handle<BrokerUnreachableException>().Or<SocketException>().WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
            });

            var eventName = @event.GetType().Name;

            _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventName);

            using (var channel = _connection.CreateModel())
            {
                _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

                channel.ExchangeDeclare(exchange: _exchangeName, type: _exchangeType, durable: true);

                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);

                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2; // persistent

                    _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.Id);

                    channel.BasicPublish(exchange: _exchangeName, routingKey: eventName, mandatory: true, basicProperties: properties, body: body);
                });
            }
        }

        /// <summary>
        /// 确认消费消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }

                await ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
            }

            // Even on exception we take the message off the queue.
            // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
            // For more information see: https://www.rabbitmq.com/dlx.html
            _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task ProcessEvent(string eventName, string message)
        {
            _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);
            
            if (!_jobSubscriptionInfo.EventHandlerTypes.TryGetValue(eventName, out var handlerTypes) 
                || !_jobSubscriptionInfo.EventTypes.TryGetValue(eventName, out var eventType))
            {
                _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
                return;
            }
            
            await using var scope = _serviceProvider.CreateAsyncScope();
            // 反序列化消息,获取事件
            var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

            foreach (var handlerType in handlerTypes)
            {
                var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                if (handler is null) continue;
                var concreteType = typeof(IJobIntegrationEventHandler<>).MakeGenericType(eventType);
                if(concreteType is null) continue;
                await Task.Yield();
                await (Task)concreteType.GetMethod("Handle").Invoke(handler, new[] { integrationEvent });
            }
        }
        
        public void Dispose()
        {
            _consumerChannel?.Dispose();
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Messaging is async so we don't need to wait for it to complete. On top of this
            // the APIs are blocking, so we need to run this on a background thread.
            _ = Task.Factory.StartNew(() =>
            {
                try
                {
                    _logger.LogInformation("Starting RabbitMQ connection on a background thread");
    
                    if (!_connection.IsConnected)
                    {
                        _connection.TryConnect();
                    }
    
                    
                    _logger.LogTrace("Creating RabbitMQ consumer channel");
    
                    _consumerChannel = _connection.CreateModel();
    
                    _consumerChannel.CallbackException += (sender, ea) =>
                    {
                        _logger.LogWarning(ea.Exception, "Error with RabbitMQ consumer channel");
                    };
    
                    _consumerChannel.ExchangeDeclare(exchange: _exchangeName,
                                            type: _exchangeType, true);
    
                    _consumerChannel.QueueDeclare(queue: _queueName,
                                         durable: true,
                                         exclusive: true,
                                         autoDelete: false,
                                         arguments: null);
                    
                    _logger.LogTrace("Starting RabbitMQ basic consume");
    
                    var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
    
                    consumer.Received += Consumer_Received;
    
                    _consumerChannel.BasicConsume(
                        queue: _queueName,
                        autoAck: false,
                        consumer: consumer);
    
                    foreach (var (eventName, _) in _jobSubscriptionInfo.EventHandlerTypes)
                    {
                        _consumerChannel.QueueBind(
                            queue: _queueName,
                            exchange: _exchangeName,
                            routingKey: eventName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting RabbitMQ connection");
                }
            },
            TaskCreationOptions.LongRunning);
    
            return Task.CompletedTask;
        }
    
        public Task StopAsync(CancellationToken cancellationToken)
        {
            //如果服务停了,RabbitMQ的队列没解绑则需要下面代码解绑
            // if (!_connection.IsConnected)
            // {
            //     _connection.TryConnect();
            // }
            //
            // using (var channel = _connection.CreateModel())
            // {
            //     channel.QueueUnbind(queue: _queueName, exchange: _exchangeName, routingKey: eventName);
            //
            //     if (_subsManager.IsEmpty)
            //     {
            //         _queueName = string.Empty;
            //         _consumerChannel.Close();
            //     }
            // }
            return Task.CompletedTask;
        } 
    }
}