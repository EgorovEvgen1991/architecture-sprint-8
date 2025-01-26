using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ReportMaker.Midleware
{
    public class JwtMiddleware
    {
        private static readonly string Bearer = "bearer";
        private readonly JwtSecurityTokenHandler _handler = new JwtSecurityTokenHandler();
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers[HeaderNames.Authorization].ToString();
            Console.WriteLine($"Token {token}");
            if (!token.ToLower().StartsWith(Bearer))
            {
                throw new InvalidOperationException(string.Format("Expected {0} at the start of the token.", Bearer));
            }

            var jwt = _handler.ReadJwtToken(token.Substring(Bearer.Length).TrimStart());
            context.User = new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims));
            Console.WriteLine($"context.User {context.User.ToString()}");
            await _next(context);
        }
    }
}
