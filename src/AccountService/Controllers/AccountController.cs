using Microsoft.AspNetCore.Mvc;
using MediatR;
using Shared;
using AccountService.Application.Commands;
using AccountService.Application.Queries;

namespace AccountService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;
    public AccountController(IMediator mediator) { _mediator = mediator; }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        if (!result.Success) return BadRequest(new { message = result.Message, type = result.Type });
        return Ok(new { numero = result.Data });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginQuery query)
    {
        var result = await _mediator.Send(query);
        if (!result.Success) return Unauthorized(new { message = result.Message, type = result.Type });
        return Ok(new { token = result.Data });
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate([FromBody] DeactivateCommand cmd)
    {
        var res = await _mediator.Send(cmd);
        if (!res.Success) return BadRequest(new { message = res.Message, type = res.Type });
        return NoContent();
    }

    [HttpPost("movement")]
    public async Task<IActionResult> Movement([FromBody] MovementCommand cmd)
    {
        var res = await _mediator.Send(cmd);
        if (!res.Success) return BadRequest(new { message = res.Message, type = res.Type });
        return NoContent();
    }

    [HttpGet("balance")]
    public async Task<IActionResult> Balance([FromQuery] BalanceQuery q)
    {
        var res = await _mediator.Send(q);
        if (!res.Success) return BadRequest(new { message = res.Message, type = res.Type });
        return Ok(res.Data);
    }
}
