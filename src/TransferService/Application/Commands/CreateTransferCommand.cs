using AccountService;
using MediatR;
namespace TransferService.Application.Commands;
public record CreateTransferCommand(string? IdRequisicao, string IdContaOrigem, string IdContaDestino, double Valor, string Token) : IRequest<Result<Unit>>;
