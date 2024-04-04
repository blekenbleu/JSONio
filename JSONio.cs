using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Media;

namespace blekenbleu
{
	[PluginDescription("game&car-specific properties to/from JSON")]
	[PluginAuthor("blekenbleu")]
	[PluginName("JSONio plugin")]
	public class JSONio : IPlugin, IDataPlugin, IWPFSettingsV2
	{
		public DataPluginSettings Settings;
		internal static readonly string My = "JSONio."; 
		internal static readonly string Ini = "DataCorePlugin.ExternalScript." + My;	// configuration source
		private string path;															// file locations
		private bool changed;
		private GameHandler games;
		public string Selected_Property = "unKnown";
		public byte Select = 0;
		internal string gname = "";
		private static List<Property> previous;
		internal static List<int>steps;
		internal static Car current;

		internal List<Property> Pclone(List<Property> prop)			// deep copy
		{
			List<Property> Plist = new List<Property> {};
			foreach(Property p in prop)
				if (null != p.Name && null != p.Value)
					Plist.Add(new Property() { Name = string.Copy(p.Name), Value = string.Copy(p.Value) });
			return Plist;
		}

		void cCopy(List<Property> Plist)
		{
			for(int c = 0; c < current.properties.Count; c++)
			{
				int Index = Plist.FindIndex(i => i.Name == current.properties[c].Name);
				if (-1 != Index)
					current.properties[c].Value = Plist[Index].Value;
			}
		}

		internal List<Property> Pclone(Car car)	=> Pclone(car.properties);

		/// <summary>
		/// Plugin-specific wrapper for SimHub.Logging.Current.Info();
		/// </summary>
		internal static bool Info(string str)
		{
			SimHub.Logging.Current.Info(JSONio.My + str);   // bool Info()
			return true;
		}

		/// <summary>
		/// Instance of the current plugin manager
		/// </summary>
		public PluginManager PluginManager { get; set; }

		/// <summary>
		/// Gets the left menu icon. Icon must be 24x24 and compatible with black and white display.
		/// </summary>
		public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);

		/// <summary>
		/// Short plugin title to show in left menu. Return null to use the PluginName attribute.
		/// </summary>
		public string LeftMenuTitle => "JSONio plugin";

		/// <summary>
		/// Called one time per game data update, contains all normalized game data,
		/// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
		///
		/// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
		///
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <param name="data">Current game data, including current and previous data frame.</param>
		public void DataUpdate(PluginManager pluginManager, ref GameData data)
		{
/*
			// Define the value of our property (declared in Init())
			if (data.GameRunning)
			{
				if (data.OldData != null && data.NewData != null)
				{
					if (data.OldData.SpeedKmh < 100 && data.OldData.SpeedKmh >= 100)
					{
						// Trigger an event
						this.TriggerEvent("SpeedWarning");
					}
				}
			}
 */
		}

		/// <summary>
		/// Called at plugin manager stop, close/dispose anything needed here !
		/// Plugins are rebuilt at game change
		/// </summary>
		/// <param name="pluginManager"></param>
		public void End(PluginManager pluginManager)
		{
			// Save settings
			if (0 < gname.Length) {
				Settings.properties = Pclone(current);
				this.SaveCommonSettings("GeneralSettings", Settings);
			}
			if (games.Save_Car(current, gname) || changed)
			{
				string js = JsonConvert.SerializeObject(games.data, Formatting.Indented);

				if ((0 == js.Length || "{}" == js) && 0 < games.data.Glist.Count)
					Info("End():  Json Serializer failure for games.data");
				else File.WriteAllText(path, js);
			}
		}

		/// <summary>
		/// Returns the settings control, return null if no settings control is required
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <returns></returns>
		public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
		{
			return new SettingsControl(this);
		}

		public void ment(int sign, string prefix)
		{
			if (0 == gname.Length || 0 == current.id.Length)
				return;
			int step = steps[Select];
			int iv = (int)(0.004 + 100 * float.Parse(current.properties[Select].Value));

			iv += sign * step;
			if (0 <= iv)
			{
				if (0 != step % 100)
					current.properties[Select].Value = $"{(float)(0.01 * iv)}";
				else current.properties[Select].Value = $"{(int)(0.004 + 0.01 * iv)}";
				Info("property " + current.properties[Select].Name + " " + prefix + $"cremented to {current.properties[Select].Value}");
				changed = true;
			}
		}

		public void select(bool next)
		{
			if (0 == gname.Length || 0 == current.id.Length)
				return;

			if (next && ++Select >= current.properties.Count)
				Select = 0;
			if (!next)
			{
				if (0 < Select)
					Select--;
				else Select = (byte)(current.properties.Count - 1);
			}
			Selected_Property = current.properties[Select].Name;
//			Info("Selected property = " + Selected_Property);
		}

		public void swap()
		{
			List<Property> temp = Pclone(previous);

			previous = Pclone(current.properties);
			cCopy(temp);
		}

		public void new_defaults()
		{
			if (0 == gname.Length)
				return;

			int Index = games.data.Glist.FindIndex(i => i.name == gname);

			if (-1 != Index)	
				games.data.Glist[Index].defaults = Pclone(current.properties);
		}

		/// <summary>
		/// Called once after plugins startup
		/// Plugins are rebuilt at game change
		/// </summary>
		/// <param name="pluginManager"></param>
		public void Init(PluginManager pluginManager)
		{
			changed = false;	// write JSON file during End() only if true

			// Load properties from settings
			Settings = this.ReadCommonSettings<DataPluginSettings>("GeneralSettings", () => new DataPluginSettings());
			games = new GameHandler()
			{
				data = new Games()
				{
					name = "JSONio",
					Glist = new List<Game>() {}
				}
			};
			current = new Car() {id = "", properties = Pclone(previous = Pclone(Settings.properties)) };
			steps = new List<int>() { } ;

/*	Hack to force settings
			current.properties = null;
			foreach (Property p in previous)
			{
				if ("offset" == p.Name)
					continue;
				if ("gamma" == p.Name)
					p.Value = "5";
				else if ("threshold" == p.Name)
					p.Value = "10";
				current.properties.Add(p);
			}
			previous = Pclone(current);
 */

			// Load existing JSON
			path = pluginManager.GetPropertyValue(Ini + "file")?.ToString();
			if (File.Exists(path))
			{
				Games foo = JsonConvert.DeserializeObject<Games>(File.ReadAllText(path));

				if (null != foo && null != foo.name && null != foo.Glist)
					games.data = foo;
				else changed = Info($"Init():  empty or invalid {path}");
			}
			else changed = Info("Init()");

			// Load properties from JSONio.ini
			string pts, ds = pluginManager.GetPropertyValue(pts = Ini + "properties")?.ToString();
			string vts, vs = pluginManager.GetPropertyValue(vts = Ini + "values")?.ToString();
			string sts, ss = pluginManager.GetPropertyValue(sts = Ini + "steps")?.ToString();
			if (!(null == ds && Info($"Init(): '{pts}' not found")
			   && null == vs && Info($"Init(): '{vts}' not found")
			   && null == ss && Info($"Init(): '{sts}' not found")
				 ))
			{
				List<Property> init = new List<Property>() {};
				List<Property> temp = new List<Property>() {};
				List<string> props, vals, stps;
				string s = "";

				props = new List<string>(ds.Split(','));
				vals = new List<string>(vs.Split(','));
				stps = new List<string>(ss.Split(','));
				if (props.Count != vals.Count || props.Count != stps.Count)
					Info($"Init(): {props.Count} properties;  {vals.Count};  {stps.Count} steps");
				int count = props.Count;

				if (count > vals.Count)
					count = vals.Count;
				if (count > stps.Count)
					count = stps.Count;
				for (int c = 0; c < count; c++)
				{
					// init accumulates properties from GetPropertyValue()
					init.Add(new Property { Name = props[c], Value = vals[c] });
					steps.Add((int)(100 * float.Parse(stps[c])));

					// previous has properties from Settings
					int Index = previous.FindIndex(i => i.Name == props[c]);

					// temp accumulates properties from previous or init, if new
					if (-1 == Index) {
						if (0 == s.Length)
							s += "adding " + props[c];
						else s += " + " + props[c];
						temp.Add(init[c]);
						changed = games.append(init[c]) || changed;
					}
					else temp.Add(previous[Index]);
				}
				if (0 < s.Length)
					s += ";  ";
				if (previous.Count != init.Count)
					Info("Init():  " + s + $"{path}.Count = {init.Count};  previous.Count = {previous.Count};\n"
						 +  JsonConvert.SerializeObject(previous, Formatting.Indented));
				current.properties = Pclone(previous = temp);	// UI can force current.properties back to previous
			}

			// Declare a property available in the property list
			// this gets evaluated "on demand" (when shown or used in formulae)
			foreach(Property p in current.properties)
				this.AttachDelegate(My+p.Name, () => p.Value);

			if (0 < current.properties.Count)
			{
				if (0 == gname.Length || 0 == current.id.Length)
					Selected_Property = "unKnown";
				else Selected_Property = current.properties[Select].Name;
				this.AttachDelegate(My+"Selected", () => Selected_Property);
				this.AttachDelegate(My+"Car", () => current.id);
				this.AttachDelegate(My+"Game", () => gname);
			}

			// Declare an event
			//this.AddEvent("SpeedWarning");

			this.AddAction("ChangeProperties",(a, b) =>
			{
				string s = "New Car: ";
				string cname = pluginManager.GetPropertyValue("CarID")?.ToString();
				string gnew = pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame")?.ToString();
				if (null !=cname && 0 < cname.Length && null != gnew)		// valid current car
				{
					s += cname;
					if (gnew == gname && games.Save_Car(current, gname))	// do not save first car in game
					{
						changed = true;
						s += $";  {current.id} saved";
					}
					else gname = gnew;
					previous = Pclone(current);
					current.id = cname;

					// properties for this car
					int gndx = games.data.Glist.FindIndex(g => g.name == gname);

					if (-1 != gndx)
					{
						int cndx = games.data.Glist[gndx].Clist.FindIndex(c => c.id == cname);
						if (-1 != cndx)
							cCopy(games.data.Glist[gndx].Clist[cndx].properties);
						else if (null != games.data.Glist[gndx].defaults)
							cCopy(games.data.Glist[gndx].defaults);
					}
					Selected_Property = current.properties[Select].Name;
				}
				else if (null == cname)
					s += "null CarID, ";
				else if (0 == cname.Length)
					s += "empty CarID, ";
				if (null == gnew)
					s += "null CurrentGame name, ";
				else if (0 == gnew.Length)
					s += "empty CurrentGame name, ";
				else gname = gnew;
				if (10 < s.Length)
					Info(s);
			});

			this.AddAction("IncrementSelectedProperty", (a, b) => ment(1, "in"));

			this.AddAction("DecrementSelectedProperty", (a, b) => ment(-1, "de"));

			this.AddAction("NextProperty", (a, b) => select(true) );

			this.AddAction("PreviousProperty", (a, b) => select(false) );

			this.AddAction("SwapCurrentPrevious", (a, b) => swap() );

			this.AddAction("CurrentAsDefaults", (a, b) => new_defaults());
		}
	}
}
