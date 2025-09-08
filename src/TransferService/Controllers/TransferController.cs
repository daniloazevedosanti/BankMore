using Microsoft.AspNetCore.Mvc;
using MediatR;
using TransferService.Application.Commands;

namespace TransferService.Controllers;
[ApiController]
[Route("api/[controller]")]
public class TransferController : ControllerBase
{
    private readonly IMediator _mediator;
    public TransferController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateTransferCommand cmd)
    {
        var res = await _mediator.Send(cmd);
        if (!res.Success) return BadRequest(new { message = res.Message, type = res.Type });
        return NoContent();
    }
}
