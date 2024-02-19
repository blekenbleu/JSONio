using System.Collections.Generic;

namespace JSONio
{
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class DataPluginSettings
    {
        public List<Property> properties = new List<Property>()
        {
            new Property() { Name = "threshold", Value = "10" }
        };
    }
}
