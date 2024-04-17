using JobInMultiInstance.Extensions;
using sample;

var builder = WebApplication.CreateBuilder(args);

//configuration
 var configurationManager = new ConfigurationManager();
 configurationManager.SetBasePath(Directory.GetCurrentDirectory())
     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
     .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? ""}.json", optional: true)
     .AddEnvironmentVariables();


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddStackExchangeRedisCache(options =>
{
    //todo Redis连接字符串需要修改
    options.Configuration = "redis-master.dataspace:6379,password=admin,abortConnect=false";
    options.InstanceName = "SampleJob";
});

builder.Services.AddJobInMultiInstance(configurationManager,typeof(SampleJobHandler).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();