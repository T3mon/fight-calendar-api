namespace FightCalendar.Auth.Models;

/// <summary>The ID token Google's Sign In With Google JS library hands the frontend after the user picks an account.</summary>
public record GoogleSignInRequest(string IdToken);

public record AuthResponseDto(string Token, DateTimeOffset ExpiresAt, string Email);

public record CurrentUserDto(string Id, string Email);
