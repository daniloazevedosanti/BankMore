using MediatR;
namespace AccountService.Application.Commands;
public record DeactivateCommand(string Senha, string AccountId) : IRequest<Result<Unit>>;
