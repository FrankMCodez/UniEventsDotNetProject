using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UniEventsAPI.Data;
using UniEventsAPI.Models;

namespace UniEventsAPI.Services;

public class EventService
{
    private readonly ApiContext _context;

    public EventService(ApiContext context)
    {
        _context = context;
    }

    public async Task<List<EventDetails>> GetFeed(string? keyword, int? categoryId, DateOnly? date, CancellationToken ct)
    {
        var now = DateTime.UtcNow.AddHours(2);
        var today = DateOnly.FromDateTime(now);
        var localTime = TimeOnly.FromDateTime(now);
        var events = _context.Events.Where(item => item.Status == "Approved" &&
            (item.EventDate > today || (item.EventDate == today && item.StartTime > localTime)));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            events = events.Where(item => item.Title.Contains(keyword) || item.Description.Contains(keyword));
        }
        if (categoryId.HasValue)
            events = events.Where(item => item.CategoryId == categoryId.Value);
        if (date.HasValue)
            events = events.Where(item => item.EventDate == date.Value);

        var rows = await EventRows(events).ToListAsync(ct);
        return rows.Select(row => row.Event).ToList();
    }

    public async Task<EventDetails> GetDetails(int id, CancellationToken ct)
    {
        var row = await EventRows(_context.Events.Where(item => item.Id == id && item.Status == "Approved"))
            .FirstOrDefaultAsync(ct);
        return row?.Event ?? throw new ApiError(404, "Event was not found.");
    }

    public Task<List<OrganizerEvent>> GetManagedEvents(int? organizerId, CancellationToken ct)
    {
        var events = organizerId.HasValue
            ? _context.Events.Where(item => item.OrganizerId == organizerId.Value)
            : _context.Events.Where(item => item.Status == "Pending");

        return EventRows(events).ToListAsync(ct);
    }

    public async Task<int> Create(EventRequest request, int organizerId, CancellationToken ct)
    {
        var item = new Event { OrganizerId = organizerId };
        ApplyDetails(item, request);
        _context.Events.Add(item);
        await SaveEvent(ct);
        return item.Id;
    }

    public Task Update(int id, int organizerId, EventRequest request, CancellationToken ct)
        => ChangeEvent(id, async item =>
        {
            CheckOwner(item, organizerId);
            if (item.Status == "Cancelled" || HasStarted(item))
                throw new ApiError(400, "This event can no longer be edited.");
            if (request.Capacity < await CountRegistrations(id, ct))
                throw new ApiError(400, "Capacity cannot be below the current registration count.");

            ApplyDetails(item, request);
            item.Status = "Pending";
            item.RejectReason = null;
        }, ct);

    public Task Cancel(int id, int organizerId, CancellationToken ct)
        => ChangeEvent(id, item =>
        {
            CheckOwner(item, organizerId);
            if (item.Status != "Cancelled" && HasStarted(item))
                throw new ApiError(400, "This event has already started.");

            item.Status = "Cancelled";
            return Task.CompletedTask;
        }, ct);

    public Task Review(int id, string? reason, CancellationToken ct)
        => ChangeEvent(id, item =>
        {
            if (item.Status != "Pending")
                throw new ApiError(400, "Only pending events can be reviewed.");
            if (reason is null && HasStarted(item))
                throw new ApiError(400, "This event has already started.");

            item.Status = reason is null ? "Approved" : "Rejected";
            item.RejectReason = reason?.Trim();
            return Task.CompletedTask;
        }, ct);

    public async Task<List<EventReport>> GetReport(CancellationToken ct)
    {
        var counts = await _context.Events.OrderBy(item => item.Id).Select(item => new
        {
            item.Id,
            item.Title,
            item.Status,
            Registrations = item.Registrations.Count(registration => registration.Status == "Registered"),
            Attended = item.Registrations.Count(registration =>
                registration.Status == "Registered" && registration.CheckInTime.HasValue)
        }).ToListAsync(ct);

        return counts.Select(row => new EventReport(row.Id, row.Title, row.Status, row.Registrations, row.Attended,
            row.Registrations == 0 ? 0 : Math.Round(100m * row.Attended / row.Registrations, 2))).ToList();
    }

    internal async Task ChangeEvent(int id, Func<Event, Task> change, CancellationToken ct)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var lockName = $"UniEvents:Event:{id}";

        // A student must wait for another write to the same event before checking capacity.
        try
        {
            await _context.Database.ExecuteSqlAsync($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource = {lockName}, @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction', @LockTimeout = 5000;
                IF @result < 0 THROW 50001, 'This event is busy.', 1;
                """, ct);
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
            throw new ApiError(503, "This event is busy. Try again.");
        }

        var item = await FindEvent(id, ct);
        await change(item);
        await SaveEvent(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task SaveEvent(CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            throw new ApiError(400, "Choose an existing category and a valid capacity.");
        }
    }

    internal async Task<Event> FindEvent(int id, CancellationToken ct)
        => await _context.Events.FindAsync([id], ct) ?? throw new ApiError(404, "Event was not found.");

    internal Task<int> CountRegistrations(int eventId, CancellationToken ct)
        => _context.Registrations.CountAsync(registration =>
            registration.EventId == eventId && registration.Status == "Registered", ct);

    internal static void CheckOwner(Event item, int organizerId)
    {
        if (item.OrganizerId != organizerId)
            throw new ApiError(403, "You can only manage your own events.");
    }

    internal static bool HasStarted(Event item)
        => item.EventDate.ToDateTime(item.StartTime) <= DateTime.UtcNow.AddHours(2);

    private static void ApplyDetails(Event item, EventRequest request)
    {
        var time = request.StartTime!.Value;
        item.Title = request.Title.Trim();
        item.Description = request.Description.Trim();
        item.EventDate = request.EventDate!.Value;
        item.StartTime = new TimeOnly(time.Hour, time.Minute, time.Second);
        item.Venue = request.Venue.Trim();
        item.Capacity = request.Capacity;
        item.CategoryId = request.CategoryId;

        if (HasStarted(item))
            throw new ApiError(400, "The event must start in the future.");
    }

    internal static IQueryable<OrganizerEvent> EventRows(IQueryable<Event> events)
        => events.OrderBy(item => item.EventDate).ThenBy(item => item.StartTime).ThenBy(item => item.Id)
            .Select(item => new OrganizerEvent(
                new EventDetails(item.Id, item.Title, item.Description, item.EventDate, item.StartTime, item.Venue,
                    item.Capacity, item.CategoryId, item.Category.Name, item.Organizer.FullName, item.Status,
                    item.Capacity - item.Registrations.Count(registration => registration.Status == "Registered")),
                item.RejectReason));
}

