using MediatR;
using Dapper;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Shared;
using AccountService.Application.Queries;

namespace AccountService.Application.Queries;
public class LoginHandler : IRequestHandler<LoginQuery, Result<string>>
{
    private readonly IDbConnection _db;
    private readonly IConfiguration _config;
    public LoginHandler(IDbConnection db, IConfiguration config) { _db = db; _config = config; }
    public Task<Result<string>> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        var acc = _db.QueryFirstOrDefault<dynamic>("select * from contacorrente where numero = @numero or nome = @cpf", new { numero = request.Numero, cpf = request.Cpf });
        if (acc is null) return Task.FromResult(Result<string>.Fail("Usuário não encontrado.", ErrorTypes.USER_UNAUTHORIZED));
        bool ok = BCrypt.Net.BCrypt.Verify(request.Senha + (string)acc.salt, (string)acc.senha);
        if (!ok) return Task.FromResult(Result<string>.Fail("Senha inválida.", ErrorTypes.USER_UNAUTHORIZED));

        var jwt = _config.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings("BankMore","BankMore.Users","CHANGE_ME",240);
        var claims = new[] { new Claim("accountId", (string)acc.idcontacorrente), new Claim("accountNumber", ((long)acc.numero).ToString()) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer: jwt.Issuer, audience: jwt.Audience, claims: claims, expires: DateTime.UtcNow.AddMinutes(jwt.ExpirationMinutes), signingCredentials: creds);
        return Task.FromResult(Result<string>.Ok(new JwtSecurityTokenHandler().WriteToken(token)));
    }
}
