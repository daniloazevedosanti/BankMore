using System.Text.Json;
using Confluent.Kafka;
using Dapper;
using Polly;
using System.Data;

namespace TariffService.Services;
public class TransferConsumerService : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _config;
    private readonly ILogger<TransferConsumerService> _logger;
    public TransferConsumerService(IServiceProvider provider, IConfiguration config, ILogger<TransferConsumerService> logger)
    {
        _provider = provider; _config = config; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrap = _config.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092";
        var cfg = new ConsumerConfig { BootstrapServers = bootstrap, GroupId = "tariff-service-group", AutoOffsetReset = AutoOffsetReset.Earliest };
        using var consumer = new ConsumerBuilder<string, string>(cfg).Build();
        consumer.Subscribe("transfers");

        var policy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) }, (ex, ts) => _logger.LogWarning(ex, "Tentando novamente..."));

        using var producer = new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = bootstrap }).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                if (cr?.Message == null) continue;
                await policy.ExecuteAsync(async () =>
                {
                    var msg = JsonSerializer.Deserialize<JsonElement>(cr.Message.Value);
                    string idTransfer = msg.GetProperty("IdTransferencia").GetString() ?? Guid.NewGuid().ToString();
                    string idConta = msg.GetProperty("IdContaOrigem").GetString() ?? "";
                    double valor = msg.GetProperty("Valor").GetDouble();
                    using var scope = _provider.CreateScope();
                    var db = (IDbConnection)scope.ServiceProvider.GetRequiredService(typeof(IDbConnection));
                    await db.ExecuteAsync(@"insert into tarifa (idtarifa, idcontacorrente, datamovimento, valor) values (@id,@conta,@dt,@valor)", new { id = Guid.NewGuid().ToString(), conta = idConta, dt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"), valor = _config.GetValue<double>("Tariff:Value", 2.0) });

                    var evt = JsonSerializer.Serialize(new { IdConta = idConta, Valor = _config.GetValue<double>("Tariff:Value", 2.0), IdRequisicao = idTransfer });
                    await producer.ProduceAsync("tariffs", new Message<string, string> { Key = idTransfer, Value = evt });
                    producer.Flush(TimeSpan.FromSeconds(1));
                });
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Erro na transferência do consumer"); }
        }
    }
}
