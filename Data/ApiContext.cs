using Microsoft.EntityFrameworkCore;
using UniEventsAPI.Models;

namespace UniEventsAPI.Data;

public class ApiContext : DbContext
{
    public ApiContext(DbContextOptions<ApiContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<EventCategory> Categories { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Registration> Registrations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<EventCategory>().HasIndex(category => category.Name).IsUnique();
        modelBuilder.Entity<Registration>().HasIndex(registration => new { registration.UserId, registration.EventId })
            .IsUnique();
    }
}
