using Microsoft.EntityFrameworkCore;
using UniEventsAPI.Data;
using UniEventsAPI.Models;

namespace UniEventsAPI.Services;

public class RegistrationService
{
    private readonly ApiContext _context;
    private readonly EventService _events;

    public RegistrationService(ApiContext context, EventService events)
    {
        _context = context;
        _events = events;
    }

    public async Task<List<EventDetails>> GetMyEvents(int userId, CancellationToken ct)
    {
        var events = _context.Events.Where(item =>
            item.Registrations.Any(registration => registration.UserId == userId && registration.Status == "Registered"));
        var rows = await EventService.EventRows(events).ToListAsync(ct);
        return rows.Select(row => row.Event).ToList();
    }

    public Task Join(int id, int userId, CancellationToken ct)
        => _events.ChangeEvent(id, async item =>
        {
            if (item.Status != "Approved" || EventService.HasStarted(item))
                throw new ApiError(400, "This event is closed for registration.");

            var registration = await FindRegistration(id, userId, ct);
            if (registration?.Status == "Registered")
                throw new ApiError(400, "You have already joined this event.");
            if (await _events.CountRegistrations(id, ct) >= item.Capacity)
                throw new ApiError(400, "This event is full.");

            if (registration is null)
            {
                registration = new Registration { EventId = id, UserId = userId };
                _context.Registrations.Add(registration);
            }
            registration.Status = "Registered";
            registration.RegisteredAt = DateTimeOffset.UtcNow;
            registration.CheckInTime = null;
        }, ct);

    public Task Leave(int id, int userId, CancellationToken ct)
        => _events.ChangeEvent(id, async item =>
        {
            var registration = await FindRegistration(item.Id, userId, ct);
            if (registration?.Status != "Registered" || registration.CheckInTime.HasValue)
                throw new ApiError(400, "Only an active registration without a check-in can be cancelled.");

            registration.Status = "Cancelled";
        }, ct);

    public async Task<List<Attendee>> GetAttendees(int id, int organizerId, CancellationToken ct)
    {
        var item = await _events.FindEvent(id, ct);
        EventService.CheckOwner(item, organizerId);

        return await _context.Registrations
            .Where(registration => registration.EventId == id && registration.Status == "Registered")
            .OrderBy(registration => registration.User.FullName).ThenBy(registration => registration.UserId)
            .Select(registration => new Attendee(registration.UserId, registration.User.FullName,
                registration.RegisteredAt, registration.CheckInTime))
            .ToListAsync(ct);
    }

    public Task CheckIn(int id, int organizerId, int userId, CancellationToken ct)
        => _events.ChangeEvent(id, async item =>
        {
            EventService.CheckOwner(item, organizerId);
            if (item.Status != "Approved")
                throw new ApiError(400, "Check-in is available for approved events.");

            var registration = await FindRegistration(id, userId, ct);
            if (registration?.Status != "Registered")
                throw new ApiError(404, "Active registration was not found.");

            registration.CheckInTime ??= DateTimeOffset.UtcNow;
        }, ct);

    private Task<Registration?> FindRegistration(int eventId, int userId, CancellationToken ct)
        => _context.Registrations.SingleOrDefaultAsync(registration =>
            registration.EventId == eventId && registration.UserId == userId, ct);
}
