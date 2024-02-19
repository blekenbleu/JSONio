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
		public List<Car> Clist { get; set; }
		public void set(string gname) { name = gname; }

		// add (if missing) a value to Car properties,
		// but do not replace value if present
		public bool append (string name, string value)
		{
			bool changed = false;

			if(defaults.mod(name, value, false))
				changed = true;
			foreach (Car c in Clist)
				if(c.mod(name, value, false))
					changed = true;
			return changed;
		}

		// add or replace property values
		public bool mod(Car car)
		{
			bool changed;
			int index = Clist.FindIndex(a => a.id == car.id);

			if (changed = -1 == index)
				Clist.Add(car);
			else foreach (Property p in car.properties)
					if (Clist[index].mod(p.Name, p.Value, true))
						changed = true;
            return changed;
		}
	}

	public class Games
	{
		public List<Game> Glist { get; set; }

		public bool mod(Game game)
		{
			bool changed;

            int index = Glist.FindIndex(g => g.name == game.name);

			if (changed = -1 == index)
				Glist.Add(game);
			else foreach (Car c in game.Clist)
				if(Glist[index].mod(c))
						changed = true;
			return changed;
		}

		internal bool New_Car(string cname, string gname)
		{
			bool changed = true;
			Car car = new Car() {id = cname, properties = DataPlugin.current};
			int gndex;

			if (null == Glist)
			{
				List<Car> car0 = new List<Car>() { car };
				Game game0 = new Game { name=gname, defaults=car, Clist=car0 };
				game0.defaults.id = "default";
				Glist = new List<Game>() { game0 };
				gndex = 0;
			}
			else gndex = Glist.FindIndex(g => g.name == gname);

			if (-1 == gndex) {
				List<Car> first = new List<Car>() { car };
				car.id = "default";
				Glist.Add( new Game() { name=gname, defaults = car, Clist = first });
			} else {
                int cndex = Glist[gndex].Clist.FindIndex(c => c.id == cname);

                if (-1 == cndex)
                {
					car.id = cname;
					Glist[gndex].Clist.Add(car);
				}
				else {
					DataPlugin.previous = DataPlugin.current;
					DataPlugin.current = Glist[gndex].Clist[cndex].properties;
					changed = false;
				}
			}
			return changed;
		}

	}
}
