namespace Shared;
public record JwtSettings(string Issuer, string Audience, string Key, int ExpirationMinutes);

public static class ErrorTypes
{
    public const string INVALID_DOCUMENT = "INVALID_DOCUMENT";
    public const string USER_UNAUTHORIZED = "USER_UNAUTHORIZED";
    public const string INVALID_ACCOUNT = "INVALID_ACCOUNT";
    public const string INACTIVE_ACCOUNT = "INACTIVE_ACCOUNT";
    public const string INVALID_VALUE = "INVALID_VALUE";
    public const string INVALID_TYPE = "INVALID_TYPE";
}
