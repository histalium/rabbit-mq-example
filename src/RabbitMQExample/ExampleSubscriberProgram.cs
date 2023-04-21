using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQExample;

public class ExampleSubscriberProgram : BackgroundService
{
    private readonly ILogger<ExampleSubscriberProgram> logger;
    private readonly IServiceProvider services;
    private RabbitMQServerOptions rabbitMQServerOptions;

    public ExampleSubscriberProgram(
        IOptions<RabbitMQServerOptions> rabbitMQServerOptions,
        ILogger<ExampleSubscriberProgram> logger,
        IServiceProvider services)
    {
        this.rabbitMQServerOptions = rabbitMQServerOptions.Value;
        this.logger = logger;
        this.services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Start subscriber");

        var (connection, channel) = InitializeRabbitMQ(rabbitMQServerOptions);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (moduleHandle, ea) =>
        {
            using var scope = services.CreateScope();

            var body = ea.Body;
            var eventJson = Encoding.UTF8.GetString(body.ToArray());
            var eventName = JsonSerializer.Deserialize<EventWithOnlyName>(eventJson);

            if (eventName is null)
            {
                return;
            }

            switch (eventName.EventName)
            {
                case nameof(DescriptionChanged):
                    var descriptionChanged = JsonSerializer.Deserialize<Event<DescriptionChanged>>(eventJson);
                    logger.LogInformation("Description changed to \"{description}\" for stream with id {streamId}", descriptionChanged?.Message?.Description, descriptionChanged?.StreamId);
                    break;
                case nameof(AmountIncremented):
                    var amountIncremented = JsonSerializer.Deserialize<Event<AmountIncremented>>(eventJson);
                    logger.LogInformation("Amount incremented with {increment} for stream with id {streamId}", amountIncremented?.Message?.Increment, amountIncremented?.StreamId);
                    break;
                case nameof(AmountDecremented):
                    var amountDecremented = JsonSerializer.Deserialize<Event<AmountDecremented>>(eventJson);
                    logger.LogInformation("Description decremented with {decrement} for stream with id {streamId}", amountDecremented?.Message?.Decrement, amountDecremented?.StreamId);
                    break;
            }
        };

        channel.BasicConsume(
            queue: "ExampleExchangeSubscriber",
            autoAck: true,
            consumer: consumer);

        await stoppingToken.WaitTillCanceled();
        logger.LogInformation("Subscriber ended");
    }

    private (IConnection, IModel) InitializeRabbitMQ(RabbitMQServerOptions options)
    {
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

        channel.QueueDeclare("ExampleExchangeSubscriber", durable: true, autoDelete: false, exclusive: false);
        channel.QueueBind(
            queue: "ExampleExchangeSubscriber",
            exchange: "ExampleExchange",
            routingKey: "");

        connection.ConnectionShutdown += (sender, e) =>
        {
            logger.LogInformation("Rabbit MQ connection shutdown");
        };

        return (connection, channel);
    }
}

public class ExampleSubscriberProgram1 : ExampleSubscriberProgram
{
    public ExampleSubscriberProgram1(IOptions<RabbitMQServerOptions> rabbitMQServerOptions, ILogger<ExampleSubscriberProgram1> logger, IServiceProvider services) : base(rabbitMQServerOptions, logger, services)
    {
    }
}

public class ExampleSubscriberProgram2 : ExampleSubscriberProgram
{
    public ExampleSubscriberProgram2(IOptions<RabbitMQServerOptions> rabbitMQServerOptions, ILogger<ExampleSubscriberProgram2> logger, IServiceProvider services) : base(rabbitMQServerOptions, logger, services)
    {
    }
}
