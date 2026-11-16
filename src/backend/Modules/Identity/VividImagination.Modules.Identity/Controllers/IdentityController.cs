using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VividImagination.Modules.Identity.Contracts;
using VividImagination.Modules.Identity.Entities;
using VividImagination.Modules.Identity.Persistence;

namespace VividImagination.Modules.Identity.Controllers;

[ApiController]
[Route("api/v1/identity")]
public class IdentityController : ControllerBase
{
    private readonly IdentityDbContext _dbContext;

    public IdentityController(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public record LoginRequest(string Email, string Username);
    public record LoginResponse(Guid UserId, string Email, string Username, SafeBrowsingMode SafeBrowsingMode, string Token);
    public record UpdatePreferencesRequest(SafeBrowsingMode SafeBrowsingMode);

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Username = string.IsNullOrWhiteSpace(request.Username) ? request.Email.Split('@')[0] : request.Username,
                SafeBrowsingMode = SafeBrowsingMode.BlurSensitive,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(ct);
        }

        var mockJwtToken = $"vivid_jwt_{user.Id:N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        return Ok(new LoginResponse(user.Id, user.Email, user.Username, user.SafeBrowsingMode, mockJwtToken));
    }

    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<User>> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _dbContext.Users.FindAsync([id], ct);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPut("users/{id:guid}/preferences")]
    public async Task<IActionResult> UpdatePreferences(Guid id, [FromBody] UpdatePreferencesRequest request, CancellationToken ct)
    {
        var user = await _dbContext.Users.FindAsync([id], ct);
        if (user == null)
        {
            return NotFound();
        }

        user.SafeBrowsingMode = request.SafeBrowsingMode;
        await _dbContext.SaveChangesAsync(ct);

        return Ok(new { user.Id, user.SafeBrowsingMode });
    }
}
