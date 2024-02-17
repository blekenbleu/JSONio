using System;

namespace JSONio
{
	public class Property
	{ public string Name { get; set; } public string Value { get; set; } }

	public class Car
	{
		public string id { get; set; }
		public string name { get; set; }
		public List<Property> properties { get; set; }
	}
	public class Game
	{
		public string name { get; set; }
		public List<Car> cars { get; set; }
	}
	public class Games
	{
		public Games()
		{
			public List<Game> list { get; set; }
		}
	}
}
