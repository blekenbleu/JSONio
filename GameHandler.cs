using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace blekenbleu
{
	// programatically define DataGrid columns
	// https://wpf-tutorial.com/datagrid-control/custom-columns/
	public class Values : INotifyPropertyChanged	// https://stackoverflow.com/questions/26871641/how-to-refresh-a-window-in-c-wpf
	{
		private string _Default = "default", _Current = "current", _Previous = "previous";

		public event PropertyChangedEventHandler PropertyChanged;
		private PropertyChangedEventArgs Cevent = new PropertyChangedEventArgs("Current");
		private PropertyChangedEventArgs Devent = new PropertyChangedEventArgs("Default");
		private PropertyChangedEventArgs Pevent = new PropertyChangedEventArgs("Previous");

		public string Name { get; set; }	// should not change
		public string Current
		{
			get { return _Current; }
			set
			{
				if (string.Compare(_Current, value) != 0)
				{
					_Current = value;
					PropertyChanged?.Invoke(this, Cevent);
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
					PropertyChanged?.Invoke(this, Devent);
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
					PropertyChanged?.Invoke(this, Pevent);
				}
			}
		}
	}	// class Values

// New slim JSON structure ------------------------------------------
// These must all be declared public for JsonConvert.SerializeObject()
	public class CarL
	{
		public string carID { get; set; }
		public List<string> vList { get; set; }	// property values
	}

	public class GameList
	{
		public string gName;			// game name
		public List<string> defaults;	// default property values
		public List<CarL> cList;
	}

	public class GamesList
	{
		public string Plugin;			// Plugin name ("JSONio")
		public List<string> PropertyL;	// property names, from JSONio.ini
		public List<GameList> GameL;
	}

	public class Slim
	{
		public GamesList data;

		private List<string> Refactor(List<Values> simprops, List<string> properties)
		{
			List<string> New = new List<string> {};

			for (int i = 0; i < JSONio.pCount; i++)
			{
				int Index =  data.PropertyL.FindIndex(j => j == simprops[i].Name);
				New.Add(string.Copy((-1 == Index) ? simprops[i].Default : properties[Index]));
			}
			return New;
		}

		// load Slim .json and reconcile with car-specific simprops from NCalcScripts/JSONio.ini
		internal bool Load(string path, List<Values> simprops)
		{
			if (!File.Exists(path))
				return false;

			data = JsonConvert.DeserializeObject<GamesList>(File.ReadAllText(path));
			if (null == data || null == data.Plugin || null == data.PropertyL || null == data.GameL)
				return !JSONio.Info($"Slim.Load({path}):  bad data");

			int nullcarID = 0;
			int pCount = JSONio.pCount;
			int i = -1;

			if (pCount == data.PropertyL.Count)
				for (i = 0; i < pCount; i++)
					if (data.PropertyL[i] != simprops[i].Name)
						break;

			if (i != pCount) // repopulate Car properties according to NCalcScripts/JSONio.ini
			{
				JSONio.Info($"Slim.Load({path}):  PropertyL mismatched NCalcScripts/JSONio.ini");
				for (i = 0; i < data.GameL.Count; i++)
				{
					data.GameL[i].defaults = Refactor(simprops, data.GameL[i].defaults);
					for (int c = 0; c < data.GameL[i].cList.Count; c++)
						if (null == data.GameL[i].cList[c].carID)
						{
							nullcarID++;
							data.GameL[i].cList.RemoveAt(c--);
						}
						else data.GameL[i].cList[c].vList = Refactor(simprops, data.GameL[i].cList[c].vList);
				}
				data.PropertyL = new List<string> {};
				for (i = 0; i < pCount; i++)
					data.PropertyL.Add(string.Copy(simprops[i].Name));
			}
			if (0 < nullcarID)
				JSONio.Info($"Slim.Load({path}): {nullcarID} null carIDs");

			return false;
		}

		internal GamesList Migrate(GameHandler g)
		{
			GamesList gsl = new GamesList { Plugin = "JSONio", PropertyL = new List<string> {}, GameL  = new List<GameList>{} };
			if (0 < g.data.Glist.Count)
			{
				int i, k, c, v, pc;

				for (i = 0; i < (pc = g.data.Glist[0].defaults.Count); i++)
					gsl.PropertyL.Add(string.Copy(g.data.Glist[0].defaults[i].Name));
				for (k = 0; k < g.data.Glist.Count; k++)
				{
					GameList gl = new GameList { gName = g.data.Glist[k].name, defaults = new List<string> {}, cList = new List<CarL> {} };
						for (i = 0; i < pc; i++)
							gl.defaults.Add(string.Copy(g.data.Glist[k].defaults[i].Value));
						for (c = 0; c < g.data.Glist[k].Clist.Count; c++)
						{
							CarL car = new CarL { carID = string.Copy(g.data.Glist[k].Clist[c].carID),
															vList = new List<string> {} };
							for (v = 0; v < pc; v++)
								car.vList.Add(string.Copy(g.data.Glist[k].Clist[c].properties[v].Value));
							gl.cList.Add(car);
						}
					gsl.GameL.Add(gl);
				}
			}
			return gsl;
		}
	}

// For original JSONio ---------------------------------------------------------------------
	internal class CarID
	{
		public string ID { get; set; }
		public uint Length { get; set; }
	}

	public class Property		// must be public for DataPluginSettings
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}

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

	// return true if any Game changes
	internal class GameHandler
	{
		internal Games data;

		// so far as GameHandler is concerned, each Car has JSONio.pCount properties
		private Game New_Game(string gname, Car car)
		{
			return new Game()
			{
				name = string.Copy(gname),
				defaults = Clone(car.properties),
				Clist = new List<Car>() { Clone(car) }
			};
		}
 
		// Implement deep copy
		private Property Clone(Property p)
		{
			return new Property()
			{
				Name = string.Copy(p.Name),
				Value = string.Copy(p.Value)
			};
		}

		private List<Property> Clone(List<Property> l)
		{
			var nl = new List<Property>() { };

			for (int i = 0; i < JSONio.pCount; i++)
				nl.Add(Clone(l[i]));
			return nl;
		}

		private Car Clone(Car car) => new Car() { carID = string.Copy(car.carID), properties = Clone(car.properties) };

		// Append or replace a single car property
		private bool Mod(Car c, string name, string value, bool replace)
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
		private bool Mod(Game g, Car car)
		{
			bool changed;
			int index = g.Clist.FindIndex(c => c.carID == car.carID);

			if (changed = -1 == index)
				g.Clist.Add(Clone(car));
			else {
				List<Property> p = car.properties;
				for (int i = 0; i < JSONio.pCount; i++)
					changed = Mod(g.Clist[index], p[i].Name, p[i].Value, true) || changed;
			}
			return changed;
		}

		// Append a new property to all cars in all games
		private bool Append (Property prop)
		{
			bool changed = false;

			if (null != data.Glist)
				foreach(Game g in data.Glist)
					changed = Append(g, prop.Name, prop.Value) || changed;

			return changed;
		}

		// add (if missing) a value to Car properties in a game,
		// but do not replace value if present
		private bool Append (Game g, string name, string value)
		{
			Car dcar = new Car() { properties = g.defaults };
			if(!Mod(dcar, name, value, false))
				return false;

			foreach (Car c in g.Clist)
				Mod(c, name, value, false);

			return true;
		}

		private Car NewCar(CarID cid, List<Property> props)
		{
			Car car = new Car() { properties = new List<Property> {}, carID = cid.ID };
			for (int i = 0; i < cid.Length; i++)
				car.properties.Add(new Property { Name = props[i].Name, Value = props[i].Value });
			return car;
		}

		// called when changing cars or games
		internal bool Save_Car(CarID car, List<Property> props, string gname)
		{
			bool changed = true;

			if (null == car || null == car.ID || 0 == car.Length || car.Length > props.Count)
				return false;									// nothing to save

																// search for game
			int gndex = data.Glist.FindIndex(g => g.name == gname);

			if (-1 == gndex)	 								// first car for this game
				data.Glist.Add(New_Game(gname, NewCar(car, props)));
			else changed = Mod(data.Glist[gndex], NewCar(car, props));

			return changed;
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
