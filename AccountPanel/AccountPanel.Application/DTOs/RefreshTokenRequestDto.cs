namespace AccountPanel.Application.DTOs;

/// <summary>
/// DTO para recibir la petición de refrescar un token.
/// </summary>
public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; }
}