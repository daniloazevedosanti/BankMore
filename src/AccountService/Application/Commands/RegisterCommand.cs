using MediatR;
namespace AccountService.Application.Commands;
public record RegisterCommand(string Nome, string Cpf, string Senha) : IRequest<Result<int>>;
