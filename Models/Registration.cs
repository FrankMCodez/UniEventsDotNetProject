namespace UniEventsAPI.Models;

public class Registration
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int EventId { get; set; }
    public string Status { get; set; } = "Registered";
    public DateTimeOffset RegisteredAt { get; set; }
    public DateTimeOffset? CheckInTime { get; set; }
}
