using GymCrest.Domain.Interface;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GymCrest.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor) 
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                HttpContext? httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return null;

                ClaimsPrincipal user = httpContext.User;
                
                string? userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                  ?? user.FindFirst("userId")?.Value 
                                  ?? user.FindFirst("uid")?.Value;

                if (Guid.TryParse(userIdClaim, out Guid userId))
                {
                    return userId;
                }

                return null;
            }
        } 
    }
}
