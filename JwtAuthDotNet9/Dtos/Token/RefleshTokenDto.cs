using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthDotNet9.Dtos.User
{
    public class RefleshTokenDto
    {
        public Guid UserId { get; set; }
        public string? RefleshToken { get; set; } = string.Empty;

    }
}