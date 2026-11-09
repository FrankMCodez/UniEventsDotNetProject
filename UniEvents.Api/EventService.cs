using System.Collections.Concurrent;

namespace UniEvents.Api;

public class EventService
{
    private readonly ConcurrentDictionary<Guid, PublishedEvent> _events = new();
    private readonly ConcurrentDictionary<string, AttendanceRecord> _tickets = new(StringComparer.Ordinal);

    public PublishedEvent CreateEvent(CreateEventRequest request)
    {
        var createdEvent = new PublishedEvent(
            Guid.NewGuid(),
            request.Title,
            request.PublishedBy,
            request.StartsAtUtc,
            request.Location);

        _events[createdEvent.Id] = createdEvent;
        return createdEvent;
    }

    public IReadOnlyCollection<PublishedEvent> GetEvents()
    {
        return _events.Values.OrderBy(e => e.StartsAtUtc).ToArray();
    }

    public TicketResponse? RegisterStudent(Guid eventId, RegisterStudentRequest request)
    {
        if (!_events.TryGetValue(eventId, out var eventDetails))
        {
            return null;
        }

        var ticketCode = Guid.NewGuid().ToString("N");
        var qrCode = $"UNI-EVENT:{ticketCode}";

        var attendance = new AttendanceRecord(
            eventDetails.Id,
            eventDetails.Title,
            request.StudentId,
            request.StudentName,
            ticketCode,
            qrCode,
            CheckedInAtUtc: null);

        _tickets[ticketCode] = attendance;
        return new TicketResponse(eventDetails.Id, eventDetails.Title, ticketCode, qrCode);
    }

    public CheckInResult CheckIn(string scannedCode)
    {
        var ticketCode = scannedCode.StartsWith("UNI-EVENT:", StringComparison.Ordinal)
            ? scannedCode["UNI-EVENT:".Length..]
            : scannedCode;

        if (!_tickets.TryGetValue(ticketCode, out var attendance))
        {
            return new CheckInResult.InvalidTicket();
        }

        if (attendance.CheckedInAtUtc is not null)
        {
            return new CheckInResult.AlreadyCheckedIn();
        }

        var checkedInAttendance = attendance with { CheckedInAtUtc = DateTimeOffset.UtcNow };
        _tickets[ticketCode] = checkedInAttendance;
        return new CheckInResult.Success(checkedInAttendance);
    }
}
