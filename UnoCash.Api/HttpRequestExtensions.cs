using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace UnoCash.Api
{
    public static class HttpRequestExtensions
    {
        internal static string GetUserUpn(this HttpRequest req) =>
            new JwtSecurityTokenHandler().ReadJwtToken(req.Cookies["jwtToken"])
                                         .Claims
                                         .SingleOrDefault(c => c.Type == "upn")
                                         .Value;
    }
}