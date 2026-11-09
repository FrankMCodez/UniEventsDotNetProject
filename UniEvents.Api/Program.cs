using UniEvents.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<EventService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/events", (CreateEventRequest request, EventService service) =>
{
    var createdEvent = service.CreateEvent(request);
    return Results.Created($"/events/{createdEvent.Id}", createdEvent);
})
.WithName("CreateEvent");

app.MapGet("/events", (EventService service) =>
{
    return Results.Ok(service.GetEvents());
})
.WithName("GetEvents");

app.MapPost("/events/{eventId:guid}/registrations", (Guid eventId, RegisterStudentRequest request, EventService service) =>
{
    var ticket = service.RegisterStudent(eventId, request);
    return ticket is null
        ? Results.NotFound(new { message = "Event not found." })
        : Results.Ok(ticket);
})
.WithName("RegisterStudent");

app.MapPost("/checkins", (CheckInRequest request, EventService service) =>
{
    return service.CheckIn(request.ScannedCode) switch
    {
        CheckInResult.Success(var attendance) => Results.Ok(attendance),
        CheckInResult.InvalidTicket => Results.BadRequest(new { message = "Invalid ticket." }),
        CheckInResult.AlreadyCheckedIn => Results.Conflict(new { message = "Attendee already checked in." }),
        _ => Results.BadRequest()
    };
})
.WithName("CheckIn");

app.Run();

public partial class Program;
