namespace UniEventsAPI.Models;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string Venue { get; set; } = "";
    public int Capacity { get; set; }
    public string Status { get; set; } = "Pending";
    public string? RejectReason { get; set; }
    public int CategoryId { get; set; }
    public EventCategory Category { get; set; } = null!;
    public int OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;
    public List<Registration> Registrations { get; set; } = new List<Registration>();
}
