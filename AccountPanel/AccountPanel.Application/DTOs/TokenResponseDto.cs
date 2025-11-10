namespace AccountPanel.Application.DTOs;

/// <summary>
/// DTO para devolver al cliente el access token y el refresh token.
/// </summary>
public class TokenResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}