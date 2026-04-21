using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AdoptionAgency.Api.Services;

public class AuthService
{
    private readonly AdoptionDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AdoptionDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var user = await _db.Users
            .Include(u => u.Applicant)
            .FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user);
        return new LoginResponse(
            token,
            user.Email,
            user.Role.ToString(),
            user.Id,
            user.Applicant?.Id);
    }

    public async Task<(LoginResponse? Response, string? Error)> RegisterApplicantAsync(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return (null, "Email already registered");

        var user = new User
        {
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = UserRole.Applicant
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var applicant = new Applicant
        {
            UserId = user.Id,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email
        };
        _db.Applicants.Add(applicant);
        await _db.SaveChangesAsync();

        user = await _db.Users.Include(u => u.Applicant).FirstAsync(u => u.Id == user.Id);
        var token = GenerateJwt(user);
        return (new LoginResponse(token, user.Email, user.Role.ToString(), user.Id, applicant.Id), null);
    }

    public async Task<MeResponse?> GetMeAsync(int userId)
    {
        var user = await _db.Users.Include(u => u.Applicant).FirstOrDefaultAsync(u => u.Id == userId);
        return user == null
            ? null
            : new MeResponse(user.Email, user.Role.ToString(), user.Id, user.Applicant?.Id, user.CanViewPaymentDetails);
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "dev-secret-at-least-32-chars-long!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        if (user.Applicant != null)
            claims.Add(new Claim("ApplicantId", user.Applicant.Id.ToString()));
        claims.Add(new Claim("CanViewPaymentDetails", user.CanViewPaymentDetails.ToString()));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
