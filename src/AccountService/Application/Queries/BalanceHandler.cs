using MediatR;
using Dapper;
using System.Data;
using Shared;

namespace AccountService.Application.Queries;
public class BalanceHandler : IRequestHandler<BalanceQuery, Result<object>>
{
    private readonly IDbConnection _db;
    public BalanceHandler(IDbConnection db) { _db = db; }
    public Task<Result<object>> Handle(BalanceQuery request, CancellationToken cancellationToken)
    {
        var id = request.AccountId;
        var acc = _db.QueryFirstOrDefault<dynamic>("select * from contacorrente where idcontacorrente = @id", new { id });
        if (acc is null) return Task.FromResult(Result<object>.Fail("Conta inválida.", ErrorTypes.INVALID_ACCOUNT));
        if ((long)acc.ativo == 0) return Task.FromResult(Result<object>.Fail("Conta inativa.", ErrorTypes.INACTIVE_ACCOUNT));
        var credit = _db.ExecuteScalar<double?>("select coalesce(sum(valor),0) from movimento where idcontacorrente=@id and tipomovimento='C'", new { id }) ?? 0.0;
        var debit  = _db.ExecuteScalar<double?>("select coalesce(sum(valor),0) from movimento where idcontacorrente=@id and tipomovimento='D'", new { id }) ?? 0.0;
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var dto = new { numero = (int)(long)acc.numero, titular = (string)acc.nome, datahora = now, saldo = Math.Round(credit - debit, 2) };
        return Task.FromResult(Result<object>.Ok(dto));
    }
}
