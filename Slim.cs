using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace blekenbleu.jsonio
{
// New slim JSON structure ------------------------------------------
// These must all be declared public for JsonConvert.SerializeObject()
	public class CarL
	{
		public string Name { get; set; }		// CarID (game name for Carl[0])
		public List<string> Vlist { get; set; }	// property values	(game defaults for Carl[0])
	}

	public class GameList
	{
		public List<CarL> cList;		// cList[0] is game Name + default per-car, then per-game property values
	}

	public class GamesList
	{
		public string Plugin;			// Plugin Name ("JSONio")
		public List<string> pList;		// per-car, then per-game property names, from JSONio.ini
		public List<GameList> gList;
	}

	public class Slim
	{
		public GamesList data;
		private readonly JSONio js;
        public Slim(JSONio plugin)
        {
            this.js = plugin;
        }

        private List<string> Refactor(List<Values> simprops, List<string> properties, int car)
		{
			List<string> New = new List<string> {};
			// car[0] is per-game car default and per-game property values
			int count = (0 == car) ? JSONio.pCount : JSONio.gCount;

			for (int i = 0; i < JSONio.pCount; i++)
			{
				int Index =  data.pList.FindIndex(j => j == simprops[i].Name);
				New.Add(string.Copy((-1 == Index) ? simprops[i].Default : properties[Index];
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
				return !js.OOpa($"Slim.Load({path}):  bad data");

			int nullcarID = 0;
			int pCount = JSONio.pCount;
			int gCount = JSONio.gCount;
			int i = -1;

			if (gCount == data.pList.Count)		// data.pList has both per-car and per-game
				for (i = 0; i < pCount; i++)
					if (data.pList[i] != simprops[i].Name)
						break;

			if (i != pCount || gCount != data.pList.Count) // repopulate car properties according to NCalcScripts/JSONio.ini
			{
				js.OOpa($"Slim.Load({path}):  pList mismatched NCalcScripts/JSONio.ini");
				if (i != pCount)
					for (i = 0; i < data.gList.Count; i++)					// all games
					{
						for (int c = 0; c < data.gList[i].cList.Count; c++)	// all cars in game
							if (null == data.gList[i].cList[c].Name)
							{
								nullcarID++;
								data.gList[i].cList.RemoveAt(c--);
							}
							else data.gList[i].cList[c].Vlist = Refactor(simprops, data.gList[i].cList[c].Vlist, c);
					}
				if (gCount != data.pList.Count)
					data.pList = new List<string> {};
					for (i = 0; i < gCount; i++)
						data.pList.Add(string.Copy(simprops[i].Name));
			}
			if (0 < nullcarID)
				js.OOpa($"Slim.Load({path}): {nullcarID} null carIDs");

			return (data.gList.Count > 0 && data.gList[0].cList.Count > 1);
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
		internal bool Save_Car(string car, List<Values> props, string gname)
		{
			bool changed;

			if (null == car || 0 == JSONio.pCount || JSONio.pCount > props.Count)
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

			CarL newc = new CarL { Name = string.Copy(car), Vlist = CCopy(props)};
			int cndex = data.gList[gndex].cList.FindIndex(c => c.Name == car);
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
}
