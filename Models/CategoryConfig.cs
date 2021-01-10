using System.Collections.Generic;

namespace Ametatsu.StatusLightClient.Models
{
    public class CategoryConfig
    {
        public List<string> CategoryPriority { get; set; }
        public Dictionary<string,string> CategoryColors { get; set; }
        public Dictionary<string,string> CategoryMapping { get; set; }
        public List<string> IgnoredApps { get; set; }
    }
}
