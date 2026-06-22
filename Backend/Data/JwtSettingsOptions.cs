namespace Backend.Data;

using System.Text;

public class JwtSettingsOptions
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;

    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Secret))
            yield return $"{SectionName}:Secret is required";
        else if (Encoding.UTF8.GetByteCount(Secret) < 32)
            yield return $"{SectionName}:Secret must be at least 32 bytes when UTF-8 encoded";

        if (string.IsNullOrWhiteSpace(Issuer))
            yield return $"{SectionName}:Issuer is required";

        if (string.IsNullOrWhiteSpace(Audience))
            yield return $"{SectionName}:Audience is required";

        if (AccessTokenExpirationMinutes <= 0)
            yield return $"{SectionName}:AccessTokenExpirationMinutes must be greater than 0";

        if (RefreshTokenExpirationDays <= 0)
            yield return $"{SectionName}:RefreshTokenExpirationDays must be greater than 0";
    }
}
