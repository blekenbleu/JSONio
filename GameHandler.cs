using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using System;
using System.Collections.Generic;

namespace blekenbleu
{
	public class Property		// must be public for DataPluginSettings
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}

	// return true if any Game changes
	public class Car
	{
		public string id { get; set; }
		public List<Property> properties { get; set; }
	}

	public class Game
	{
		public string name { get; set; }
		public List<Property> defaults { get; set; }
		public List<Car> Clist { get; set; }

	}

    // programatically define DataGrid columns
    // https://wpf-tutorial.com/datagrid-control/custom-columns/
    public class SimProp
    {
        public string Name { get; set; }
        public string Default { get; set; }
        public string Current { get; set; }
        public string Previous { get; set; }
    }

	public class Games
	{
		public string name { get; set; }
		public List<Game> Glist { get; set; }
	}

	internal class GameHandler
	{
		internal Games data;

		internal Game New_Game(string gname, Car car)
		{
			return new Game()
			{
				name = string.Copy(gname),
				defaults = Pclone(car),
				Clist = new List<Car>() { Clone(car) }
			};
		}
 
		// Implement deep copy
		internal Property Clone(Property p)
		{
			return new Property()
			{
				Name = string.Copy(p.Name),
				Value = string.Copy(p.Value)
			};
		}

		internal List<Property> Clone(List<Property> l)
		{
			var nl = new List<Property>() { };

			foreach (Property p in l)
				nl.Add(Clone(p));
			return nl;
		}

		internal Car Clone(Car car) => new Car() { id = string.Copy(car.id), properties = Clone(car.properties) };

		// append or replace a single car property
		internal bool mod(Car c, string name, string value, bool replace)
		{
			int index = c.properties.FindIndex(p => p.Name == name);

			if (-1 == index)
				c.properties.Add ( new Property() { Name=string.Copy(name), Value=string.Copy(value) });
			else if (replace && (c.properties[index].Value != value))
				c.properties[index].Value = string.Copy(value);
			else return false;

			return true;
		}

		// add or replace any property values for a car in a game
		internal bool mod(Game g, Car car)
		{
			bool changed;
			int index = g.Clist.FindIndex(c => c.id == car.id);

			if (changed = -1 == index)
				g.Clist.Add(Clone(car));
			else foreach (Property p in car.properties)
					changed = mod(g.Clist[index], p.Name, p.Value, true) || changed;
			return changed;
		}

		internal List<Property> Pclone(Car c)
		{
			List<Property> prop = new List<Property> { };
			foreach(Property p in c.properties)
				prop.Add(Clone(p));
			return prop;
		}
 
		// append a new property to all cars in all games
		internal bool append (Property prop)
		{
			bool changed = false;

			if (null != data.Glist)
				foreach(Game g in data.Glist)
					changed = append(g, prop.Name, prop.Value) || changed;

			return changed;
		}

		// add (if missing) a value to Car properties in a game,
		// but do not replace value if present
		internal bool append (Game g, string name, string value)
		{
			Car dcar = new Car() { properties = g.defaults };
			if(!mod(dcar, name, value, false))
				return false;

			foreach (Car c in g.Clist)
				mod(c, name, value, false);

			return true;
		}

		// called when changing cars or games
		internal bool Save_Car(Car car, string gname)
		{
			bool changed = true;

			if (null == car || null == car.id || 0 == car.id.Length)
				return false;									// nothing to save

																// search for game
			int gndex = data.Glist.FindIndex(g => g.name == gname);

			if (-1 == gndex)	 								// first car for this game
				data.Glist.Add(New_Game(gname, car));
			else changed = mod(data.Glist[gndex], car);

			return changed;
		}
	}
}
