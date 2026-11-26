using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using UniEventsAPI.Services;

namespace UniEventsAPI.Configuration;

internal static class ApiSetup
{
    public static void AddJwtAuthentication(this WebApplicationBuilder builder)
    {
        string? configuredKey = builder.Configuration["Jwt:Key"];
        byte[] key = configuredKey is null && builder.Environment.IsDevelopment()
            ? RandomNumberGenerator.GetBytes(32)
            : Encoding.UTF8.GetBytes(configuredKey ?? "");
        if (key.Length < 32)
            throw new InvalidOperationException("Configure Jwt:Key with at least 32 bytes.");

        var signingKey = new SymmetricSecurityKey(key);
        builder.Services.AddSingleton(new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
        builder.Services.AddScoped<IPasswordHasher<string>, PasswordHasher<string>>();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = AccountService.Issuer,
                ValidAudience = AccountService.Issuer,
                IssuerSigningKey = signingKey,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents { OnTokenValidated = ValidateAccount };
        });
    }

    private static async Task ValidateAccount(TokenValidatedContext context)
    {
        var accounts = context.HttpContext.RequestServices.GetRequiredService<AccountService>();
        string? role = context.Principal?.FindFirstValue(ClaimTypes.Role);
        if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ||
            role is null || !await accounts.IsCurrent(id, role, context.HttpContext.RequestAborted))
            context.Fail("The account or role has changed. Sign in again.");
    }

    public static void ConfigureSwagger(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };
            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, _) =>
        {
            if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
                operation.Security = [new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
                }];
            return Task.CompletedTask;
        });
    }

    public static ExceptionHandlerOptions ErrorHandler { get; } = new()
    {
        AllowStatusCode404Response = true,
        ExceptionHandler = context =>
        {
            var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var (status, message) = error switch
            {
                ApiError apiError => (apiError.Status, apiError.Message),
                SqlException or DbUpdateException => (503, "The database request could not be completed."),
                _ => (500, "An unexpected server error occurred.")
            };
            return Results.Problem(statusCode: status, detail: message).ExecuteAsync(context);
        }
    };
}
