using System.IdentityModel.Tokens.Jwt;
using FightCalendar.Auth.Models;
using FightCalendar.Auth.Options;
using FightCalendar.Auth.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FightCalendar.Auth.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(UserManager<IdentityUser> userManager, JwtTokenService jwtTokenService, IOptions<GoogleOptions> googleOptions) : ControllerBase
{
    private const string GoogleLoginProvider = "Google";

    /// <summary>
    /// Exchanges a Google ID token (from the frontend's Sign In With Google button) for our own JWT. Creates the
    /// user's account on their first sign-in - there's no separate registration step.
    /// </summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponseDto>> SignInWithGoogle(GoogleSignInRequest request, CancellationToken ct)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [googleOptions.Value.ClientId],
            });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized("The Google sign-in token is invalid or expired.");
        }

        var user = await userManager.FindByLoginAsync(GoogleLoginProvider, payload.Subject);

        if (user is null)
        {
            // Google verified this email is real and owned by whoever's signing in, so it's
            // trustworthy enough to skip the usual "click the link we emailed you" step.
            user = new IdentityUser { UserName = payload.Email, Email = payload.Email, EmailConfirmed = true };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return Problem(string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }

            var addLoginResult = await userManager.AddLoginAsync(user, new UserLoginInfo(GoogleLoginProvider, payload.Subject, GoogleLoginProvider));
            if (!addLoginResult.Succeeded)
            {
                return Problem(string.Join("; ", addLoginResult.Errors.Select(e => e.Description)));
            }
        }

        var (token, expiresAt) = jwtTokenService.CreateToken(user);
        return Ok(new AuthResponseDto(token, expiresAt, user.Email!));
    }

    /// <summary>Returns the signed-in user's identity. Requires a valid <c>Authorization: Bearer</c> token from a prior <c>POST /auth/google</c> call.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var id = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        return Ok(new CurrentUserDto(id!, email!));
    }
}
