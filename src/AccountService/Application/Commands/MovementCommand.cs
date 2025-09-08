using MediatR;
namespace AccountService.Application.Commands;
public record MovementCommand(string? IdRequisicao, string? IdContaCorrente, string Tipo, double Valor, string TokenAccountId) : IRequest<Result<Unit>>;
