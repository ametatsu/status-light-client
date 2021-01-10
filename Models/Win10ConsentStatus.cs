using System;

namespace Ametatsu.StatusLightClient.Models
{
    public class Win10ConsentStatus
    {
        public string ConsentDomain { get; set; }
        public string Consent { get; set; }
        public string KeyName { get; set; }
        public bool PackagedApp { get; set; }
        public DateTimeOffset? LastUsedTimeStart { get; set; }
        public DateTimeOffset? LastUsedTimeStop { get; set; }

        public bool Active => LastUsedTimeStop < LastUsedTimeStart;

        public string AppName
        {
            get
            {
                var relativeKeyName = KeyName.Substring(KeyName.LastIndexOf('\\') + 1);
                if (PackagedApp)
                {
                    if (relativeKeyName[^14] == '_')
                    {
                        return relativeKeyName.Substring(0, relativeKeyName.Length - 14);
                    }
                    else
                    {
                        return relativeKeyName;
                    }
                }
                else
                {
                    var exeName = relativeKeyName.Substring(relativeKeyName.LastIndexOf('#') + 1);
                    return exeName.Replace(".exe", "", StringComparison.InvariantCultureIgnoreCase);
                }
            }
        }
    }
}
