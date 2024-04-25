using System.Collections.Generic;

namespace blekenbleu
{
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class DataPluginSettings
    {
        public List<Property> properties = new List<Property>()
        {
			// each current Value stored as string of integer 10x actual value
            new Property() {}
        };
    }
}
