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
		public string Name { get; set; }
		public List<string> Vlist { get; set; }	// property values
	}

	public class GameList
	{
		public List<CarL> cList;		// first CarL is game Name + default property values
	}

	public class GamesList
	{
		public string Plugin;			// Plugin Name ("JSONio")
		public List<string> pList;	// property names, from JSONio.ini
		public List<GameList> gList;
	}

	public class Slim
	{
		public GamesList data;

		private List<string> Refactor(List<Values> simprops, List<string> properties)
		{
			List<string> New = new List<string> {};

			for (int i = 0; i < JSONio.pCount; i++)
			{
				int Index =  data.pList.FindIndex(j => j == simprops[i].Name);
				New.Add(string.Copy((-1 == Index) ? simprops[i].Default : properties[Index]));
			}
			return New;
		}

		// load Slim .json and reconcile with CurrentCar-specific simprops from NCalcScripts/JSONio.ini
		internal bool Load(string path, List<Values> simprops)
		{
			if (!File.Exists(path))
				return false;

			data = JsonConvert.DeserializeObject<GamesList>(File.ReadAllText(path));
			if (null == data || null == data.Plugin || null == data.pList || null == data.gList)
				return !JSONio.Info($"Slim.Load({path}):  bad data");

			int nullcarID = 0;
			int pCount = JSONio.pCount;
			int i = -1;

			if (pCount == data.pList.Count)
				for (i = 0; i < pCount; i++)
					if (data.pList[i] != simprops[i].Name)
						break;

			if (i != pCount) // repopulate car properties according to NCalcScripts/JSONio.ini
			{
				JSONio.Info($"Slim.Load({path}):  pList mismatched NCalcScripts/JSONio.ini");
				for (i = 0; i < data.gList.Count; i++)
				{
					for (int c = 0; c < data.gList[i].cList.Count; c++)
						if (null == data.gList[i].cList[c].Name)
						{
							nullcarID++;
							data.gList[i].cList.RemoveAt(c--);
						}
						else data.gList[i].cList[c].Vlist = Refactor(simprops, data.gList[i].cList[c].Vlist);
				}
				data.pList = new List<string> {};
				for (i = 0; i < pCount; i++)
					data.pList.Add(string.Copy(simprops[i].Name));
			}
			if (0 < nullcarID)
				JSONio.Info($"Slim.Load({path}): {nullcarID} null carIDs");

			return false;
		}

		private List<string> pCopy (List<Property> p)
		{
			List<string> l = new List<string> {};
			for (int i = 0; i < p.Count; i++)
				l.Add(string.Copy(p[i].Value));
			return l;
		}

		internal GamesList Migrate(GameHandler g)
		{
			GamesList gsl = new GamesList { Plugin = "JSONio", pList = new List<string> {}, gList  = new List<GameList>{} };
			if (0 < g.data.Glist.Count)
			{
				int i, k, c, v, pc;

				for (i = 0; i < (pc = g.data.Glist[0].defaults.Count); i++)
					gsl.pList.Add(string.Copy(g.data.Glist[0].defaults[i].Name));
				for (k = 0; k < g.data.Glist.Count; k++)
				{
					if (0 == g.data.Glist[k].Clist.Count)
						continue;

					GameList gl = new GameList
					{
						cList = new List<CarL> {
							new CarL { Name = g.data.Glist[k].name,
										Vlist = pCopy(g.data.Glist[k].defaults) } }
					};
					for (c = 0; c < g.data.Glist[k].Clist.Count; c++)
					{
						CarL car = new CarL
						{
							Name = string.Copy(g.data.Glist[k].Clist[c].carID),
							Vlist = new List<string> {}
						};
						for (v = 0; v < pc; v++)
							car.Vlist.Add(string.Copy(g.data.Glist[k].Clist[c].properties[v].Value));
						gl.cList.Add(car);
					}
					gsl.gList.Add(gl);
				}
			}
			return gsl;
		}

		bool Mod(int gi, int ci, CarL c)
		{
			bool ch = false;

			for (int i = 0; i < JSONio.pCount; i++)
				if (data.gList[gi].cList[ci].Vlist[i] != c.Vlist[i])
				{
					ch = true;
					data.gList[gi].cList[ci].Vlist[i] = string.Copy(c.Vlist[i]);
		   		}
			return ch;
		}

		List<string> CCopy(List<Values> v)
		{
			List<string> New = new List<string> { };
			for (int i = 0; i < JSONio.pCount; i++) { New.Add(string.Copy(v[i].Current)); }
			return New;
		}

		List<string> DCopy(List<Values> v)
		{
			List<string> New = new List<string> { };
			for (int i = 0; i < v.Count; i++) { New.Add(string.Copy(v[i].Default)); }
			return New;
		}

		// called when changing cars or games
		internal bool Save_Car(CarID car, List<Values> props, string gname)
		{
			bool changed;

			if (null == car || null == car.ID || 0 == JSONio.pCount || JSONio.pCount > props.Count)
				return false;									// nothing to save

			// search for game
			int gndex = data.gList.FindIndex(g => g.cList[0].Name == gname);
			if (0 > gndex)	 									// first car for this game?
			{
				changed = true;
				gndex = data.gList.Count;
				data.gList.Add(new GameList
				{
					cList = new List<CarL> { new CarL { Name = string.Copy(gname), Vlist = DCopy(props) } }
				});
			}
			// defaults may have been changed
			else changed = Mod(gndex, 0, new CarL { Name = gname, Vlist = DCopy(props) });

			CarL newc = new CarL { Name = string.Copy(car.ID), Vlist = CCopy(props)};
			int cndex = data.gList[gndex].cList.FindIndex(c => c.Name == car.ID);
			if (-1 == cndex)
			{
				changed = true;
				data.gList[gndex].cList.Add(newc);
			}
			else changed = Mod(gndex, cndex, newc) || changed;
			return changed;
		}

		internal int Car_Change(out int gi, string Gnew, string Cname)
		{
			gi = (0 < Gnew.Length) ? data.gList.FindIndex(g => g.cList[0].Name == Gnew) : -1;
			return (0 <= gi) ? data.gList[gi].cList.FindIndex(c => c.Name == Cname) : -1;
		}
	}		// class Slim

// For original JSONio ---------------------------------------------------------------------
	internal class CarID
	{
		public string ID { get; set; }
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

		// so far as GameHandler is concerned, each car has JSONio.pCount properties
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

		// add or replace any property values for a car in a game
		private bool Mod(Game game, Car car)
		{
			bool changed;
			int index = game.Clist.FindIndex(c => c.carID == car.carID);
			int gndx = data.Glist.FindIndex(f => f.name == game.name);

			if (changed = -1 == index)
				data.Glist[gndx].Clist.Add(Clone(car));
			else for (int i = 0; i < JSONio.pCount; i++)
				if (game.Clist[index].properties[i].Value != car.properties[i].Value)
				{
					changed = true;
					data.Glist[gndx].Clist[index].properties[i].Value = string.Copy(car.properties[i].Value);
				}
			return changed;
		}

		private Car NewCar(CarID c, List<Property> props)
		{
			Car car = new Car() { properties = new List<Property> {}, carID = c.ID };
			for (int i = 0; i < JSONio.pCount; i++)
				car.properties.Add(new Property { Name = string.Copy(props[i].Name), Value = string.Copy(props[i].Value) });
			return car;
		}

		// called when changing cars or games
		internal bool Save_Car(CarID car, List<Values> val, string gname)
		{
			if (null == car || null == car.ID || 0 == JSONio.pCount || JSONio.pCount > val.Count)
				return false;									// nothing to save

			bool changed;
			int i, gndex = data.Glist.FindIndex(g => g.name == gname); // search for game
			List<Property> props = new List<Property> {};
			for (i = 0; i < JSONio.pCount; i++)
				props.Add(new Property { Name = val[i].Name, Value = val[i].Current });

			if (changed = 0 > gndex)	 								// first car for this game
			{
				data.Glist.Add(New_Game(gname, NewCar(car, props)));
				gndex = data.Glist.Count - 1;
			}
			else changed = Mod(data.Glist[gndex], NewCar(car, props));
			// defaults may have changed
			for (i = 0; i < JSONio.pCount; i++)
				if (data.Glist[gndex].defaults[i].Value != val[i].Default)
				{
					changed = true;
					data.Glist[gndex].defaults[i].Value = string.Copy(val[i].Default);
				}

			return changed;
		}

		internal int Car_Change(out int gi, string Gnew, string Cname)
		{
			gi = (0 < Gnew.Length) ? data.Glist.FindIndex(g => g.name == Gnew) : -1;
			return (0 <= gi) ? data.Glist[gi].Clist.FindIndex(c => c.carID == Cname) : -1;
		}
	}	// class GameHandler
}
