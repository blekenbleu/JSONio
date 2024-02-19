using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
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
		public void set(string value) { Value = value; }
		public string getName() { return Name; }
		public string getValue() { return Value; }
	}

	// return true if any Game changes
	public class Car
	{
		public string id { get; set; }
		public List<Property> properties { get; set; }
		public void set (string cid) { id = cid; }
		public bool mod(string name, string value, bool replace)
		{
			int index = properties.FindIndex(a => a.Name == name);

			if (-1 == index)
				properties.Add ( new Property() { Name=name, Value=value });
			else if (replace && properties[index].Value != value)
				properties[index].Value = value;
			else return false;
			return true;
		}
	}

	public class Game
	{
		public string name { get; set; }
		public Car defaults { get; set; }
		public List<Car> cars { get; set; }
		public void set(string gname) { name = gname; }

		// add (if missing) a value to Car properties,
		// but do not replace value if present
		public bool append (string name, string value)
		{
			bool changed = false;

			if(defaults.mod(name, value, false))
				changed = true;
			foreach (Car c in cars)
				if(c.mod(name, value, false))
					changed = true;
			return changed;
		}

		// add or replace property values
		public bool mod(Car car)
		{
			bool changed;
			int index = cars.FindIndex(a => a.id == car.id);

			if (changed = -1 == index)
				cars.Add(car);
			else foreach (Property p in car.properties)
					if (cars[index].mod(p.Name, p.Value, true))
						changed = true;
            return changed;
		}
	}

	public class Games
	{
		public Car defaults { get; set; }
		public List<Game> list { get; set; }

		public bool mod(Game game)
		{
			bool changed;

            int index = list.FindIndex(g => g.name == game.name);

			if (changed = -1 == index)
				list.Add(game);
			else foreach (Car c in game.cars)
				if(list[index].mod(c))
						changed = true;
			return changed;
		}
	}
}
