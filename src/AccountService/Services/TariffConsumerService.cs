using System.Text.Json;
using Confluent.Kafka;
using Dapper;
using Polly;
using System.Data;

namespace AccountService.Services;
public class TariffConsumerService : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _config;
    private readonly ILogger<TariffConsumerService> _logger;
    public TariffConsumerService(IServiceProvider provider, IConfiguration config, ILogger<TariffConsumerService> logger)
    {
        _provider = provider; _config = config; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrap = _config.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092";
        var cfg = new ConsumerConfig { BootstrapServers = bootstrap, GroupId = "account-service-tariff-group", AutoOffsetReset = AutoOffsetReset.Earliest };
        using var consumer = new ConsumerBuilder<string, string>(cfg).Build();
        consumer.Subscribe("tariffs");

        var policy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) }, (ex, ts) =>
            {
                _logger.LogWarning(ex, "Tentando novamente o processamento de tarifas após {Delay}", ts);
            });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                if (cr?.Message == null) continue;
                await policy.ExecuteAsync(async () =>
                {
                    var msg = JsonSerializer.Deserialize<JsonElement>(cr.Message.Value);
                    string idConta = msg.GetProperty("IdConta").GetString() ?? "";
                    double valor = msg.GetProperty("Valor").GetDouble();
                    using var scope = _provider.CreateScope();
                    var db = (IDbConnection)scope.ServiceProvider.GetRequiredService(typeof(IDbConnection));
                    await db.ExecuteAsync(@"insert into movimento (idmovimento, idcontacorrente, datamovimento, tipomovimento, valor) values (@id,@conta,@dt,'D',@valor)",
                        new { id = Guid.NewGuid().ToString(), conta = idConta, dt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"), valor });
                });
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Erro ao consumir mensagem de tarifa"); }
        }
    }
}
