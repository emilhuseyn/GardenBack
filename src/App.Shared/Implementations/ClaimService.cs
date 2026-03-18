using App.Shared.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace App.Shared.Implementations
{
    public class ClaimService : IClaimService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClaimService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetUserId()
        {
            return GetClaim(ClaimTypes.Name);
        }

        public string GetClaim(string key)
        {
            var result = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "id")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault()?.Value;

            return result;
        }
    }
}
