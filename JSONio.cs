using System;
using System.Collections.Generic;

namespace JSONio
{
	public class Property
	{
		public string Name { get; set; }
		public string Value { get; set; }
		public void set(string name, string value)
		{ Name = name; Value = value; }
		public string getName() { return Name; }
		public string getValue() { return Value; }
	}

	public class Car
	{
		public string id { get; set; }
		public string name { get; set; }
		public List<Property> properties { get; set; }
		public void set (string cname, string cid)
		{ name = cname; id = cid; }
		public void add(Property property)
		{ properties.Add(property); }
	}
	public class Game
	{
		public string name { get; set; }
		public List<Car> cars { get; set; }
		public void set(string gname)
		{ name = gname; }
		public void add(Car car)
		{ cars.Add(car); }
	}
	public class Games
	{
        public List<Game> list { get; set; }
		public void add(Game game)
		{ list.Add(game); }
	}
}
