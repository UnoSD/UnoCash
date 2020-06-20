using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace UnoCash.Api
{
    public static class HttpRequestExtensions
    {
        internal static string GetUserEmail(this HttpRequest req) =>
            new JwtSecurityTokenHandler().ReadJwtToken(req.Cookies["jwtToken"])
                                         .Claims
                                         .SingleOrDefault(c => c.Type == "email")
                                         .Value;
    }
}