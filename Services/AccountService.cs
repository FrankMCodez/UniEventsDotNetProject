using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UniEventsAPI.Data;
using UniEventsAPI.Models;

namespace UniEventsAPI.Services;

public class AccountService
{
    public const string Issuer = "UniEvents";
    private readonly ApiContext _context;
    private readonly IPasswordHasher<string> _hasher;
    private readonly SigningCredentials _signingCredentials;

    public AccountService(ApiContext context, IPasswordHasher<string> hasher, SigningCredentials signingCredentials)
    {
        _context = context;
        _hasher = hasher;
        _signingCredentials = signingCredentials;
    }

    public async Task<Account> Register(RegisterRequest request, CancellationToken ct)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _hasher.HashPassword(email, request.Password)
        };

        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ApiError(409, "An account already exists for this email.");
        }
        return new Account(user.Id, user.FullName, user.Email, user.Role, user.IsActive);
    }

    public async Task<LoginResponse> Login(LoginRequest request, CancellationToken ct)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Email == email, ct);
        if (user is null || !user.IsActive ||
            _hasher.VerifyHashedPassword(email, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw new ApiError(401, "Invalid email or password, or account is inactive.");

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role)
        ];
        var token = new JwtSecurityToken(Issuer, Issuer, claims, expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: _signingCredentials);
        var account = new Account(user.Id, user.FullName, user.Email, user.Role, user.IsActive);
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), account);
    }

    public Task<bool> IsCurrent(int id, string role, CancellationToken ct)
        => _context.Users.AnyAsync(user => user.Id == id && user.IsActive && user.Role == role, ct);

    public Task<List<Account>> GetUsers(CancellationToken ct)
        => _context.Users.OrderBy(user => user.Id)
            .Select(user => new Account(user.Id, user.FullName, user.Email, user.Role, user.IsActive))
            .ToListAsync(ct);

    public async Task UpdateUser(int id, int adminId, UserUpdate request, CancellationToken ct)
    {
        if (id == adminId && (!request.IsActive!.Value || request.Role != "Admin"))
            throw new ApiError(400, "You cannot deactivate or demote your own admin account.");

        var user = await _context.Users.FindAsync([id], ct)
            ?? throw new ApiError(404, "User was not found.");
        user.Role = request.Role;
        user.IsActive = request.IsActive!.Value;
        await _context.SaveChangesAsync(ct);
    }

    public async Task BootstrapAdmin(string email, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (await _context.Users.AnyAsync(user => user.Role == "Admin" && user.IsActive, ct))
            throw new InvalidOperationException("An active admin already exists.");

        var account = await _context.Users.FirstOrDefaultAsync(user => user.Email == email && user.IsActive, ct)
            ?? throw new InvalidOperationException("Register an active account for this email first.");
        account.Role = "Admin";
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
