using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniEventsAPI.Models;
using UniEventsAPI.Services;

namespace UniEventsAPI.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AccountService _accounts;
    private readonly EventService _events;

    public AdminController(AccountService accounts, EventService events)
    {
        _accounts = accounts;
        _events = events;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<Account>>> GetUsers(CancellationToken ct)
    {
        return Ok(await _accounts.GetUsers(ct));
    }

    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, UserUpdate request, CancellationToken ct)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _accounts.UpdateUser(id, adminId, request, ct);
        return NoContent();
    }

    [HttpGet("events/pending")]
    public async Task<ActionResult<List<OrganizerEvent>>> GetPendingEvents(CancellationToken ct)
    {
        return Ok(await _events.GetManagedEvents(null, ct));
    }

    [HttpPost("events/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        await _events.Review(id, null, ct);
        return NoContent();
    }

    [HttpPost("events/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RejectRequest request, CancellationToken ct)
    {
        await _events.Review(id, request.Reason, ct);
        return NoContent();
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReport(CancellationToken ct)
    {
        var events = await _events.GetReport(ct);
        return Ok(new { totalEvents = events.Count, totalRegistrations = events.Sum(e => (long)e.Registrations),
            totalAttended = events.Sum(e => (long)e.Attended), events });
    }
}
