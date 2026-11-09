namespace UniEvents.Api;

public record CreateEventRequest(
    string Title,
    string PublishedBy,
    DateTimeOffset StartsAtUtc,
    string Location);

public record PublishedEvent(
    Guid Id,
    string Title,
    string PublishedBy,
    DateTimeOffset StartsAtUtc,
    string Location);

public record RegisterStudentRequest(
    string StudentId,
    string StudentName);

public record TicketResponse(
    Guid EventId,
    string EventTitle,
    string TicketCode,
    string QrCode);

public record CheckInRequest(string ScannedCode);

public record AttendanceRecord(
    Guid EventId,
    string EventTitle,
    string StudentId,
    string StudentName,
    string TicketCode,
    string QrCode,
    DateTimeOffset? CheckedInAtUtc);

public abstract record CheckInResult
{
    public sealed record Success(AttendanceRecord Attendance) : CheckInResult;
    public sealed record InvalidTicket : CheckInResult;
    public sealed record AlreadyCheckedIn : CheckInResult;
}
