using System;

namespace Ametatsu.StatusLightClient.Models
{
    public class AppStatus
    {
        public string AppName { get; set; }
        public string Category { get; set; }
        public bool IgnoredApp { get; set; }

        public bool Active => MicActive || CamActive;

        public DateTimeOffset? MicLastUsedTimeStart { get; set; }
        public DateTimeOffset? MicLastUsedTimeStop { get; set; }
        public bool MicActive => MicLastUsedTimeStop < MicLastUsedTimeStart;
        public TimeSpan? MicActiveDuration => MicActive ? DateTime.Now - MicLastUsedTimeStart : null;

        public DateTimeOffset? CamLastUsedTimeStart { get; set; }
        public DateTimeOffset? CamLastUsedTimeStop { get; set; }
        public bool CamActive => CamLastUsedTimeStop < CamLastUsedTimeStart;
        public TimeSpan? CamActiveDuration => CamActive ? DateTime.Now - CamLastUsedTimeStart : null;
    }
}
