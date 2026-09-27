using System;
using System.Collections.Generic;
using System.Text;

namespace nowClock.Application.DTO.Auth
{
    public class TwoFactorSetupResponse
    {
        public string SharedKey { get; set; } = string.Empty;
        public string AuthenticatorUri { get; set; } = string.Empty;
    }
}
