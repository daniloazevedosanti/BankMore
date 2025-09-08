using MediatR;
namespace AccountService.Application.Queries;
public record BalanceQuery(string AccountId) : IRequest<Result<object>>;
