using MediatR;
using Dapper;
using System.Data;
using Shared;

namespace AccountService.Application.Commands;
public class MovementHandler : IRequestHandler<MovementCommand, Result<Unit>>
{
    private readonly IDbConnection _db;
    public MovementHandler(IDbConnection db) { _db = db; }
    public Task<Result<Unit>> Handle(MovementCommand request, CancellationToken cancellationToken)
    {
        var tokenAccountId = request.TokenAccountId;
        var accountId = string.IsNullOrWhiteSpace(request.IdContaCorrente) ? tokenAccountId : request.IdContaCorrente;

        var acc = _db.QueryFirstOrDefault<dynamic>("select * from contacorrente where idcontacorrente = @id", new { id = accountId });
        if (acc is null) return Task.FromResult(Result<Unit>.Fail("Conta inválida.", ErrorTypes.INVALID_ACCOUNT));
        if ((long)acc.ativo == 0) return Task.FromResult(Result<Unit>.Fail("Conta inativa.", ErrorTypes.INACTIVE_ACCOUNT));
        if (request.Valor <= 0) return Task.FromResult(Result<Unit>.Fail("Valor inválido.", ErrorTypes.INVALID_VALUE));
        if (request.Tipo != "C" && request.Tipo != "D") return Task.FromResult(Result<Unit>.Fail("Tipo inválido.", ErrorTypes.INVALID_TYPE));
        if (accountId != tokenAccountId && request.Tipo == "D") return Task.FromResult(Result<Unit>.Fail("Somente crédito permitido para terceiros.", ErrorTypes.INVALID_TYPE));

        if (!string.IsNullOrWhiteSpace(request.IdRequisicao))
        {
            _db.Execute(@"CREATE TABLE IF NOT EXISTS idempotencia (chave_idempotencia TEXT PRIMARY KEY, requisicao TEXT, resultado TEXT);");
            var idem = _db.QueryFirstOrDefault<dynamic>("select * from idempotencia where chave_idempotencia=@k", new { k = request.IdRequisicao });
            if (idem is not null) return Task.FromResult(Result<Unit>.Ok(Unit.Value));
            _db.Execute("insert into idempotencia (chave_idempotencia, requisicao, resultado) values (@k,@r,@s)", new { k = request.IdRequisicao, r = "movement", s = "204" });
        }

        _db.Execute(@"insert into movimento (idmovimento, idcontacorrente, datamovimento, tipomovimento, valor) values (@id,@acc,@dt,@tipo,@valor)", new { id = Guid.NewGuid().ToString(), acc = accountId, dt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"), tipo = request.Tipo, valor = request.Valor });
        return Task.FromResult(Result<Unit>.Ok(Unit.Value));
    }
}
