using System;

namespace Ametatsu.StatusLightClient
{
    public static class Helpers
    {
        public static DateTimeOffset? ConvertLDAPTimestamp(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp)) return null;
            return ConvertLDAPTimestamp(Int64.Parse(timestamp));
        }

        public static DateTimeOffset? ConvertLDAPTimestamp(Int64 timestamp)
        {
            return new DateTime(1601, 01, 01, 0, 0, 0, DateTimeKind.Utc).AddTicks(timestamp);
        }
    }
}
