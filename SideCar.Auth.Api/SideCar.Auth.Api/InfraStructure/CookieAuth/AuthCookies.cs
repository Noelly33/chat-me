using Microsoft.AspNetCore.Http;

namespace SideCar.Auth.Api.InfraStructure.CookieAuth
{
    public static class AuthCookies
    {
        public const string DefaultAuthCookieName = "chat_auth";
        public const string DefaultRefreshCookieName = "chat_refresh";
        public const string DefaultPath = "/";
        public const string DefaultRefreshPath = "/api/v1/auth";

        public static string AuthCookieName(IConfiguration config)
            => config["JwtSettings:AuthCookieName"] ?? DefaultAuthCookieName;

        public static string RefreshCookieName(IConfiguration config)
            => config["JwtSettings:RefreshCookieName"] ?? DefaultRefreshCookieName;

        public static string CookiePath(IConfiguration config)
            => config["JwtSettings:CookiePath"] ?? DefaultPath;

        public static string RefreshCookiePath(IConfiguration config)
            => config["JwtSettings:RefreshCookiePath"] ?? DefaultRefreshPath;

        public static bool CookieSecureOnly(IConfiguration config)
            => bool.TryParse(config["JwtSettings:CookieSecureOnly"], out var v) ? v : true;

        public static SameSiteMode CookieSameSite(IConfiguration config)
            => (config["JwtSettings:CookieSameSite"] ?? "Lax").ToLowerInvariant() switch
            {
                "strict" => SameSiteMode.Strict,
                "none" => SameSiteMode.None,
                _ => SameSiteMode.Lax,
            };

        public static void Set(
            HttpResponse response,
            IConfiguration config,
            string accessToken,
            string refreshToken,
            DateTime refreshTokenExpiration)
        {
            var accessMinutes = double.Parse(config["JwtSettings:AccessTokenExpirationInMinutes"] ?? "15");
            var sameSite = CookieSameSite(config);
            var secureOnly = CookieSecureOnly(config);
            var path = CookiePath(config);
            var refreshPath = RefreshCookiePath(config);

            response.Cookies.Append(AuthCookieName(config), accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = secureOnly,
                SameSite = sameSite,
                Path = path,
                MaxAge = TimeSpan.FromMinutes(accessMinutes),
            });

            response.Cookies.Append(RefreshCookieName(config), refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = secureOnly,
                SameSite = sameSite,
                Path = refreshPath,
                Expires = refreshTokenExpiration,
            });
        }

        public static void Clear(HttpResponse response, IConfiguration config)
        {
            var sameSite = CookieSameSite(config);
            var secureOnly = CookieSecureOnly(config);

            response.Cookies.Delete(AuthCookieName(config), new CookieOptions
            {
                HttpOnly = true,
                Secure = secureOnly,
                SameSite = sameSite,
                Path = CookiePath(config),
            });
            response.Cookies.Delete(RefreshCookieName(config), new CookieOptions
            {
                HttpOnly = true,
                Secure = secureOnly,
                SameSite = sameSite,
                Path = RefreshCookiePath(config),
            });
        }
    }
}