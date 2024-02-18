namespace JSONio
{
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class DataPluginSettings
    {
        public List<Property> default = new List<Property>()
		{
			new Property() { Name = 'offset'; Value = '0'; }
		};
    }
}
