using System.Collections.Generic;

namespace blekenbleu.jsonio
{
	public class Property	   // must be public for DataPluginSettings
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}

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

		public List<string> carpropnames = new List<string>();
		public List<string> gamepropnames = new List<string>();
	}
}
