namespace Auth.Api.Settings;

public sealed class CookieSettings
{
    public string AccessTokenCookieName { get; set; } = "access_token";
    public string RefreshTokenCookieName { get; set; } = "refresh_token";
    public bool HttpOnly { get; set; } = true;
    public bool Secure { get; set; } = true;
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;
    public int AccessTokenMaxAgeMinutes { get; set; } = 5;
    public int RefreshTokenMaxAgeDays { get; set; } = 7;

    public CookieOptions GetAccessTokenCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = HttpOnly,
            Secure = Secure,
            SameSite = SameSite,
            MaxAge = TimeSpan.FromMinutes(AccessTokenMaxAgeMinutes),
            Path = "/"
        };
    }

    public CookieOptions GetRefreshTokenCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = HttpOnly,
            Secure = Secure,
            SameSite = SameSite,
            MaxAge = TimeSpan.FromDays(RefreshTokenMaxAgeDays),
            Path = "/api/auth"
        };
    }
}