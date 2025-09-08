using MediatR;
using Dapper;
using System.Data;
using Confluent.Kafka;
using System.Text.Json;
using Shared;
using AccountService;

namespace TransferService.Application.Commands;
public class CreateTransferHandler : IRequestHandler<CreateTransferCommand, Result<Unit>>
{
    private readonly IDbConnection _db;
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    public CreateTransferHandler(IDbConnection db, IHttpClientFactory http, IConfiguration config) { _db = db; _http = http; _config = config; }

    public async Task<Result<Unit>> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        if (request.Valor <= 0) return Result<Unit>.Fail("Valor inválido.", Shared.ErrorTypes.INVALID_VALUE);
        
        await _db.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS idempotencia (chave_idempotencia TEXT PRIMARY KEY, requisicao TEXT, resultado TEXT);", new { });
        if (!string.IsNullOrWhiteSpace(request.IdRequisicao))
        {
            var idem = _db.QueryFirstOrDefault<dynamic>("select * from idempotencia where chave_idempotencia=@k", new { k = request.IdRequisicao });
            if (idem is not null) return Result<Unit>.Ok(Unit.Value);
            await _db.ExecuteAsync("insert into idempotencia (chave_idempotencia, requisicao, resultado) values (@k,@r,@s)", new { k = request.IdRequisicao, r = "transfer", s = "204" });
        }

        var client = _http.CreateClient("AccountService");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.Token.Replace("Bearer ", ""));

        var debit = new { IdRequisicao = request.IdRequisicao, IdContaCorrente = (string?)null, Tipo = "D", Valor = request.Valor };
        var respDebit = await client.PostAsJsonAsync("api/account/movement", debit);
        if (!respDebit.IsSuccessStatusCode) { var body = await respDebit.Content.ReadAsStringAsync(); return Result<Unit>.Fail(body, "TRANSFER_ERROR"); }

        var credit = new { IdRequisicao = request.IdRequisicao + "-credit", IdContaCorrente = request.IdContaDestino, Tipo = "C", Valor = request.Valor };
        var respCredit = await client.PostAsJsonAsync("api/account/movement", credit);
        if (!respCredit.IsSuccessStatusCode)
        {
            
            var rollback = new { IdRequisicao = request.IdRequisicao + "-rollback", IdContaCorrente = (string?)null, Tipo = "C", Valor = request.Valor };
            await client.PostAsJsonAsync("api/account/movement", rollback);
            var body = await respCredit.Content.ReadAsStringAsync();
            return Result<Unit>.Fail(body, "TRANSFER_ERROR");
        }

        var transferId = Guid.NewGuid().ToString();
        await _db.ExecuteAsync(@"insert into transferencia (idtransferencia, idcontacorrente_origem, idcontacorrente_destino, datamovimento, valor) values (@id,@o,@d,@dt,@v)", new { id = transferId, o = request.IdContaOrigem, d = request.IdContaDestino, dt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"), v = request.Valor });

        // produce to Kafka
        try
        {
            var producerCfg = new ProducerConfig { BootstrapServers = _config.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092" };
            using var producer = new ProducerBuilder<string, string>(producerCfg).Build();
            var evt = JsonSerializer.Serialize(new { IdTransferencia = transferId, IdContaOrigem = request.IdContaOrigem, IdContaDestino = request.IdContaDestino, Valor = request.Valor, IdRequisicao = request.IdRequisicao });
            await producer.ProduceAsync("transfers", new Message<string, string> { Key = request.IdRequisicao ?? transferId, Value = evt });
            producer.Flush(TimeSpan.FromSeconds(5));
        }
        catch { /* log */ }

        return Result<Unit>.Ok(Unit.Value);
    }
}
