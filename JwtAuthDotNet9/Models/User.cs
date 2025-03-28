using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthDotNet9.Models
{
    [Table("Users")]
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "USER";
        public string Email { get; set; } = string.Empty;


    }
}
