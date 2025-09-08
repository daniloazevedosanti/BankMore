using MediatR;
namespace AccountService.Application.Queries;
public record LoginQuery(int? Numero, string? Cpf, string Senha) : IRequest<Result<string>>;
