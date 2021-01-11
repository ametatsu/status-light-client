using System.Collections.Generic;

namespace Ametatsu.StatusLightClient.Models
{
    public class HomeAssistantConfig
    {
        public string BaseUrl { get; set; }
        public string AuthToken { get; set; }
        public string StatusEntity { get; set; }
    }
}
