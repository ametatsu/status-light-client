using System.Collections.Generic;
using System.Linq;
using Ametatsu.StatusLightClient.Models;
using Microsoft.Win32;

namespace Ametatsu.StatusLightClient.Services
{
    public class Win10MicConsentService
    {
        private static readonly string KEY_LOCATION =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\";

        public Win10MicConsentService()
        {
            
        }

        public IEnumerable<Win10ConsentStatus> GetConsentStatuses(string consentDomain)
        {
            var consentStatuses = new List<Win10ConsentStatus>();

            // Newer Windows 10 versions store the consent data in the current user hive
            using var currentUserKey = Registry.CurrentUser.OpenSubKey(KEY_LOCATION + consentDomain);
            if (currentUserKey != null)
            {
                consentStatuses.AddRange(ExtractConsentStatusesFromRegKey(currentUserKey, consentDomain));
            }

            // Older Windows 10 versions use the local machine hive for this data
            using var localMachineKey = Registry.LocalMachine.OpenSubKey(KEY_LOCATION + consentDomain);
            if (localMachineKey != null)
            {
                consentStatuses.AddRange(ExtractConsentStatusesFromRegKey(localMachineKey, consentDomain));
            }

            // Dedupe records with the same name by using the latest start date
            var dedupedStatuses = consentStatuses.GroupBy(s => s.AppName) // Group by app name
                .Select(g => g.OrderByDescending(gs => gs.LastUsedTimeStart)) // Sort latest start date to top for each app
                .Select(g => g.First()); // Grab the first item in each app

            return dedupedStatuses;
        }

        private List<Win10ConsentStatus> ExtractConsentStatusesFromRegKey(RegistryKey rootKey, string consentDomain)
        {
            if (rootKey == null) return null;

            var consentStatuses = new List<Win10ConsentStatus>();

            foreach (var subKeyName in rootKey.GetSubKeyNames())
            {
                // Win32 apps are further nested in a "NonPackaged" key
                if (subKeyName == "NonPackaged")
                {
                    var nonPackagedRoot = rootKey.OpenSubKey(subKeyName);
                    foreach (var nonPackagedSubKeyName in nonPackagedRoot.GetSubKeyNames())
                    {
                        var status = BuildConsentStatus(nonPackagedRoot.OpenSubKey(nonPackagedSubKeyName));
                        status.ConsentDomain = consentDomain;
                        consentStatuses.Add(status);
                    }
                }
                // UWP apps are in the root
                else
                {
                    var status = BuildConsentStatus(rootKey.OpenSubKey(subKeyName));
                    status.ConsentDomain = consentDomain;
                    status.PackagedApp = true;
                    consentStatuses.Add(status);
                }
            }

            return consentStatuses;
        }

        private Win10ConsentStatus BuildConsentStatus(RegistryKey appKey)
        {
            var lastUsedTimeStartValue = appKey.GetValue("LastUsedTimeStart")?.ToString();
            var lastUsedTimeStart = Helpers.ConvertLDAPTimestamp(lastUsedTimeStartValue);
            var lastUsedTimeStopValue = appKey.GetValue("LastUsedTimeStop")?.ToString();
            var lastUsedTimeStop = Helpers.ConvertLDAPTimestamp(lastUsedTimeStopValue);

            var value = appKey.GetValue("Value")?.ToString();

            return new Win10ConsentStatus
            {
                KeyName = appKey.Name,
                LastUsedTimeStart = lastUsedTimeStart,
                LastUsedTimeStop = lastUsedTimeStop,
                Consent = value
            };
        }
    }
}
