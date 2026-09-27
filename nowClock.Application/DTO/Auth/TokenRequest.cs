using System;
using System.Collections.Generic;
using System.Text;

namespace nowClock.Application.DTO.Auth
{
    public class TokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
