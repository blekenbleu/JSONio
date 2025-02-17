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

		// called in End()
		public void Data()
		{
			data = new GamesList()
			{
				Plugin = "JSONio",
				gList = new List<GameList>() { },	// GameList @ slim.cs line 16
				// property names
				pList = new List<string> { }		// per-car, then per-game
			};
			for (int i = 0; i < JSONio.gCount; i++)
				data.pList.Add(js.simValues[i].Name);
		}

		// Reconcile .json values with simValues based on .ini and Settings
		private List<string> Reconcile(List<string> vList, int car)
		{
			List<string> New = new List<string> {};
			// car[0] is per-game car default and per-game property values
			int count = (0 == car) ? JSONio.gCount : JSONio.pCount;

			for (int i = 0; i < count; i++)
			{
				int Index =  data.pList.FindIndex(j => j == js.simValues[i].Name);

				if (-1 == Index || Index >= vList.Count)
					New.Add(string.Copy(js.simValues[i].Default));
				else New.Add(string.Copy(vList[Index]));	// reuse as many as possible
			}
			return New;
		}

		// load Slim .json and reconcile with CurrentCar-specific simValues from NCalcScripts/JSONio.ini
		// return true if path fails or unrecoverable JSON
		// .ini may have added, deleted or moved properties among per-car, per-game and global
		// .json may be e.g. obsolete format, out-of-date or bad because JSONio code bugs.
		internal bool Load(string path)
		{
			if (!File.Exists(path))
				return true;

			data = JsonConvert.DeserializeObject<GamesList>(File.ReadAllText(path));
			if (null == data)
			{
				JSONio.Msg = "null data";
				return true;
			}

			if (null == data.pList)
			{
				JSONio.Msg = "null data.pList";
				return true;
			}

			if (null == data.gList)
			{
				JSONio.Msg = "null data.gList";
				return true;
			}

			// Now, can only return false, meaning data fully reconciled to simValues

			if (null == data.Plugin || "JSONio" != data.Plugin) {
				js.OOpa($"Slim.Load({path}) data.Plugin: {data.Plugin} != JSONio");
				data.Plugin = "JSONio";	// user has at least been warned...
			}

			int nullcarID = 0;
			int pCount = JSONio.pCount;
			int gCount = JSONio.gCount;
			int i, g, c;

			if (gCount != data.pList.Count)
				i = -1;
			else for (i = 0; i < data.pList.Count; i++)
				if (data.pList[i] != js.simValues[i].Name)
					break;

			if (i == gCount)
				for (g = 0; g < data.gList.Count; g++)
					if(data.gList[g].cList.Count < 2 || data.gList[g].cList[0].Vlist.Count != gCount)
					{ i--; break; }
					else for (c = 0; c < data.gList[g].cList.Count; c++)
						if (data.gList[g].cList[c].Vlist.Count != ((0 == c) ? gCount : pCount))
						{ i--; g = data.gList.Count; break; }

			if (i != gCount)
			// repopulate car properties according to simValues
			{
				js.OOpa($"Slim.Load({path}):  pList mismatch");
				if (i != pCount)
					for (i = 0; i < data.gList.Count; i++)					// all games
					{
						for (c = 0; c < data.gList[i].cList.Count; c++)	// all cars in game
							if (null == data.gList[i].cList[c].Name)
							{
								nullcarID++;
								data.gList[i].cList.RemoveAt(c--);
							}
							else data.gList[i].cList[c].Vlist = Reconcile(data.gList[i].cList[c].Vlist, c);
					}
				data.pList = new List<string> {};
				for (i = 0; i < gCount; i++)
					data.pList.Add(string.Copy(js.simValues[i].Name));
			}
			if (0 < nullcarID)
				js.OOpa($"Slim.Load({path}): {nullcarID} null carIDs");

			if (data.gList.Count < 1 || data.gList[0].cList.Count < 2)
				js.OOpa($"Slim.Load({path}): empty data.gList");

			return false;
		}	// Load()
	}		// class Slim
}
