using System.IdentityModel.Tokens.Jwt;
using System.Net;
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
public class AuthController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    JwtTokenService jwtTokenService,
    IEmailSender emailSender,
    IOptions<GoogleOptions> googleOptions,
    IOptions<FrontendOptions> frontendOptions) : ControllerBase
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

    /// <summary>
    /// Creates an email+password account. Unlike Google sign-in, this can't trust the email is real on its own, so
    /// the account starts unconfirmed and can't sign in until the link in the confirmation email is used.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            // Deliberately not "add a password to this account" here, even if
            // existing is Google-only - that path requires being signed in as
            // that account already (see SetPassword), not just knowing its
            // email address.
            return Conflict("An account with this email already exists. Sign in instead.");
        }

        var user = new IdentityUser { UserName = request.Email, Email = request.Email, EmailConfirmed = false };
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            // Bad client input (weak password, etc.) - Problem() defaults to
            // 500 without an explicit status, which would be misleading here.
            return Problem(string.Join("; ", createResult.Errors.Select(e => e.Description)), statusCode: StatusCodes.Status400BadRequest);
        }

        await SendConfirmationEmailAsync(user, ct);
        return Ok();
    }

    /// <summary>Confirms the email address using the token from the link sent by <c>POST /auth/register</c>.</summary>
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return BadRequest("This confirmation link is invalid.");
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return BadRequest("This confirmation link is invalid or has expired.");
        }

        return Ok();
    }

    /// <summary>
    /// Re-sends the confirmation email. Always returns success regardless of whether the address has an account or
    /// is already confirmed - otherwise this endpoint could be used to check who's registered.
    /// </summary>
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null && !user.EmailConfirmed)
        {
            await SendConfirmationEmailAsync(user, ct);
        }

        return Ok();
    }

    /// <summary>Exchanges an email+password for our own JWT, the password-based equivalent of <c>POST /auth/google</c>.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized("Incorrect email or password.");
        }

        if (!await userManager.HasPasswordAsync(user))
        {
            // Created via Google sign-in and never added a password (see SetPassword) -
            // "incorrect password" would be misleading, there simply isn't one to check.
            return Unauthorized("This account signs in with Google. Sign in with Google, or add a password from your account settings first.");
        }

        // lockoutOnFailure: true locks the account out after repeated bad attempts
        // (IdentityUser already has the AccessFailedCount/LockoutEnd columns for
        // this) - free brute-force protection, no extra code needed here.
        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return Unauthorized("Too many failed attempts. Try again later.");
        }
        if (!result.Succeeded)
        {
            return Unauthorized("Incorrect email or password.");
        }

        if (!user.EmailConfirmed)
        {
            return Unauthorized("Confirm your email before signing in - check your inbox, or use /auth/resend-confirmation.");
        }

        var (token, expiresAt) = jwtTokenService.CreateToken(user);
        return Ok(new AuthResponseDto(token, expiresAt, user.Email!));
    }

    /// <summary>
    /// Adds a password to the signed-in user's own account - for someone who signed up with Google and wants
    /// email+password as a second way in (a future phone app, Telegram bot, etc). Fails if one is already set;
    /// that's a "change password" operation, not this one.
    /// </summary>
    [HttpPost("set-password")]
    [Authorize]
    public async Task<IActionResult> SetPassword(SetPasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(CurrentUserId);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await userManager.AddPasswordAsync(user, request.Password);
        if (!result.Succeeded)
        {
            // Same reasoning as Register: this is the client's fault (already
            // has a password, or the new one is too weak), not a server error.
            return Problem(string.Join("; ", result.Errors.Select(e => e.Description)), statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok();
    }

    /// <summary>
    /// Permanently deletes the signed-in user's own account and everything tied to it (tracked promotions and
    /// fighters, Google login link). There is no undo.
    /// </summary>
    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount()
    {
        var user = await userManager.FindByIdAsync(CurrentUserId);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Problem(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return Ok();
    }

    /// <summary>Returns the signed-in user's identity. Requires a valid <c>Authorization: Bearer</c> token from a prior <c>POST /auth/google</c> call.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        return Ok(new CurrentUserDto(CurrentUserId, User.FindFirst(JwtRegisteredClaimNames.Email)?.Value!));
    }

    // Always read "who is this" from the token's own claims, never from a
    // request body field - a client can't be trusted to say which account
    // it's acting as, only the signed JWT can.
    private string CurrentUserId => User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value;

    private async Task SendConfirmationEmailAsync(IdentityUser user, CancellationToken ct)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = $"{frontendOptions.Value.BaseUrl}/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";
        var html = $"""
            <p>Welcome to Fight Calendar - confirm your email to finish setting up your account:</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">Confirm email</a></p>
            <p>If you didn't create this account, you can ignore this email.</p>
            """;
        await emailSender.SendAsync(user.Email!, "Confirm your Fight Calendar account", html, ct);
    }
}
