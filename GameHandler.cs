using System.Collections.Generic;
using System.ComponentModel;

namespace blekenbleu
{
	// programatically define DataGrid columns
	// https://wpf-tutorial.com/datagrid-control/custom-columns/
	public class Values : INotifyPropertyChanged	// https://stackoverflow.com/questions/26871641/how-to-refresh-a-window-in-c-wpf
	{
		private string _Default = "default", _Current = "current", _Previous = "previous";

		public event PropertyChangedEventHandler PropertyChanged;

		// Create the OnPropertyChanged method to raise the event
		protected void OnPropertyChanged(string value)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(value));
		}

		public string Name { get; set; }	// should not change
		public string Current
		{
			get { return _Current; }
			set
			{
				if (string.Compare(_Current, value) != 0)
				{
					_Current = value;
					OnPropertyChanged("Current");
				}
			}
		}
		public string Default
		{
			get { return _Default; }
			set
			{
				if (string.Compare(_Default, value) != 0)
				{
					_Default = value;
					OnPropertyChanged("Default");
				}
			}
		}
		public string Previous
		{
			get { return _Previous; }
			set
			{
				if (string.Compare(_Previous, value) != 0)
				{
					_Previous = value;
					OnPropertyChanged("Previous");
				}
			}
		}
	}	// class Values

//	New JSON structure ------------------------
	internal class CarL
	{
		public string carID { get; set; }
		public List<string> vList { get; set; }	// property values
	}

	internal class GameList
	{
		internal string gName;			// game name
		internal List<string> defaults;	// default property values
		internal List<CarL> cList;
	}

	internal class GamesList
	{
		internal string Plugin;			// Plugin name ("JSONio")
		internal List<string> pList;	// property names, from JSONio.ini
		internal List<GameList> gList;
	}

	public class Property		// must be public for DataPluginSettings
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}

	// return true if any Game changes
	public class Car
	{
		public string carID { get; set; }
		public List<Property> properties { get; set; }
	}

	public class Game
	{
		public string name { get; set; }
		public List<Property> defaults { get; set; }
		public List<Car> Clist { get; set; }

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

		internal Car Clone(Car car) => new Car() { carID = string.Copy(car.carID), properties = Clone(car.properties) };

		// Append or replace a single car property
		internal bool Mod(Car c, string name, string value, bool replace)
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
		internal bool Mod(Game g, Car car)
		{
			bool changed;
			int index = g.Clist.FindIndex(c => c.carID == car.carID);

			if (changed = -1 == index)
				g.Clist.Add(Clone(car));
			else foreach (Property p in car.properties)
					changed = Mod(g.Clist[index], p.Name, p.Value, true) || changed;
			return changed;
		}

		internal List<Property> Pclone(Car c)
		{
			List<Property> prop = new List<Property> { };
			foreach(Property p in c.properties)
				prop.Add(Clone(p));
			return prop;
		}
 
		// Append a new property to all cars in all games
		internal bool Append (Property prop)
		{
			bool changed = false;

			if (null != data.Glist)
				foreach(Game g in data.Glist)
					changed = Append(g, prop.Name, prop.Value) || changed;

			return changed;
		}

		// add (if missing) a value to Car properties in a game,
		// but do not replace value if present
		internal bool Append (Game g, string name, string value)
		{
			Car dcar = new Car() { properties = g.defaults };
			if(!Mod(dcar, name, value, false))
				return false;

			foreach (Car c in g.Clist)
				Mod(c, name, value, false);

			return true;
		}

		// called when changing cars or games
		internal bool Save_Car(Car car, string gname)
		{
			bool changed = true;

			if (null == car || null == car.carID || 0 == car.carID.Length)
				return false;									// nothing to save

																// search for game
			int gndex = data.Glist.FindIndex(g => g.name == gname);

			if (-1 == gndex)	 								// first car for this game
				data.Glist.Add(New_Game(gname, car));
			else changed = Mod(data.Glist[gndex], car);

			return changed;
		}
	}	// class GameHandler
}
