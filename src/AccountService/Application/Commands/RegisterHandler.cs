using MediatR;
using Dapper;
using System.Data;
using Shared;
using AccountService.Application.Commands;

namespace AccountService.Application.Commands;
public class RegisterHandler : IRequestHandler<RegisterCommand, Result<int>>
{
    private readonly IDbConnection _db;
    public RegisterHandler(IDbConnection db) { _db = db; }
    public Task<Result<int>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (!CpfValidator.IsValid(request.Cpf)) return Task.FromResult(Result<int>.Fail("CPF inválido.", ErrorTypes.INVALID_DOCUMENT));
        var id = Guid.NewGuid().ToString();
        var numero = _db.ExecuteScalar<int?>("select max(numero)+1 from contacorrente") ?? 1000;
        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        var hash = BCrypt.Net.BCrypt.HashPassword(request.Senha + salt);
        _db.Execute(@"insert into contacorrente (idcontacorrente, numero, nome, ativo, senha, salt) values (@id,@numero,@nome,1,@senha,@salt)", new { id, numero, nome = request.Nome, senha = hash, salt });
        return Task.FromResult(Result<int>.Ok(numero));
    }
}
