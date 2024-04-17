# JobInMultiInstance
    本项目旨在管理跨多个服务实例的长时间运行线程任务。
    它允许调用接口启动或取消任务，而无需关心实际执行任务的具体实例。
    这为分布式任务管理提供了一个高效且易于使用的解决方案。

主要特点:

任务管理：支持启动和取消长时间运行的线程任务。

透明执行：用户操作简单，无需了解后端逻辑和分布式系统的复杂性。

分布式协调：使用Redis存储任务状态，通过RabbitMQ实现实例间的通信。

注入方式:

    builder.Services.AddJobInMultiInstance(configurationManager,typeof(SampleJobHandler).Assembly);

配置:

    "EventBus": {
        "EventBusConnection": "rabbitmq.dataspace",
        "Port": 5672,
        "SubscriptionClientName": "JobSample",
        "ClientProvidedName": "client_JobSample_api",
        "ExchangeName": "JobSample",
        "EventBusUserName": "admin",
        "EventBusPassword": "admin",
        "VirtualHost": "/",
        "EventBusRetryCount": 5,
        "OpenSSL": false
    }

该项目未注入Redis,需要在您的项目中自己注入Redis,例如:

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = "redis-master.dataspace:6379,password=admin,abortConnect=false";
        options.InstanceName = "SampleJob";
    });

通过IMultiInstanceJobService接口进行使用:

    // 获取Job信息
    await _multiInstanceJobService.GetJob<SampleJobHandler>(key);
    // 启动Job
    await _multiInstanceJobService.RunJobAsync<SampleJobHandler>(key,"test");
    // 停止Job
    await _multiInstanceJobService.KillJobAsync<SampleJobHandler>(key);
    