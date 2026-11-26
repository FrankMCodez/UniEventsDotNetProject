using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniEventsAPI.Models;
using UniEventsAPI.Services;

namespace UniEventsAPI.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly EventService _events;
    private readonly RegistrationService _registrations;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public EventsController(EventService events, RegistrationService registrations)
    {
        _events = events;
        _registrations = registrations;
    }

    [HttpGet]
    public async Task<ActionResult<List<EventDetails>>> GetAll(string? keyword, int? categoryId, DateOnly? date,
        CancellationToken ct)
    {
        return Ok(await _events.GetFeed(keyword, categoryId, date, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDetails>> GetDetails(int id, CancellationToken ct)
    {
        return Ok(await _events.GetDetails(id, ct));
    }

    [Authorize(Roles = "Student")]
    [HttpGet("my-events")]
    public async Task<ActionResult<List<EventDetails>>> GetMyEvents(CancellationToken ct)
    {
        return Ok(await _registrations.GetMyEvents(UserId, ct));
    }

    [Authorize(Roles = "Student")]
    [HttpPost("{id:int}/join")]
    public async Task<IActionResult> Join(int id, CancellationToken ct)
    {
        await _registrations.Join(id, UserId, ct);
        return NoContent();
    }

    [Authorize(Roles = "Student")]
    [HttpPost("{id:int}/leave")]
    public async Task<IActionResult> Leave(int id, CancellationToken ct)
    {
        await _registrations.Leave(id, UserId, ct);
        return NoContent();
    }

    [Authorize(Roles = "Organizer")]
    [HttpGet("organized")]
    public async Task<ActionResult<List<OrganizerEvent>>> GetOrganizedEvents(CancellationToken ct)
    {
        return Ok(await _events.GetManagedEvents(UserId, ct));
    }

    [Authorize(Roles = "Organizer")]
    [HttpPost]
    public async Task<IActionResult> Create(EventRequest request, CancellationToken ct)
    {
        int id = await _events.Create(request, UserId, ct);
        return StatusCode(StatusCodes.Status201Created, new { id, status = "Pending" });
    }

    [Authorize(Roles = "Organizer")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventRequest request, CancellationToken ct)
    {
        await _events.Update(id, UserId, request, ct);
        return NoContent();
    }

    [Authorize(Roles = "Organizer")]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await _events.Cancel(id, UserId, ct);
        return NoContent();
    }

    [Authorize(Roles = "Organizer")]
    [HttpGet("{id:int}/attendees")]
    public async Task<IActionResult> GetAttendees(int id, CancellationToken ct)
    {
        var attendees = await _registrations.GetAttendees(id, UserId, ct);
        return Ok(new { count = attendees.Count, attendees });
    }

    [Authorize(Roles = "Organizer")]
    [HttpPost("{id:int}/attendees/{userId:int}/check-in")]
    public async Task<IActionResult> CheckIn(int id, int userId, CancellationToken ct)
    {
        await _registrations.CheckIn(id, UserId, userId, ct);
        return NoContent();
    }
}
