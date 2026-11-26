using Microsoft.AspNetCore.Mvc;
using UniEventsAPI.Models;
using UniEventsAPI.Services;

namespace UniEventsAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AccountService _accounts;

    public AuthController(AccountService accounts)
    {
        _accounts = accounts;
    }

    [HttpPost("register")]
    public async Task<ActionResult<Account>> Register(RegisterRequest request, CancellationToken ct)
    {
        var account = await _accounts.Register(request, ct);
        return StatusCode(StatusCodes.Status201Created, account);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        return Ok(await _accounts.Login(request, ct));
    }
}
