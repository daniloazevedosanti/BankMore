using MediatR;
using Dapper;
using System.Data;
using Shared;

namespace AccountService.Application.Commands;
public class DeactivateHandler : IRequestHandler<DeactivateCommand, Result<Unit>>
{
    private readonly IDbConnection _db;
    public DeactivateHandler(IDbConnection db) { _db = db; }
    public Task<Result<Unit>> Handle(DeactivateCommand request, CancellationToken cancellationToken)
    {
        var acc = _db.QueryFirstOrDefault<dynamic>("select * from contacorrente where idcontacorrente = @id", new { id = request.AccountId });
        if (acc is null) return Task.FromResult(Result<Unit>.Fail("Conta inválida.", ErrorTypes.INVALID_ACCOUNT));
        bool ok = BCrypt.Net.BCrypt.Verify(request.Senha + (string)acc.salt, (string)acc.senha);
        if (!ok) return Task.FromResult(Result<Unit>.Fail("Senha inválida.", ErrorTypes.USER_UNAUTHORIZED));
        _db.Execute("update contacorrente set ativo = 0 where idcontacorrente = @id", new { id = request.AccountId });
        return Task.FromResult(Result<Unit>.Ok(Unit.Value));
    }
}
