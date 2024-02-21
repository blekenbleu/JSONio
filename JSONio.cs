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

		// This is a deep copy implementation of Clone
		public Property Clone()
		{
			Property another = new Property() {};
			another.Name = string.Copy(Name);
			another.Value = string.Copy(Value);
			return another;
		}
	}

    // return true if any Game changes
    public class Car
	{
		public string id { get; set; }
		public List<Property> properties { get; set; }

		public void set (string cid) { id = cid; }

		public bool mod(string name, string value, bool replace)
		{
			int index = properties.FindIndex(p => p.Name == name);

			if (-1 == index)
				properties.Add ( new Property() { Name=string.Copy(name), Value=string.Copy(value) });
			else if (replace && (properties[index].Value != value))
				properties[index].Value = string.Copy(value);
			else return false;

			return true;
		}

		internal List<Property> Pclone(List<Property> prop)
        {
            List<Property> foo = new List<Property> { };
            foreach(Property p in prop)
                foo.Add(p.Clone());
            return foo;
        }

		public Car Clone()
		{
			Car c = new Car(){};
			c.id = string.Copy(id);
			c.properties = Pclone(properties);
			return c;
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
			if(!defaults.mod(name, value, false))
				return false;

			foreach (Car c in Clist)
				c.mod(name, value, false);

			return true;
		}

		// add or replace property values
		public bool mod(Car car)
		{
			bool changed;
			int index = Clist.FindIndex(c => c.id == car.id);

			if (changed = -1 == index)
				Clist.Add(car.Clone());
			else foreach (Property p in car.properties)
					changed = Clist[index].mod(p.Name, p.Value, true) || changed;
			return changed;
		}
	}

	public class Games
	{
		public List<Game> Glist { get; set; }

		public bool append (Property prop)
		{
			bool changed = false;

			if (null != Glist)
				foreach(Game g in Glist)
					changed = g.append(prop.Name, prop.Value) || changed;

			return changed;
		}

		public bool mod(Game game)
		{
			bool changed;

			int index = Glist.FindIndex(g => g.name == game.name);

			if (changed = -1 == index)
				Glist.Add(game);
			else foreach (Car c in game.Clist)
				changed = Glist[index].mod(c) || changed;
			return changed;
		}

		Game gamen(string gname, Car car)
		{
			List<Car> carl = new List<Car>() { car.Clone() };
			car.id = "default";
			return new Game { name=gname, Clist=carl, defaults = car };
		}

		// typically called just before updating DataPlugin.current
		internal bool Save_Car(Car car, string gname)
		{
			bool changed = true;
			int gndex;

			if (null == car || null == car.id || 0 == car.id.Length)
				return false;									// nothing to save

			if (null == Glist)
				Glist = new List<Game>() { gamen(gname, car) }; // first time for everything
			else
			{													// search for game
				gndex = Glist.FindIndex(g => g.name == gname);

				if (-1 == gndex) 								// first car for this game
					Glist.Add(gamen(gname, car));
				else changed = Glist[gndex].mod(car);
			}
			return changed;
		}
	}
}
