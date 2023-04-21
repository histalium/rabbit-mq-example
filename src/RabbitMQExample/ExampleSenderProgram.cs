using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RabbitMQExample;

public class ExampleSenderProgram : BackgroundService
{
    private readonly ILogger<ExampleSenderProgram> logger;
    private RabbitMQServerOptions rabbitMQServerOptions;

    public ExampleSenderProgram(
        IOptions<RabbitMQServerOptions> rabbitMQServerOptions,
        ILogger<ExampleSenderProgram> logger)
    {
        this.rabbitMQServerOptions = rabbitMQServerOptions.Value;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("start program");
        var (connection, channel) = InitializeRabbitMQ(rabbitMQServerOptions);

        var streamId1 = Guid.NewGuid();
        var message1 = new DescriptionChanged
        {
            Description = "Article 1"
        };

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        logger.LogInformation("Sending message 1");
        SendMessage(message1, streamId1, nameof(DescriptionChanged), channel);

        var message2 = new AmountIncremented
        {
            Increment = 5
        };

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        logger.LogInformation("Sending message 2");
        SendMessage(message2, streamId1, nameof(AmountIncremented), channel);

        var message3 = new AmountDecremented
        {
            Decrement = 2
        };

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        logger.LogInformation("Sending message 3");
        SendMessage(message3, streamId1, nameof(AmountDecremented), channel);

        logger.LogInformation("program ended");
    }

    private (RabbitMQ.Client.IConnection, IModel) InitializeRabbitMQ(RabbitMQServerOptions options)
    {
        logger.LogInformation("start initializing Rabbit MQ");
        
        var factory = new ConnectionFactory
        {
            HostName = options.HostName ?? throw new Exception("HostName is not configured"),
            Port = options.Port ?? throw new Exception("Port is not configured"),
            UserName = options.UserName ?? throw new Exception("UserName is not configured"),
            Password = options.Password ?? throw new Exception("Password is not configured")
        };

        var connection = factory.CreateConnection();

        var channel = connection.CreateModel();
        channel.ExchangeDeclare(exchange: "ExampleExchange", type: ExchangeType.Fanout);

        logger.LogInformation("Rabbit MQ initialized");

        return (connection, channel);
    }

    public void SendMessage<T>(T message, Guid streamId, string EventName, IModel channel)
    {
        var @event = new Event<T>
        {
            StreamId = streamId,
            EventName = EventName,
            Message = message
        };

        SendEvent(@event, channel);
    }

    private void SendEvent<T>(Event<T> @event, IModel channel)
    {
        logger.LogInformation("sending {eventName} event for stream {streamId}", @event.EventName, @event.StreamId);
        logger.LogDebug("sending message {message}", @event.Message);

        var message = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: "ExampleExchange",
            routingKey: string.Empty,
            basicProperties: null,
            body: body
        );

        logger.LogInformation("event sended");
    }
}
