﻿using GameReaderCommon;
using SimHub.Plugins;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Media;
using System.Windows;

namespace blekenbleu
{
	[PluginDescription("game&car-specific properties to/from JSON")]
	[PluginAuthor("blekenbleu")]
	[PluginName("JSONio plugin")]
	public class JSONio : IPlugin, IDataPlugin, IWPFSettingsV2
	{
		public DataPluginSettings Settings;
		internal static readonly string My = "JSONio.";			// breaks Ini if not preceding
		internal static readonly string Ini = "DataCorePlugin.ExternalScript." + My;	// configuration source
		internal static int pCount;								// global Property settings appended after pCount
		private string oops = "Oops!", path, slimPath;															// file locations
		private bool changed;
		private GameHandler games;
		private Slim slim;
        public string Selected_Property = "unKnown";
		public string New_Car = "false";
		internal string gname = "";
		private List<int>steps;
		private List<Property> temp;
		private List<string> props;
		private CarID car = new CarID { Length = 0 };

		/// <summary>
		/// DisplayGrid contents
		/// </summary>
		public List<Values> simprops = new List<Values>();		// must be initialized before Init()

		internal List<Property> Pclone(List<Property> prop)			// deep copy
		{
			List<Property> Plist = new List<Property> {};
			foreach(Property p in prop)
				if (null != p.Name && null != p.Value)
					Plist.Add(new Property() { Name = string.Copy(p.Name), Value = string.Copy(p.Value) });
			return Plist;
		}

		internal List<Property> Pcopy(List<Values> prop)			// deep copy
		{
			List<Property> Plist = new List<Property> {};
			foreach(Values p in prop)
				if (null != p.Name && null != p.Current)
					Plist.Add(new Property() { Name = string.Copy(p.Name), Value = string.Copy(p.Current) });
			return Plist;
		}

		internal List<Property> Pclone(Car car)	=> Pclone(car.properties);

		void Scopy(int gndx, int cndx)	// copy matching property values from Plist
		{
			if (-1 != cndx)
				for (int i = 0; i < pCount; i++)
				{
					simprops[i].Current = games.data.Glist[gndx].Clist[cndx].properties[i].Value;
					simprops[i].Default = games.data.Glist[gndx].defaults[i].Value;
				}
			else for (int i = 0; i < pCount; i++)
				simprops[i].Current = simprops[i].Default = games.data.Glist[gndx].defaults[i].Value;
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
		/// Called one time per game data update, contains all normalized game data,
		/// raw data are intentionnally "hidden" under a generic object type (plugins SHOULD NOT USE)
		/// This method is on the critical path, must execute as fast as possible and avoid throwing any error
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <param name="data">Current game data, including present and previous data frames.</param>
		public void DataUpdate(PluginManager pluginManager, ref GameData data)
		{
		}

		private void SlimEnd(GamesList slim)
		{
			string sjs = JsonConvert.SerializeObject(slim, Formatting.Indented);
			if (0 == sjs.Length || "{}" == sjs)
				Info("SlimEnd():  Json Serializer failure");
			else File.WriteAllText(slimPath, sjs);
		}

		/// <summary>
		/// Called at plugin manager stop, close/dispose anything needed here !
		/// Plugins are rebuilt at game changes
		/// </summary>
		/// <param name="pluginManager"></param>
		public void End(PluginManager pluginManager)
		{
			// Save settings
			if (0 < gname.Length) {
				Settings.properties = Pcopy(simprops);
				this.SaveCommonSettings("GeneralSettings", Settings);
			}
			if (games.Save_Car(car, Pcopy(simprops), gname) || changed)
			{
				string js = JsonConvert.SerializeObject(games.data, Formatting.Indented);

				if ((0 == js.Length || "{}" == js) && 0 < games.data.Glist.Count)
					Info("End():  Json Serializer failure for games.data");
				else File.WriteAllText(path, js);
				if (null == slim.data || 0 == slim.data.GameL.Count)
					slim.data = slim.Migrate(games);
				SlimEnd(slim.data);
			}
			else SlimEnd(slim.data);
		}

		/// <summary>
		/// Returns the settings control, return null if no settings control is required
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <returns> instance of UserControl </returns>
		private Control View;	// instance of Control.xaml.cs Control()
		public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
		{
			return View = new Control(this);		// invoked *after* Init()
		}

		/// <summary>
		/// Helper functions used in Init() AddAction()s and Control.xaml.cs button Clicks
		/// </summary>
		/// <param name="sign"></param> should be 1 or -1
		/// <param name="prefix"></param> should be "in" or "de"
		public void ment(int sign, string prefix)
		{
			if (0 == gname.Length || 0 == car.ID.Length)
				return;
			int step = steps[View.Selection];
			int iv = (int)(0.004 + 100 * float.Parse(simprops[View.Selection].Current));

			iv += sign * step;
			if (0 <= iv)
			{
				if (0 != step % 100)
					simprops[View.Selection].Current = $"{(float)(0.01 * iv)}";
				else simprops[View.Selection].Current = $"{(int)(0.004 + 0.01 * iv)}";
//				Info("property " + simprops[View.Selection].Name + " " + prefix
//					 + $"cremented to {simprops[View.Selection].Value}");
				changed = true;
			}
		}

		private void SelectedStatus()
		{
			Selected_Property = simprops[View.Selection].Name;
			Control.Model.StatusText = gname + " " + car.ID + " " + Selected_Property;
		}

		/// <summary>
		/// Select next or prior property; exception if invoked on other than UI thread
		/// </summary>
		/// <param name="next"></param> false for prior
		public void Select(bool next)
		{
			if (0 == gname.Length || 0 == car.ID.Length)
				return;

			if (next)
			{
				if (++View.Selection >= simprops.Count)
					View.Selection = 0;
			}
			else if (0 < View.Selection)	// prior
				View.Selection--;
			else View.Selection = (byte)(simprops.Count - 1);
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
					simprops[i].Default = simprops[i].Current;
		}

		// when JSONio.ini and JSONio.json disagree
		private List<Property> Refactor(List<Property> fold)
		{
			List<Property> dlist = new List<Property> {};
			for (int p = 0; p < pCount; p++)	// JSONio.json does not contain settings ( p >= pCount)
			{
				int Index =  fold.FindIndex(j => j.Name == props[p]);
				if (-1 == Index)
					dlist.Add(new Property() { Name = props[p], Value = simprops[p].Default });
				else dlist.Add(fold[Index]);
			}
			return dlist;
		}

		// add properties and settings to simprops
		private void populate(List<string>props, List<string> vals, List<string> stps)
		{
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

				if (c < stps.Count)
					steps.Add((int)(100 * float.Parse(stps[c])));
				else steps.Add(10);
			}
		}

		/// <summary>
		/// Called once after plugins startup
		/// Plugins are rebuilt at game change
		/// </summary>
		/// <param name="pluginManager"></param>
		public void Init(PluginManager pluginManager)
		{
			changed = false;	// write JSON file during End() only if true

			slim = new Slim()
			{
				data = new GamesList()
				{
					Plugin = "JSONio",
					GameL = new List<GameList>() {},
					PropertyL = new List<string> {}
				}
			};

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

			steps = new List<int>() { };

			// Load property and setting names, default values and steps from JSONio.ini
			string pts, ds = pluginManager.GetPropertyValue(pts = Ini + "properties")?.ToString();
			string vts, vs = pluginManager.GetPropertyValue(vts = Ini + "values")?.ToString();
			string sts, ss = pluginManager.GetPropertyValue(sts = Ini + "steps")?.ToString();
			string ptts, dss = pluginManager.GetPropertyValue(ptts = Ini + "settings")?.ToString();
			string vtts, vss = pluginManager.GetPropertyValue(vtts = Ini + "setvals")?.ToString();
			string stts, sss = pluginManager.GetPropertyValue(stts = Ini + "setsteps")?.ToString();
			if ((!(null == ds && Info($"Init(): '{pts}' not found")))
			 && (!(null == vs && Info($"Init(): '{vts}' not found")))
			 && (!(null == ss && Info($"Init(): '{sts}' not found")))
			 && (!(null == dss && Info($"Init(): '{ptts}' not found")))
			 && (!(null == vss && Info($"Init(): '{vtts}' not found")))
			 && (!(null == sss && Info($"Init(): '{stts}' not found")))
				)
			{
				List<Property> init = new List<Property>() {};
				List<string> values, steps;

				// JSONio.ini defines per-car Properties
				props = new List<string>(ds.Split(','));
				pCount = props.Count;						// these are per-car
				values = new List<string>(vs.Split(','));
				steps = new List<string>(ss.Split(','));
				if (pCount != values.Count || props.Count != steps.Count)
					Info($"Init(): {props.Count} properties;  {values.Count} values;  {steps.Count} steps");
				populate(props, values, steps);

				// JSONio.ini defines settings
				props = new List<string>(dss.Split(','));
				values = new List<string>(vss.Split(','));
				steps = new List<string>(sss.Split(','));
				if (props.Count != values.Count || props.Count != steps.Count)
					Info($"Init(): {props.Count} properties;  {values.Count} values;  {steps.Count} steps");
				populate(props, values, steps);
			}

			if (0 == simprops.Count)
			{
				Info(oops = "Missing or invalid " + Ini + "properties from NCalcScripts/JSONio.ini");
				return;
			}

			// Load existing JSON, first trying new slim format
			if (!slim.Load(slimPath = pluginManager.GetPropertyValue(Ini + "slim")?.ToString(), simprops)
			 && File.Exists(path = pluginManager.GetPropertyValue(Ini + "file")?.ToString()))
			{
				Games foo = JsonConvert.DeserializeObject<Games>(File.ReadAllText(path));

				// test for consistency between simprops and foo
				if (null != foo && null != foo.name && null != foo.Glist)
				{
					int nullcarID = 0;
					List<Property> d = foo.Glist[0].defaults;
					int i = -1;

					if (pCount == d.Count)
						for (i = 0; i < pCount; i++)
							if (d[i].Name != simprops[i].Name)
								break;

					if (i != pCount) // repopulate Car properties according to NCalcScripts/JSONio.ini
					{
						Info($"Init(): {path} properties mismatched NCalcScripts/JSONio.ini");
						for (i = 0; i < foo.Glist.Count; i++)
						{
							foo.Glist[i].defaults = Refactor(foo.Glist[i].defaults);
							for (int c = 0; c < foo.Glist[i].Clist.Count; c++)
								if (null == foo.Glist[i].Clist[c].carID)
								{
									nullcarID++;
									foo.Glist[i].Clist.RemoveAt(c--);
								}
								else foo.Glist[i].Clist[c].properties = Refactor(foo.Glist[i].Clist[c].properties);
						}
					} else {	// eliminate null carIDs
						for (i = 0; i < foo.Glist.Count; i++)
							for (int c = 0; c < foo.Glist[i].Clist.Count;)
								if (null == foo.Glist[i].Clist[c].carID)
								{
									nullcarID++;
									foo.Glist[i].Clist.RemoveAt(c);
								}
								else c++;
					}
					if (0 < nullcarID)
						Info($"Init(): {nullcarID} null carIDs");
					games.data = foo;
				}
				else changed = Info($"Init():  empty or invalid {path}");
			}
			else changed = Info($"Init(): {path} not found");

			// Declare available properties
			// these get evaluated "on demand" (when shown or used in formulae)
			foreach(Values p in simprops)
				this.AttachDelegate(My+p.Name, () => p.Current);

			if (0 == gname.Length || 0 == car.ID.Length)
				Selected_Property = "unKnown";
			else SelectedStatus();

			this.AttachDelegate(My+"Selected", () => Selected_Property);
			this.AttachDelegate(My+"New Car", () => New_Car);
			this.AttachDelegate(My+"Car", () => car.ID);
			this.AttachDelegate(My+"Game", () => gname);

/*---------	this.AddAction("ChangeProperties",...)
 ;		invoked for CarId changes, based on this `NCalcScripts/JSONio.ini` entry:
 ;			[ExportEvent]
 ;			name='CarChange'
 ;			trigger=changed(200, [DataCorePlugin.GameData.CarId])
 ;--------------------------------------------------------------- */	
			this.AddAction("ChangeProperties",(a, b) =>
			{
				if (0 == simprops.Count)
				{
					Control.Model.StatusText = oops;
					return;
				}
				string s = "New Car: ";
				string cname = pluginManager.GetPropertyValue("CarID")?.ToString();
				string gnew = pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame")?.ToString();
				if (null !=cname && 0 < cname.Length && null != gnew)		// valid current car
				{
					int cndx = -1;
					// properties for this car
					int gndx = (0 < gnew.Length) ? games.data.Glist.FindIndex(g => g.name == gnew) : -1;

					if (-1 != gndx)
						cndx = games.data.Glist[gndx].Clist.FindIndex(c => c.carID == cname);

					New_Car = (-1 == cndx) ? "true" : "false";
						
					s += cname;
					if (0 < gname.Length && games.Save_Car(car, Pcopy(simprops), gname))	// do not save first car in game
					{
						changed = true;
						s += $";  {car.ID} saved";
					}
					
					for (int i = 0; i < pCount; i++)
						simprops[i].Previous = simprops[i].Current;
					car.ID = cname;

					if (-1 != gndx)
					{
						Scopy(gndx, cndx);
						SelectedStatus();
						Control.Model.ButtonVisibility = Visibility.Visible;	// ready for business
					}
				}
				else if (null == cname)		// carID not found
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
