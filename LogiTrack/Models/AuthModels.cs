using System.ComponentModel.DataAnnotations;

namespace LogiTrack.Models;

public class RegisterModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional role assignment. Defaults to "User" or can specify "Manager".
    /// </summary>
    public string? Role { get; set; }
}

public class LoginModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public string Username { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}
