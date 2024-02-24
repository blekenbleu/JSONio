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
			// each Value stored as string of integer 10x actual value
            new Property() { Name = "threshold", Value = "0" }
        };
		public string gname = "";	// game for which properties were saved
    }
}
