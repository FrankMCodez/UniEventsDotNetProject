using Microsoft.EntityFrameworkCore;
using UniEventsAPI.Configuration;
using UniEventsAPI.Data;
using UniEventsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApiContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UniEvents")));
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddControllers();

builder.AddJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddOpenApi(ApiSetup.ConfigureSwagger);

var app = builder.Build();

if (args is ["bootstrap-admin", var email])
{
    using var scope = app.Services.CreateScope();
    var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
    await accounts.BootstrapAdmin(email, CancellationToken.None);
    Console.WriteLine("Admin account activated.");
    return;
}

app.UseExceptionHandler(ApiSetup.ErrorHandler);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "UniEvents API v1"));
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

