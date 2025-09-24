using System.Text;
namespace server.Util;

public static class CursorUtil
{
    public static string EncodeCursor(DateTime dateTime, string worldId) // 프론트 전용..
    {
        string raw = $"{dateTime.ToUniversalTime():O}|{worldId}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static (DateTime dateTime, string worldId) DecodeCursor(string cursor)
    {
        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        string[] parts = decoded.Split('|', 2); // 2개까지만 split

        if (parts.Length != 2)
            throw new FormatException("잘못된 커서 형식");

        DateTime dt = DateTime.Parse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind);
        string worldId = parts[1];

        return (dt, worldId);
    }
}