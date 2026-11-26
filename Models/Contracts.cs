using System.ComponentModel.DataAnnotations;

namespace UniEventsAPI.Models;

public class RegisterRequest
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, StringLength(128)]
    public string Password { get; set; } = "";
}

public class CategoryRequest
{
    [Required, StringLength(80)]
    public string Name { get; set; } = "";
}

public class UserUpdate
{
    [Required, RegularExpression("^(Student|Organizer|Admin)$")]
    public string Role { get; set; } = "";

    [Required]
    public bool? IsActive { get; set; }
}

public class RejectRequest
{
    [Required, StringLength(500)]
    public string Reason { get; set; } = "";
}

public class EventRequest
{
    [Required, StringLength(150)]
    public string Title { get; set; } = "";

    [Required, StringLength(4000)]
    public string Description { get; set; } = "";

    [Required]
    public DateOnly? EventDate { get; set; }

    [Required]
    public TimeOnly? StartTime { get; set; }

    [Required, StringLength(150)]
    public string Venue { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public sealed record EventDetails(int Id, string Title, string Description, DateOnly EventDate, TimeOnly StartTime,
    string Venue, int Capacity, int CategoryId, string Category, string Organizer, string Status, int SeatsRemaining);

public sealed record OrganizerEvent(EventDetails Event, string? RejectReason);
public sealed record Account(int Id, string FullName, string Email, string Role, bool IsActive);
public sealed record LoginResponse(string Token, Account User);
public sealed record Attendee(int UserId, string FullName, DateTimeOffset RegisteredAt, DateTimeOffset? CheckInTime);
public sealed record EventReport(int EventId, string Title, string Status, int Registrations, int Attended,
    decimal AttendanceRatePercent);
