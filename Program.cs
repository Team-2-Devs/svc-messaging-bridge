using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Messaging.RabbitMQ;
using MessagingBridge.Workers;
using Messaging.Kafka;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

// Read required environment variables (fail fast if missing)
static string RequireEnv(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"Missing environment variable: {name}");

var rabbitHost = RequireEnv("RABBIT_HOST");
var rabbitUser = RequireEnv("RABBIT_USER");
var rabbitPass = RequireEnv("RABBIT_PASS");
var rabbitPort = int.Parse(RequireEnv("RABBIT_PORT"));
var kafkaBrokers = RequireEnv("KAFKA_BROKERS");

Console.WriteLine($"[Bridge] RabbitMQ: host={rabbitHost}, port={rabbitPort}, user={rabbitUser}");

builder.Services.AddSingleton<IEventPublisher>(_ => new RabbitMQPublisher(rabbitHost, rabbitUser, rabbitPass, rabbitPort));
builder.Services.AddKafkaMessaging(
    clientId: "svc-messaging-bridge",
    groupId: "svc-messaging-bridge");

builder.Services.AddHostedService(sp =>
    new KafkaToRabbitWorker(
        sp.GetRequiredService<IMessageConsumer>(),
        sp.GetRequiredService<IEventPublisher>()));
        
// Health checks (live/ready)
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddRabbitMQ(
        serviceProvider => new ConnectionFactory { HostName = rabbitHost, UserName = rabbitUser, Password = rabbitPass, Port = rabbitPort }
                    .CreateConnectionAsync(),
        name: "rabbitmq",
        tags: new[] { "ready" }
    );

// Graceful shutdown: Give background services time to stop cleanly
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(10));

var app = builder.Build();

// By filtering by tag, we prevent all checks from failing together.
// This way, each endpoint only runs its own checks.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

app.Run();