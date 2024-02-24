using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Media;

namespace JSONio
{
	[PluginDescription("game&car-specific properties to/from JSON")]
	[PluginAuthor("blekenbleu")]
	[PluginName("JSONio plugin")]
	public class DataPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
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

		// deep copy 
		private Property Clone(Property p) => new Property() { Name = string.Copy(p.Name), Value = string.Copy(p.Value) };

		internal List<Property> Pclone(Car car)	=> Pclone(car.properties);

		internal List<Property> Pclone(List<Property> prop)
		{
			List<Property> Plist = new List<Property> { };
			foreach(Property p in prop)
				Plist.Add(Clone(p));
			return Plist;
		}

		private Car Clone(Car c)
		{
			return new Car()
			{
				id = string.Copy(c.id),
				properties = Pclone(c)
			};
		}

		/// <summary>
		/// Plugin-specific wrapper for SimHub.Logging.Current.Info();
		/// </summary>
		internal static bool Info(string str)
		{
			SimHub.Logging.Current.Info(DataPlugin.My + str);   // bool Info()
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
				Settings.gname = string.Copy(gname);
				Settings.properties = Pclone(current);
				this.SaveCommonSettings("GeneralSettings", Settings);
			}
			if (null != games)
			{
//				bool ch = games.Save_Car(Clone(current), gname);
				if ( games.Save_Car(current, gname) || changed )
				{
					string js = JsonConvert.SerializeObject(games.data, Formatting.Indented);
					if ((0 == js.Length || "{}" == js) && 0 < games.data.Glist.Count)
						Info("End():  Json Serializer failure for games.data");
					else File.WriteAllText(path, js);
				}
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
			gname = Settings.gname;			// most recent sim
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
					steps.Add((int)(10 * float.Parse(stps[c])));

					// previous has properties from Settings
					int Index = previous.FindIndex(i => i.Name == props[c]);

					// temp accumulates properties from previous or init, if new
					if (-1 == Index) {
						if (0 == s.Length)
							s += "adding " + props[c];
						else s += " + " + props[c];
						temp.Add(init[c]);
						if (null != games.data)
							changed = games.append(init[c]) || changed;
					}
					else temp.Add(previous[Index]);
				}
				if (0 < s.Length)
					s += ";  ";
				if (previous.Count != init.Count)
					Info("Init():  " + s + $"previous.Count = {previous.Count};  {path}.Count = {init.Count}");
				current.properties = Pclone(previous = temp);	// UI can force current.properties back to previous
			}


			// Declare a property available in the property list
			// this gets evaluated "on demand" (when shown or used in formulae)
			foreach(Property p in current.properties)
				this.AttachDelegate(My+p.Name, () => p.Value);

			if (0 < current.properties.Count)
			{
				Selected_Property = current.properties[Select].Name;
				this.AttachDelegate(My+"Selected", () => Selected_Property);
			}

			// Declare an event
			//this.AddEvent("SpeedWarning");

			// Declare actions which can be called
			this.AddAction("ChangeProperties",(a, b) =>
			{
				string cname = pluginManager.GetPropertyValue("CarID")?.ToString();
  				string s = "New Car: ";
				gname = pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame")?.ToString();
				// save current car and game
				if (null !=cname && 0 < cname.Length && 0 < gname.Length)
				{
//					s += cname;
					if (0 < games.data.Glist.Count		// do not save first car
					&& games.Save_Car(current, gname))
					{
						changed = true;
//						s += $";  {current.id} saved";
					}
					previous = Pclone(current);
					current.id = cname;
					if (null != games.data.Glist) {
						// retrieve properties stored for this car
						int gndx = games.data.Glist.FindIndex(g => g.name == gname);

						if (-1 != gndx)
						{
							int cndx = games.data.Glist[gndx].Clist.FindIndex(c => c.id == cname);
							if (-1 != cndx)
								current.properties = Pclone(games.data.Glist[gndx].Clist[cndx]);
						}
					}
				}
				else if (null == cname)
					s += "null CarID, ";
				else if (0 == cname.Length)
					s += "empty CarID, ";
				if (null == gname)
					s += "null CurrentGame, ";
				else if (0 == gname.Length)
					s += "empty CurrentGame, ";
				if (10 < s.Length)
					Info(s);
			});

			void ment(int s, string prefix)
			{
				if (0 == steps[Select] % 10)
				{
					int fv = int.Parse(current.properties[Select].Value);
					fv += (int)(0.1 * s * steps[Select]);
					current.properties[Select].Value = $"{fv}";
				} else {
					float fv = float.Parse(current.properties[Select].Value);
					int i = (int)(steps[Select] + 10 * s * fv);
					current.properties[Select].Value = $"{0.1 * i}";
				}
				Info("property " + current.properties[Select].Name + " " + prefix + "cremented");
			}
			this.AddAction("IncrementSelectedProperty", (a, b) => ment(1, "in"));

			this.AddAction("DecrementSelectedProperty", (a, b) => ment(-1, "de"));

			void select(bool next)
			{
				if (next && ++Select >= current.properties.Count)
					Select = 0;
				if (!next)
				{
					if (0 < Select)
						Select--;
					else Select = (byte)(current.properties.Count - 1);
				}
				Selected_Property = current.properties[Select].Name;
				Info("Selected property = " + Selected_Property);
			}

			this.AddAction("NextProperty", (a, b) => select(true) );

			this.AddAction("PreviousProperty", (a, b) => select(false) );
		}
	}
}
