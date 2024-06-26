﻿using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace JobEventBusRabbitMq;

public class JobRabbitMqConnection
    : IJobRabbitMqConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<JobRabbitMqConnection> _logger;
    private readonly int _retryCount;
    IConnection _connection;
    bool _disposed;

    object sync_root = new object();

    public JobRabbitMqConnection(ILogger<JobRabbitMqConnection> logger, IOptions<JobEventBusRabbitMqOptions> optionsAccessor)
    {
        var retryCount = 3;
        var options = optionsAccessor.Value;
            
        var factory = new ConnectionFactory()
        {
            HostName =options.EventBusConnection,
            Port = options.Port,
            DispatchConsumersAsync = true
        };

        if (!string.IsNullOrEmpty(options.EventBusUserName))
            factory.UserName = options.EventBusUserName;

        if (!string.IsNullOrEmpty(options.EventBusPassword))
            factory.Password = options.EventBusPassword;

        if (!string.IsNullOrEmpty(options.ClientProvidedName))
            factory.ClientProvidedName = options.ClientProvidedName;

        factory.VirtualHost =options.VirtualHost;
            
        if (options.OpenSSL)
            factory.Ssl = new RabbitMQ.Client.SslOption() { ServerName = options.EventBusConnection, Enabled = true };
            
            
        _connectionFactory = factory;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryCount = retryCount;
    }

    public bool IsConnected
    {
        get
        {
            return _connection != null && _connection.IsOpen && !_disposed;
        }
    }

    public IModel CreateModel()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return _connection.CreateModel();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            if (_connection != null)
                _connection.Dispose();
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex.ToString());
        }
    }

    public bool TryConnect()
    {
        _logger.LogInformation("RabbitMQ Client is trying to connect");

        lock (sync_root)
        {
            var policy = RetryPolicy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
                    }
                );

            policy.Execute(() =>
            {
                _connection = _connectionFactory
                    .CreateConnection();
            });

            if (IsConnected)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;

                _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);

                return true;
            }
            else
            {
                _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                return false;
            }
        }
    }

    private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

        TryConnect();
    }

    void OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

        TryConnect();
    }

    void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

        TryConnect();
    }
}