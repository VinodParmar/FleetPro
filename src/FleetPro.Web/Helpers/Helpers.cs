namespace FleetPro.Helpers;

public static class PasswordHelper
{
    public static string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public static bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);

    public static bool IsStrong(string password)
    {
        if (password.Length < 8) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        return true;
    }
}

public static class ExpiryHelper
{
    public static string GetCssClass(DateTime? expiry)
    {
        if (!expiry.HasValue) return "";
        var days = (expiry.Value.Date - DateTime.Today).Days;
        return days < 0 ? "expiry-bad" : days <= 7 ? "expiry-bad" : days <= 30 ? "expiry-warn" : "expiry-ok";
    }

    public static string GetLabel(DateTime? expiry)
    {
        if (!expiry.HasValue) return "—";
        var days = (expiry.Value.Date - DateTime.Today).Days;
        if (days < 0) return $"EXPIRED {Math.Abs(days)}d ago";
        if (days == 0) return "Expires TODAY";
        return $"{days}d remaining";
    }

    public static string GetBadgeClass(DateTime? expiry)
    {
        if (!expiry.HasValue) return "badge-secondary";
        var days = (expiry.Value.Date - DateTime.Today).Days;
        return days < 0 ? "badge-danger" : days <= 7 ? "badge-danger" : days <= 30 ? "badge-warning" : "badge-success";
    }
}

public static class StringHelper
{
    public static string Initials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "??";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
            : name.Substring(0, Math.Min(2, name.Length)).ToUpper();
    }

    public static string Truncate(string? text, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "…";
    }
}
