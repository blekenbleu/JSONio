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
		readonly JSONio js;

        public Slim(JSONio plugin)
        {
            this.js = plugin;
        }

		// Reconcile .json values with simValues based on .ini and Settings
        private List<string> Reconcile(List<string> vList, int car)
		{
			List<string> New = new List<string> {};
			// car[0] is per-game car default and per-game property values
			int count = (0 == car) ? JSONio.pCount : JSONio.gCount;

			for (int i = 0; i < JSONio.pCount; i++)
			{
				int Index =  data.pList.FindIndex(j => j == js.simValues[i].Name);

				if (-1 == Index)
				{
					js.changed = true;
					New.Add(string.Copy(js.simValues[i].Default));
				}
				else New.Add(string.Copy(vList[Index]));
			}
			return New;
		}

		// load Slim .json and reconcile with CurrentCar-specific simValues from NCalcScripts/JSONio.ini
		internal bool Load(string path)
		{
			if (!File.Exists(path))
				return false;

			data = JsonConvert.DeserializeObject<GamesList>(File.ReadAllText(path));
			if (null == data)
				return js.OOpa($"Slim.Load({path}):  null data");
			if (null == data.Plugin)
			{
				js.OOpa($"Slim.Load({path}):  null data.Plugin");
				data.Plugin = "JSONio";
				js.changed = true;
			}
			if ("JSONio" != data.Plugin) {
				js.OOpa($"Slim.Load({path}) data.Plugin: {data.Plugin} != JSONio");
				data.Plugin = "JSONio";
				js.changed = true;
			}
			if (null == data.pList)
			{
				js.changed = false;
				return js.OOpa($"Slim.Load({path}):  null data.pList");
			}
			if (null == data.gList)
			{
                js.changed = false;
                return js.OOpa($"Slim.Load({path}):  null data.gList");
			}
			int nullcarID = 0;
			int pCount = JSONio.pCount;
			int gCount = JSONio.gCount;
			int i = -1;

			if (gCount == data.pList.Count)		// data.pList has both per-car and per-game
				for (i = 0; i < pCount; i++)
					if (data.pList[i] != js.simValues[i].Name)
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
							else data.gList[i].cList[c].Vlist = Reconcile(data.gList[i].cList[c].Vlist, c);
					}
				if (gCount != data.pList.Count)
					data.pList = new List<string> {};
					for (i = 0; i < gCount; i++)
						data.pList.Add(string.Copy(js.simValues[i].Name));
			}
			if (0 < nullcarID)
				js.OOpa($"Slim.Load({path}): {nullcarID} null carIDs");

			if (data.gList.Count < 1 || data.gList[0].cList.Count < 2)
				js.OOpa($"Slim.Load({path}): empty data.gList");

			return true;
		}	// Load()

		// game defaults (0 == ci) or per-car values
		internal void Mod(int gi, int ci, List<string> vList)
		{
			int count = vList.Count;

			for (int i = 0; i < count; i++)
				if (data.gList[gi].cList[ci].Vlist[i] != vList[i])
				{
					js.changed = true;
					data.gList[gi].cList[ci].Vlist[i] = string.Copy(vList[i]);
		   		}
		}
	}		// class Slim
}
