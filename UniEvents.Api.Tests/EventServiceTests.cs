using UniEvents.Api;

namespace UniEvents.Api.Tests;

public class EventServiceTests
{
    [Fact]
    public void RegisterStudent_ForExistingEvent_ReturnsTicketWithQrCode()
    {
        var service = new EventService();
        var createdEvent = service.CreateEvent(new CreateEventRequest(
            "Robotics Workshop",
            "Engineering Club",
            DateTimeOffset.UtcNow.AddDays(2),
            "Lab A"));

        var ticket = service.RegisterStudent(createdEvent.Id, new RegisterStudentRequest("s12345", "Ava"));

        Assert.NotNull(ticket);
        Assert.Equal(createdEvent.Id, ticket.EventId);
        Assert.StartsWith("UNI-EVENT:", ticket.QrCode);
        Assert.NotEmpty(ticket.TicketCode);
    }

    [Fact]
    public void CheckIn_ValidTicket_SucceedsOnce()
    {
        var service = new EventService();
        var createdEvent = service.CreateEvent(new CreateEventRequest(
            "Career Fair",
            "Business Department",
            DateTimeOffset.UtcNow.AddDays(1),
            "Main Hall"));
        var ticket = service.RegisterStudent(createdEvent.Id, new RegisterStudentRequest("s86753", "Noah"))!;

        var firstCheckIn = service.CheckIn(ticket.QrCode);
        var secondCheckIn = service.CheckIn(ticket.QrCode);

        var success = Assert.IsType<CheckInResult.Success>(firstCheckIn);
        Assert.NotNull(success.Attendance.CheckedInAtUtc);
        Assert.IsType<CheckInResult.AlreadyCheckedIn>(secondCheckIn);
    }

    [Fact]
    public void RegisterStudent_ForUnknownEvent_ReturnsNull()
    {
        var service = new EventService();

        var ticket = service.RegisterStudent(Guid.NewGuid(), new RegisterStudentRequest("s11223", "Mia"));

        Assert.Null(ticket);
    }
}
