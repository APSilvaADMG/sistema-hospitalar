using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace SistemaHospitalar.Infrastructure.Messaging;

public class HospitalEventPublisher : IDisposable
{
    private readonly ILogger<HospitalEventPublisher> _logger;
    private readonly string? _connectionString;
    private IConnection? _connection;
    private IChannel? _channel;

    public HospitalEventPublisher(IConfiguration configuration, ILogger<HospitalEventPublisher> logger)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("RabbitMQ");
    }

    public async Task PublishAsync(string routingKey, object payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return;
        }

        try
        {
            await EnsureChannelAsync(cancellationToken);
            if (_channel is null)
            {
                return;
            }

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                routingKey,
                timestamp = DateTime.UtcNow,
                data = payload
            }));

            await _channel.BasicPublishAsync(
                exchange: "hospital.events",
                routingKey: routingKey,
                mandatory: false,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Evento publicado: {RoutingKey}", routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao publicar evento RabbitMQ: {RoutingKey}", routingKey);
        }
    }

    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null && _channel.IsOpen)
        {
            return;
        }

        var factory = new ConnectionFactory { Uri = new Uri(_connectionString!) };
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: "hospital.events",
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
