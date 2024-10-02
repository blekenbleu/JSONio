using GameReaderCommon;
using SimHub.Plugins;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Media;
using System.Windows;
using System.Windows.Forms;

namespace blekenbleu.jsonio
{
	[PluginDescription("NCalc configured properties to/from JSON")]
	[PluginAuthor("blekenbleu")]
	[PluginName("JSONio plugin")]
	public class JSONio : IPlugin, IDataPlugin, IWPFSettingsV2
	{
		public DataPluginSettings Settings;
		public string Selected_Property = "unKnown";
		public string New_Car = "false";
		internal static string Msg = "";
		internal static readonly string My = "JSONio.";			// breaks Ini if not preceding
		internal static readonly string Ini = "DataCorePlugin.ExternalScript." + My;	// configuration source
		internal static int pCount;								// global Property settings appended after pCount
		internal int[] Low, High;
		internal string[] Fmin, Fmax;
		private string path, slimPath;			// file locations
		private string Gname = "";
		private bool changed;
		private GameHandler games;
		private Slim slim;
		private List<int> Steps;
		private List<Property> SetProps;
		private readonly CarID CurrentCar = new CarID {};
		public ShakeIt S = new ShakeIt {};
		public double random0, random1, random2, random3;

		/// <summary>
		/// DisplayGrid contents
		/// </summary>
		public List<Values> simprops = new List<Values>();		// must be initialized before Init()

		internal List<Property> Pcopy(List<Values> p)			// deep copy
		{
			List<Property> Plist = new List<Property> {};
			for(int i = 0; i < p.Count; i++)
				if (null != p[i].Name &&  null != p[i].Current)
					Plist.Add(new Property() { Name = string.Copy(p[i].Name), Value = string.Copy(p[i].Current) });
			return Plist;
		}

/*		// copy per-car properties from game to simprops
		void Scopy(int cndx, Game game)	// copy matching values from Game
		{
			if (0 > cndx)
				for (int i = 0; i < pCount; i++)
					simprops[i].Current = simprops[i].Default = game.defaults[i].Value;
			else for (int i = 0; i < pCount; i++)
			{
				simprops[i].Current = game.Clist[cndx].properties[i].Value;
				simprops[i].Default = game.defaults[i].Value;
			}
		}
 */
		void Scopy(int cndx, GameList game)	// copy matching values from GameList
		{
			if (0 > cndx)
				for (int i = 0; i < pCount; i++)
					simprops[i].Current = simprops[i].Default = game.cList[0].Vlist[i];
			else for (int i = 0; i < pCount; i++)
			{
				simprops[i].Current = game.cList[cndx].Vlist[i];
				simprops[i].Default = game.cList[0].Vlist[i];
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

		internal void OOpsMB()
		{
			System.Windows.Forms.MessageBox.Show(Msg, "JSONio", MessageBoxButtons.OK);
		}

		internal bool OOps(string str)
		{
			if (0 < str.Length)
				Msg = str;
			OOpsMB();
			return (0 == str.Length) || Info(Msg);
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
			random0 = S.random.NextDouble();
			random1 = S.random.NextDouble();
			random2 = S.random.NextDouble();
			random3 = S.random.NextDouble();
		}

		private void SlimEnd(GamesList slim)
		{
			string sjs = JsonConvert.SerializeObject(slim, Formatting.Indented);
			if (0 == sjs.Length || "{}" == sjs)
				OOps("SlimEnd():  Json Serializer failure");
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
			if (0 < Gname.Length) {
				Settings.properties = Pcopy(simprops);
				this.SaveCommonSettings("GeneralSettings", Settings);
			}
			if (changed = slim.Save_Car(CurrentCar, simprops, Gname) || changed)
				SlimEnd(slim.data);
			else if (null != path && changed && games.Save_Car(CurrentCar, simprops, Gname))
			{
				string js = JsonConvert.SerializeObject(games.data, Formatting.Indented);

				if ((0 == js.Length || "{}" == js) && 0 < games.data.Glist.Count)
					OOps("End():  Json Serializer failure for games.data");
				else File.WriteAllText(path, js);
			}
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
		public void Ment(int sign)
		{
			if (0 == Gname.Length || 0 == CurrentCar.ID.Length)
				return;
			int step = Steps[View.Selection];
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
			Control.Model.StatusText = Gname + " " + CurrentCar.ID + " " + Selected_Property;
		}

		/// <summary>
		/// Select next or prior property; exception if invoked on other than UI thread
		/// </summary>
		/// <param name="next"></param> false for prior
		public void Select(bool next)
		{
			if (0 == Gname.Length || 0 == CurrentCar.ID.Length)
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

		private void New_defaults(List<GameList> Glist)
		{
			if (0 == Gname.Length)
				return;

			int p, Index = Glist.FindIndex(i => i.cList[0].Name == Gname);

			if (0 <= Index)
			{
				for (p = 0; p < pCount; p++)
					Glist[Index].cList[0].Vlist[p] =
					simprops[p].Default = simprops[p].Current;
				for (; p < simprops.Count; p++)
					simprops[p].Default = simprops[p].Current;
			}
		}
/*
		private void New_defaults(List<Game> Glist)
		{
			if (0 == Gname.Length)
				return;

			int p, Index = Glist.FindIndex(i => i.name == Gname);

			if (0 <= Index)
			{
				for (p = 0; p < pCount; p++)
					Glist[Index].defaults[p].Value =
					simprops[p].Default = simprops[p].Current;
				for (; p < simprops.Count; p++)
					simprops[p].Default = simprops[p].Current;
			}
		}
 */
		public void New_defaults() => New_defaults(slim.data.gList);

		// when JSONio.ini and JSONio.json disagree
		private List<Property> Refactor(List<string> iprops, List<Property> fold)
		{
			List<Property> dlist = new List<Property> {};
			for (int p = 0; p < pCount; p++)	// JSONio.json does not contain settings ( p >= pCount)
			{
				int Index =  fold.FindIndex(j => j.Name == iprops[p]);
				if (-1 == Index)
					dlist.Add(new Property() { Name = iprops[p], Value = simprops[p].Default });
				else dlist.Add(fold[Index]);
			}
			return dlist;
		}

		// add properties and settings to simprops
		private void Populate(List<string>props, List<string> vals, List<string> stps)
		{
			for (int c = 0; c < props.Count; c++)
			{
				// populate DisplayGrid ItemsSource
				// JSONio.ini contents may not match saved car properties
				int Index = SetProps.FindIndex(i => i.Name == props[c]);
				string s = (c < vals.Count) ? vals[c] : "0";
				string p = (-1 != Index) ? SetProps[Index].Value : s;
				if (c >= vals.Count && -1 != Index)
					s = p;

				simprops.Add(new Values { Name = props[c], Default = s, Current = p, Previous = p });

				if (c < stps.Count)
					Steps.Add((int)(100 * float.Parse(stps[c])));
				else Steps.Add(10);
			}
		}

		/// <summary>
		/// Called once after plugins startup
		/// Plugins are rebuilt at game change
		/// </summary>
		/// <param name="pluginManager"></param>
		public void Init(PluginManager pluginManager)
		{
			List<string> Iprops = new List<string> { "" };
			Low = new int[] {0,0,0,0};
			High = new int[] {0,0,0,0};
			Fmax = new string[] {"Fmax.FrontLeft", "Fmax.FrontRight", "Fmax.RearLeft", "Fmax.RearRight"};
			Fmin = new string[] {"Fmin.FrontLeft", "Fmin.FrontRight", "Fmin.RearLeft", "Fmin.RearRight"};

			changed = false;	// write JSON file during End() only if true

			slim = new Slim(this)
			{
				data = new GamesList()
				{
					Plugin = "JSONio",
					gList = new List<GameList>() {},
					pList = new List<string> {}
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

			// Load Properties from settings
			Settings = this.ReadCommonSettings<DataPluginSettings>("GeneralSettings", () => new DataPluginSettings());

			// Declare an event and corresponding action
            this.AddEvent("JSONioOOps");
			this.AddAction("OopsMessageBox", (a, b) => OOpsMB());

			// restore previously saved car properties
			SetProps = new List<Property> {};				// deep copy
			foreach(Property p in Settings.properties)
				if (null != p.Name && null != p.Value)
					SetProps.Add(new Property() { Name = string.Copy(p.Name), Value = string.Copy(p.Value) });

			Steps = new List<int>() { };

			// Load property and setting names, default values and steps from JSONio.ini
			string pts, ds = pluginManager.GetPropertyValue(pts = Ini + "properties")?.ToString();
			string vts, vs = pluginManager.GetPropertyValue(vts = Ini + "values")?.ToString();
			string sts, ss = pluginManager.GetPropertyValue(sts = Ini + "steps")?.ToString();
			if ((!(null == ds && OOps($"Init(): '{pts}' not found")))
			 && (!(null == vs && OOps($"Init(): '{vts}' not found")))
			 && (!(null == ss && OOps($"Init(): '{sts}' not found")))
				)
			{
				// JSONio.ini defines per-car Properties
				Iprops = new List<string>(ds.Split(','));
				pCount = Iprops.Count;						// these are per-car
				List<string> values = new List<string>(vs.Split(','));
				List<string> steps = new List<string>(ss.Split(','));
				if (pCount != values.Count || pCount != steps.Count)
					OOps($"Init(): {pCount} per-car properties;  {values.Count} values;  {steps.Count} steps");
				Populate(Iprops, values, steps);
			}

			// JSONio.ini also optionally defines settings (NOT per-car)
			string ptts, dss = pluginManager.GetPropertyValue(ptts = Ini + "settings")?.ToString();
			string vtts, vss = pluginManager.GetPropertyValue(vtts = Ini + "setvals")?.ToString();
			string stts, sss = pluginManager.GetPropertyValue(stts = Ini + "setsteps")?.ToString();
			if ((!(null == dss && OOps($"Init(): '{ptts}' not found")))
			 && (!(null == vss && OOps($"Init(): '{vtts}' not found")))
			 && (!(null == sss && OOps($"Init(): '{stts}' not found")))
				)
			{
				List<string> Sprops = new List<string>(dss.Split(','));
				List<string> values = new List<string>(vss.Split(','));
				List<string> steps = new List<string>(sss.Split(','));
				if (Sprops.Count != values.Count || Sprops.Count != steps.Count)
					OOps($"Init(): {Sprops.Count} settings;  {values.Count} values;  {steps.Count} steps");
				Populate(Sprops, values, steps);
			}

			if (0 == simprops.Count)
			{
				OOps(Control.Model.StatusText = "Missing or invalid " + Ini + "properties from NCalcScripts/JSONio.ini");
				return;
			}

			// find Fmin, Fmax settings
			for (int i = 0; i < Fmin.Length; i++)
			{
				int j = simprops.FindIndex(k => k.Name == Fmin[i]);
				if (0 <= j)
					Low[i] = j;
				j = simprops.FindIndex(k => k.Name == Fmax[i]);
				if (0 <= j)
                    High[i] = j;
			}

			path = pluginManager.GetPropertyValue(Msg = Ini + "file")?.ToString();
			// Load existing JSON, first trying new slim format
			if (!slim.Load(slimPath = pluginManager.GetPropertyValue(Msg = Ini + "slim")?.ToString(), simprops))
			{
				changed = OOps($"Init(): {Msg} not found");
				if (File.Exists(path))
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
							OOps($"Init(): {path} properties mismatched NCalcScripts/JSONio.ini");
							for (i = 0; i < foo.Glist.Count; i++)
							{
								foo.Glist[i].defaults = Refactor(Iprops, foo.Glist[i].defaults);
								for (int c = 0; c < foo.Glist[i].Clist.Count; c++)
									if (null == foo.Glist[i].Clist[c].carID)
									{
										nullcarID++;
										foo.Glist[i].Clist.RemoveAt(c--);
									}
									else foo.Glist[i].Clist[c].properties = Refactor(Iprops, foo.Glist[i].Clist[c].properties);
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
							OOps($"Init(): {nullcarID} null carIDs");
						games.data = foo;
						if (null == slim.data || 0 == slim.data.gList.Count)
							SlimEnd(slim.data = slim.Migrate(games));
					}
					else changed = OOps($"Init():  empty or invalid {Msg}");
				}
				else changed = OOps($"Init(): {Msg} file not found");
			}
			else Msg = "Init():  " + Msg + " loaded";

			// Declare available properties
			// these get evaluated "on demand" (when shown or used in formulae)
			foreach(Values p in simprops)
				this.AttachDelegate(p.Name, () => p.Current);

			if (0 == Gname.Length || 0 == CurrentCar.ID.Length)
				Selected_Property = "unKnown";
			else SelectedStatus();

			this.AttachDelegate("Selected", () => Selected_Property);
			this.AttachDelegate("New Car", () => New_Car);
			this.AttachDelegate("Car", () => CurrentCar.ID);
			this.AttachDelegate("Game", () => Gname);
			this.AttachDelegate("Msg", () => Msg);
			this.AttachDelegate("random0", () => random0);
			this.AttachDelegate("random1", () => random1);
			this.AttachDelegate("random2", () => random2);
			this.AttachDelegate("random3", () => random3);

/*---------	this.AddAction("ChangeProperties",...)
 ;		invoked for CarId changes, based on this `NCalcScripts/JSONio.ini` entry:
 ;			[ExportEvent]
 ;			name='CarChange'
 ;			trigger=changed(200, [DataCorePlugin.GameData.CarId])
 ;--------------------------------------------------------------- */	
			this.AddAction("ChangeProperties",(a, b) =>
			{
				if (0 == simprops.Count)
					return;

				int ml = 0;
				string cname = pluginManager.GetPropertyValue("CarID")?.ToString();
				string gnew = pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame")?.ToString();
				if (null !=cname && 0 < cname.Length && null != gnew)		// valid new car?
				{
					Msg = "Current Car: " + cname;
					if (0 < Gname.Length								// do not save first (null) CurrentCar.ID in game
					 && slim.Save_Car(CurrentCar, simprops, Gname))
					{
						changed = true;
						slim.Save_Car(CurrentCar, simprops, Gname);
						Msg += $";  {CurrentCar.ID} saved";
					}
					ml = Msg.Length;

					for (int i = 0; i < pCount; i++)					// copy Current to previous
						simprops[i].Previous = simprops[i].Current;

					// indices for new car
					int gndx, cndx = slim.Car_Change(out gndx, gnew, cname);
					New_Car = (-1 == cndx) ? "true" : "false";
						
					CurrentCar.ID = cname;
					if (0 <= gndx)										// else reuse current properties
						Scopy(cndx, slim.data.gList[gndx]);
					SelectedStatus();
					Control.Model.ButtonVisibility = Visibility.Visible;	// ready for business
				}
				else if (null == cname)		// CarID verification - should make a popup
					Msg = "null CarID";
				else if (0 == cname.Length)
					Msg = "empty CarID";

				if (null == gnew)
					Msg += ", null CurrentGame Name, ";
				else if (0 == gnew.Length)
					Msg += ", empty CurrentGame Name, ";
				else Gname = gnew;

				if (ml < Msg.Length) {
					Info(Msg);
					this.TriggerEvent("JSONioOOps");
				}
			});

			this.AddAction("IncrementSelectedProperty", (a, b) => Ment(1));
			this.AddAction("DecrementSelectedProperty", (a, b) => Ment(-1));
			this.AddAction("NextProperty",				(a, b) => Select(true)	);
			this.AddAction("PreviousProperty",			(a, b) => Select(false)	);
			this.AddAction("SwapCurrentPrevious",		(a, b) => Swap()		);
			this.AddAction("CurrentAsDefaults",			(a, b) => New_defaults());

			S.Init(this, pluginManager);
		}	// Init()
	}		// class JSONio
}
