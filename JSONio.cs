using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Media;
using System.Windows;
using System.ComponentModel;

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
		internal string gname = "";
		private List<int>steps;
		private List<Property> temp;
		internal Car current;

		internal List<Property> Pclone(List<Property> prop)			// deep copy
		{
			List<Property> Plist = new List<Property> {};
			foreach(Property p in prop)
				if (null != p.Name && null != p.Value)
					Plist.Add(new Property() { Name = string.Copy(p.Name), Value = string.Copy(p.Value) });
			return Plist;
		}

		internal List<Property> Pclone(Car car)	=> Pclone(car.properties);

		void cCopy(List<Property> Plist)
		{
			for(int c = 0; c < current.properties.Count; c++)
			{
				int Index = Plist.FindIndex(i => i.Name == current.properties[c].Name);
				if (-1 != Index)
					current.properties[c].Value = Plist[Index].Value;
			}
		}

		/// <summary>
		/// Plugin-specific wrapper for SimHub.Logging.Current.Info();
		/// </summary>
		internal static bool Info(string str)
		{
			SimHub.Logging.Current.Info(JSONio.My + str);   // bool Info()
			return true;
		}

		/// <summary>
		/// Instance of the plugin manager
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
		/// DisplayGrid contents
		/// </summary>
		public List<Values> simprops = new List<Values>();		// must be initialized before Init()

		/// <summary>
		/// Called one time per game data update, contains all normalized game data,
		/// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
		/// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <param name="data">Current game data, including present and previous data frames.</param>
		public void DataUpdate(PluginManager pluginManager, ref GameData data)
		{
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
        /// <returns> instance of UserControl </returns>
        private Control cx;	// instance of Control.xaml.cs Control()
		public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
		{
			return cx = new Control(this);		// invoked *after* Init()
		}

		/// <summary>
		/// Helper functions used in Init() AddAction()s and Control.xaml.cs button Clicks
		/// </summary>
		/// <param name="sign"></param> should be 1 or -1
		/// <param name="prefix"></param> should be "in" or "de"
		public void ment(int sign, string prefix)
		{
			if (0 == gname.Length || 0 == current.carID.Length)
				return;
			int step = steps[cx.Selection];
			int iv = (int)(0.004 + 100 * float.Parse(current.properties[cx.Selection].Value));

			iv += sign * step;
			if (0 <= iv)
			{
				if (0 != step % 100)
					current.properties[cx.Selection].Value = $"{(float)(0.01 * iv)}";
				else current.properties[cx.Selection].Value = $"{(int)(0.004 + 0.01 * iv)}";
//				Info("property " + current.properties[cx.Selection].Name + " " + prefix
//					 + $"cremented to {current.properties[cx.Selection].Value}");
				simprops[cx.Selection].Current = current.properties[cx.Selection].Value;
				changed = true;
			}
		}

		private void SelectedStatus()
		{
			Selected_Property = current.properties[cx.Selection].Name;
			Control.ui.StatusText = gname + " " + current.carID + " " + Selected_Property;
		}

		/// <summary>
		/// Select next or prior property; exception if invoked on other than UI thread
		/// </summary>
		/// <param name="next"></param> false for prior
		public void Select(bool next)
		{
			if (0 == gname.Length || 0 == current.carID.Length)
				return;

			if (next)
			{
				if (++cx.Selection >= current.properties.Count)
					cx.Selection = 0;
			}
			else if (0 < cx.Selection)	// prior
				cx.Selection--;
			else cx.Selection = (byte)(current.properties.Count - 1);
			SelectedStatus();
//			Info("Selected property = " + Selected_Property);
		}

		public void Swap()
		{
			string temp;
			for (int i = 0; i < simprops.Count; i++)
			{
				temp = simprops[i].Previous;
				simprops[i].Previous = simprops[i].Current;
				simprops[i].Current = temp;
			}
		}

		public void New_defaults()
		{
			if (0 == gname.Length)
				return;

			int Index = games.data.Glist.FindIndex(i => i.name == gname);

			if (-1 != Index)
				for (int i = 0; i < simprops.Count; i++)
					games.data.Glist[Index].defaults[i].Value =
					simprops[i].Default = current.properties[i].Value;
		}

		/// <summary>
		/// Called once after plugins startup
		/// Plugins are rebuilt at game change
		/// </summary>
		/// <param name="pluginManager"></param>
		public void Init(PluginManager pluginManager)
		{
			changed = false;    // write JSON file during End() only if true

			games = new GameHandler()
			{
				data = new Games()
				{
					name = "JSONio",
					Glist = new List<Game>() {}
				}
			};

            // Load properties from settings
            Settings = this.ReadCommonSettings<DataPluginSettings>("GeneralSettings", () => new DataPluginSettings());

			// retrieve previously saved Car properties
			temp = Pclone(Settings.properties);

			current = new Car() { carID = "", properties = new List<Property> {} };
			steps = new List<int>() { };

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
				List<string> props, vals, stps;

				// JSONio.ini props determine current Properties
				props = new List<string>(ds.Split(','));
				vals = new List<string>(vs.Split(','));
				stps = new List<string>(ss.Split(','));
				if (props.Count != vals.Count || props.Count != stps.Count)
					Info($"Init(): {props.Count} properties;  {vals.Count} values;  {stps.Count} steps");

				for (int c = 0; c < props.Count; c++)
				{
					// populate DisplayGrid ItemsSource
					// JSONio.ini contents may not match saved Car properties
					int Index = temp.FindIndex(i => i.Name == props[c]);
					string s = (c < vals.Count) ? vals[c] : "0";
					string p = (-1 != Index) ? temp[Index].Value : s;
					if (c >= vals.Count && -1 != Index)
						s = p;

					simprops.Add(new Values { Name = props[c], Default = s, Current = p, Previous = p });

					current.properties.Add( new Property {
											Name = simprops[c].Name, Value = simprops[c].Current });
					if (c < stps.Count)
						steps.Add((int)(100 * float.Parse(stps[c])));
					else steps.Add(10);
				}
			}

			if (0 == simprops.Count)
			{
				Control.ui.StatusText = "Missing or invalid " + Ini + "properties from NCalcScripts/JSONio.ini";
				Info(Control.ui.StatusText);
				return;
			}

/*	Hack to force settings
			temp = Pclone(current);
			current.properties = null;
			foreach (Property p in temp)
			{
				if ("offset" == p.Name)
					continue;
				if ("gamma" == p.Name)
					p.Value = "5";
				else if ("threshold" == p.Name)
					p.Value = "10";
				current.properties.Add(p);
			}
 */

			// Load existing JSON
			path = pluginManager.GetPropertyValue(Ini + "file")?.ToString();
			if (File.Exists(path))
			{
				Games foo = JsonConvert.DeserializeObject<Games>(File.ReadAllText(path));

				// test for consistency between current.properties and foo
				if (null != foo && null != foo.name && null != foo.Glist) {
					List<Property> d = foo.Glist[0].defaults;
					int i = -1;

					if (simprops.Count == d.Count)
						for (i = 0; i < simprops.Count; i++)
							if (d[i].Name != simprops[i].Name)
								break;

					if (i != simprops.Count) // repopulate Car properties according to NCalcScripts/JSONio.ini
					{
						Info($"Init(): {path} properties mismatched NCalcScripts/JSONio.ini");
						for (i = 0; i < foo.Glist.Count; i++)
						{
							List<Property> dlist = new List<Property> {};
							for (int p = 0; p < simprops.Count; p++)
							{
								int Index = foo.Glist[i].defaults.FindIndex(j => j.Name == simprops[p].Name);
								if (-1 == Index)
                                    dlist.Add(new Property() { Name = simprops[p].Name, Value = simprops[p].Default });
                                else dlist.Add(foo.Glist[i].defaults[Index]);
							}
							foo.Glist[i].defaults = dlist;
							for (int c = 0; c < foo.Glist[i].Clist.Count; c++)
							{
								List<Property> plist = new List<Property> {};
								for (int p = 0; p < simprops.Count; p++)
								{
									int Index = foo.Glist[i].Clist[c].properties.FindIndex(j => j.Name == simprops[c].Name);
									if (-1 == Index)
										plist.Add(new Property() { Name = simprops[c].Name, Value = simprops[c].Default });
									else plist.Add(foo.Glist[i].Clist[c].properties[Index]);
								}
								foo.Glist[i].Clist[c].properties = plist;
							}
						}
					}
					games.data = foo;
				}
				else changed = Info($"Init():  empty or invalid {path}");
			}
			else changed = Info("Init()");

			// Declare available properties
			// these get evaluated "on demand" (when shown or used in formulae)
			foreach(Property p in current.properties)
				this.AttachDelegate(My+p.Name, () => p.Value);

			if (0 == gname.Length || 0 == current.carID.Length)
				Selected_Property = "unKnown";
			else SelectedStatus();

			this.AttachDelegate(My+"Selected", () => Selected_Property);
			this.AttachDelegate(My+"Car", () => current.carID);
			this.AttachDelegate(My+"Game", () => gname);

/*---------	this.AddAction("ChangeProperties",...)
 ;		invoked for CarId changes, based on this `NCalcScripts/JSONio.ini` entry:
 ;			[ExportEvent]
 ;			name='CarChange'
 ;			trigger=changed(200, [DataCorePlugin.GameData.CarId])
 ;--------------------------------------------------------------- */	
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
						s += $";  {current.carID} saved";
					}
					else gname = gnew;
					for (int i = 0; i < current.properties.Count; i++)
						simprops[i].Previous = current.properties[i].Value;
					current.carID = cname;

					// properties for this car
					int gndx = games.data.Glist.FindIndex(g => g.name == gname);

					if (-1 != gndx)
					{
						int cndx = games.data.Glist[gndx].Clist.FindIndex(c => c.carID == cname);
						if (-1 != cndx)
							cCopy(games.data.Glist[gndx].Clist[cndx].properties);
						else if (null != games.data.Glist[gndx].defaults)
							cCopy(games.data.Glist[gndx].defaults);
						for (int i = 0; i < current.properties.Count; i++)
							simprops[i].Current = current.properties[i].Value;
						if (null != games.data.Glist[gndx].defaults)
							for (int i = 0; i < current.properties.Count; i++)
								simprops[i].Default = games.data.Glist[gndx].defaults[i].Value;
						SelectedStatus();
						Control.ui.ButtonVisibility = Visibility.Visible;	// ready for business
					}
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

			this.AddAction("IncrementSelectedProperty", (a, b) => ment(1, "in")	);
			this.AddAction("DecrementSelectedProperty", (a, b) => ment(-1, "de"));
			this.AddAction("NextProperty",				(a, b) => Select(true)	);
			this.AddAction("PreviousProperty",			(a, b) => Select(false)	);
			this.AddAction("SwapCurrentPrevious",		(a, b) => Swap()		);
			this.AddAction("CurrentAsDefaults",			(a, b) => New_defaults());
		}	// Init()
	}		// class JSONio
}
