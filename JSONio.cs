using GameReaderCommon;
using SimHub.Plugins;
using System.Collections.Generic;
using System.Windows.Media;
using System;
using System.Windows;

namespace blekenbleu.jsonio
{
	[PluginDescription("NCalc configured properties to/from JSON")]
	[PluginAuthor("blekenbleu")]
	[PluginName("JSONio plugin")]
	public class JSONio : IPlugin, IDataPlugin, IWPFSettingsV2
	{
		public DataPluginSettings Settings;
		public string New_Car = "false";
		internal static int pCount, gCount;												// append per-game settings after pCount, global after gCount
		internal int slider = -1;														// simValues index for configured JSONIO.properties
		internal static string Msg = "";
		internal bool changed;															// slim may change
		private static readonly string My = "JSONio.";									// breaks Ini if not preceding
		private static readonly string Myni = "DataCorePlugin.ExternalScript." + My;	// configuration source
		private string CurrentCar;
		private string Gname = "";
		private string path;															// JSON file location
		private Slim slim;																// new JSON format
		private List<Property> SetProps;
		private List<int> Steps;														// 100 times actual values
		private readonly double[] Slider_factor = new double[] { 0, 0 };

		/// <summary>
		/// DisplayGrid contents
		/// </summary>
		public List<Values> simValues = new List<Values>();								// must be initialized before Init()

		internal void Psave(List<Values> p)												// deep copy for Settings.properties
		{
			Settings.properties = new List<Property> {};
			for(int i = 0; i < p.Count; i++)
				if (null != p[i].Name &&  null != p[i].Current)
					Settings.properties.Add(new Property() { Name = string.Copy(p[i].Name), Value = string.Copy(p[i].Current) });
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
			Info("OOpsMB(): " + Msg);
		//	System.Windows.Forms.MessageBox.Show(Msg, "JSONio");
			View.Dispatcher.Invoke(() => View.OOpsMB());
			Msg = "";
		}

		internal bool OOps(string str)
		{
			if (null != str  || 0 < Msg.Length)
			{
				if (null != str)
					Msg = str;
			//	this.TriggerEvent("JSONioOOps");
				OOpsMB();							// either way can Log [WatchDog] Abnormal Inactivity dump
			}
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
		public void DataUpdate(PluginManager pluginManager, ref GameData data) {}

		// called in Save_Car()
		List<string> CurrentCopy()
		{
			List<string> New = new List<string> { };
			for (int i = 0; i < pCount; i++) { New.Add(string.Copy(simValues[i].Current)); }
			return New;
		}

		List<string> DefaultCopy()
		{
			List<string> New = new List<string> { };
			for (int i = 0; i < simValues.Count; i++) { New.Add(string.Copy(simValues[i].Default)); }
			return New;
		}

		// called in End() and 'CarChange' ("ChangeProperties")
		internal void Save_Car()	// update or create car; update or create game
		{
			if (null == CurrentCar || 0 == pCount || pCount > simValues.Count)	// weird state based on pCount??
				return;											// nothing to save

			var vList = DefaultCopy();							// search for game
			int gndex = slim.data.gList.FindIndex(g => g.cList[0].Name == Gname);
			if (0 > gndex)	 									// first car for this game?
			{
				changed = true;
				gndex = slim.data.gList.Count;
				slim.data.gList.Add(new GameList { cList = new List<CarL> { new CarL { Name = string.Copy(Gname), Vlist = vList } } });
			}
			else slim.Mod(gndex, 0, vList);						// game defaults may have changed

			vList = CurrentCopy();
			int cndex = slim.data.gList[gndex].cList.FindIndex(c => c.Name == CurrentCar);
			if (0 > cndex)
			{
				changed = true;
				slim.data.gList[gndex].cList.Add(new CarL { Name = string.Copy(CurrentCar), Vlist = vList });
			}
			else slim.Mod(gndex, cndex, vList);
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
				Psave(simValues);
				this.SaveCommonSettings("GeneralSettings", Settings);
			}

			// slim.data.pList.Count != simValues.Count will fail when simValues includes globals
			if (null == slim.data.pList || (0 < slim.data.pList.Count && slim.data.pList.Count != simValues.Count))
			{
				slim.data.pList = new List<string> {};
				changed = true;
			}
			Save_Car();
			if (changed)
			{
				if (0 == slim.data.pList.Count) // no previous JSON file
					for (int i = 0; i < simValues.Count; i++)
						slim.data.pList.Add(simValues[i].Name);

				string sjs = Newtonsoft.Json.JsonConvert.SerializeObject(slim.data, Newtonsoft.Json.Formatting.Indented);
				if (0 == sjs.Length || "{}" == sjs)
					OOps("End():  Json Serializer failure");
				else System.IO.File.WriteAllText(path, sjs);
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
			View = new Control(this);		// invoked *after* Init()
			SetSlider();
			View.Slslider_Point();
			if (0 < Msg.Length)
			{
				Info("OOpsMB(): " + Msg);
				View.Dispatcher.Invoke(() => View.OOpsMB());
				Msg = "";
			}
			return View;
		}

		internal void SetSlider()
		{
			if (0 > slider)
				return;

			View.Model.Slider_Property = simValues[slider].Name;
			/* slider View.SL.Maximum = 100; scale property to it, based on Steps[slider]
			 ; Steps	   Guestimated range
			 ; 1  (0.01)	0 - 2
			 ; 10 (0.10)	0 - 10	
			 ; 100 (1)		0 - 100
			 ; 1000 (10)	0 - 1000
			 */
			if (0 != Steps[slider] % 10)
			{
				Slider_factor[0] = 0.02;	// slider to value
				Slider_factor[1] = 50;	// value to slider
			} else if (0 != Steps[slider] % 100) {
				Slider_factor[0] = 0.1;	// slider to value
				Slider_factor[1] = 10;	// value to slider
			} else {
				Slider_factor[0] = 1;	// slider to value
				Slider_factor[1] = 1;	// value to slider
			}
		}

		internal string FromSlider(double value)
		{
			simValues[slider].Current = (Slider_factor[0] * (int)value).ToString();
			return simValues[slider].Name + ":  " + simValues[slider].Current;
		}

		internal double ToSlider()
		{
			if(0 > slider)
				return 0;
			View.TBL.Text = simValues[slider].Name + ":  " + simValues[slider].Current;
			return Slider_factor[1] * System.Convert.ToDouble(simValues[slider].Current);
		}

		/// <summary>
		/// Helper functions used in Init() AddAction()s and Control.xaml.cs button Clicks
		/// </summary>
		/// <param name="sign"></param> should be 1 or -1
		/// <param name="prefix"></param> should be "in" or "de"
		public void Ment(int sign)
		{
			if (0 == Gname.Length || 0 == CurrentCar.Length)
				return;
			int step = Steps[View.Selection];
			int iv = (int)(0.004 + 100 * float.Parse(simValues[View.Selection].Current));

			iv += sign * step;
			if (0 <= iv)
			{
				if (0 != step % 100)
					simValues[View.Selection].Current = $"{(float)(0.01 * iv)}";
				else simValues[View.Selection].Current = $"{(int)(0.004 + 0.01 * iv)}";
				changed = true;
				if (slider == View.Selection)
					View.Slslider_Point();
			}
		}

		private void SelectedStatus()
		{
			if (null == View)
				return;
			View.Model.Selected_Property = simValues[View.Selection].Name;
			View.Model.StatusText = Gname + " " + CurrentCar + ":\t" + View.Model.Selected_Property;
		}

		/// <summary>
		/// Select next or prior property; exception if invoked on other than UI thread
		/// </summary>
		/// <param name="next"></param> false for prior
		public void Select(bool next)
		{
			if (0 == Gname.Length || 0 == CurrentCar.Length)
				return;

			if (next)
			{
				if (++View.Selection >= simValues.Count)
					View.Selection = 0;
			}
			else if (0 < View.Selection)	// prior
				View.Selection--;
			else View.Selection = (byte)(simValues.Count - 1);
			SelectedStatus();
		}

		public void Swap()
		{
			string temp;
			for (int i = 0; i < simValues.Count; i++)
			{
				temp = simValues[i].Previous;
				simValues[i].Previous = simValues[i].Current;
				simValues[i].Current = temp;
			}
		}

		// set "CurrentAsDefaults" action
		internal void SetDefault()	// List<GameList> Glist)
		{
			if (0 == Gname.Length)
			{
				OOps("SetDefault: no Gname");
				return;
			}
			int p, Index = slim.data.gList.FindIndex(i => i.cList[0].Name == Gname);
			if (0 > Index)
			{
				OOps($"SetDefault: {Gname} not in slim.data.gList");
				return;
			}
			p = View.Selection;
			slim.data.gList[Index].cList[0].Vlist[p] = simValues[p].Default = simValues[p].Current;
/*
			List<GameList> Glist = slim.data.gList;
			int p, Index = Glist.FindIndex(i => i.cList[0].Name == Gname);

			if (0 <= Index)
			{
				changed = true;
				for (p = 0; p < pCount; p++)
					Glist[Index].cList[0].Vlist[p] =			// first "car" has per-car game default values, then per-game
					simValues[p].Default = simValues[p].Current;
				for (; p < simValues.Count; p++)
					simValues[p].Default = simValues[p].Current;
			}
 */
		}

		// add properties and settings to simValues; initialize Steps
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

				simValues.Add(new Values { Name = props[c], Default = s, Current = p, Previous = p });

				if (c < stps.Count)
					Steps.Add((int)(100 * float.Parse(stps[c])));
				else Steps.Add(10);
			}
		}

		internal bool OOpa(string msg)   // defer OOps() until GetWPFSettingsControl()
		{
			Msg += msg + "\n";
			return true;
		}
		/// <summary>
		/// Called once after plugins startup
		/// Plugins are rebuilt at game change
		/// </summary>
		/// <param name="pluginManager"></param>
		public void Init(PluginManager pluginManager)
		{
			changed = false;	// write JSON file during End() only if true

			slim = new Slim(this)
			{
				data = new GamesList()
				{
					Plugin = "JSONio",
					gList = new List<GameList>() {},	// GameList @ slim.cs line 16
					pList = new List<string> {}			// per-car property names, then per-game names
				}
			};

			// restore Properties from settings
			Settings = this.ReadCommonSettings<DataPluginSettings>("GeneralSettings", () => new DataPluginSettings());

			// restore previously saved car properties		<- do this AFTER sorting JSONio.ini??
			SetProps = new List<Property> {};				// deep copy
			foreach(Property p in Settings.properties)
				if (null != p.Name && null != p.Value)
					SetProps.Add(new Property() { Name = string.Copy(p.Name), Value = string.Copy(p.Value) });

			Steps = new List<int>() { };

			// property and setting names, default values and steps from JSONio.ini
			string pts, ds = pluginManager.GetPropertyValue(pts = Myni + "properties")?.ToString();
			string vts, vs = pluginManager.GetPropertyValue(vts = Myni + "values")?.ToString();
			string sts, ss = pluginManager.GetPropertyValue(sts = Myni + "steps")?.ToString();
			if ((!(null == ds && OOpa($"Init(): '{pts}' not found")))
			 && (!(null == vs && OOpa($"Init(): '{vts}' not found")))
			 && (!(null == ss && OOpa($"Init(): '{sts}' not found")))
				)
			{
				// JSONio.ini defines per-car Properties
				List<string> CarProps = new List<string>(ds.Split(','));
				pCount = CarProps.Count;						// these are per-car
				List<string> values = new List<string>(vs.Split(','));
				List<string> steps = new List<string>(ss.Split(','));
				if (pCount != values.Count || pCount != steps.Count)
					OOpa($"Init(): {pCount} per-car properties;  {values.Count} values;  {steps.Count} steps");
				Populate(CarProps, values, steps);
			}

			// JSONio.ini also optionally defines per-game Properties

			// JSONio.ini also optionally defines per-game settings
			string ptts, dss = pluginManager.GetPropertyValue(ptts = Myni + "gameprops")?.ToString();
			string vtts, vss = pluginManager.GetPropertyValue(vtts = Myni + "gamevals")?.ToString();
			string stts, sss = pluginManager.GetPropertyValue(stts = Myni + "gamesteps")?.ToString();
			if ((!(null == dss && OOpa($"Init(): '{ptts}' not found")))
			 && (!(null == vss && OOpa($"Init(): '{vtts}' not found")))
			 && (!(null == sss && OOpa($"Init(): '{stts}' not found")))
				)
			{
				List<string> Sprops = new List<string>(dss.Split(','));
				List<string> values = new List<string>(vss.Split(','));
				List<string> steps = new List<string>(sss.Split(','));
				if (Sprops.Count != values.Count || Sprops.Count != steps.Count)
					OOpa($"Init(): {Sprops.Count} gameprops;  {values.Count} gamevals;  {steps.Count} gamesteps");
				gCount = (Sprops.Count < values.Count) ? Sprops.Count : values.Count;
				if (gCount > steps.Count)
					gCount = steps.Count + pCount;
				else gCount += pCount;
				Populate(Sprops, values, steps);
			}

			if (0 == simValues.Count)
			{
				OOpa("Missing or invalid " + Myni + "properties from NCalcScripts/JSONio.ini");
				return;
			}

			string sl = pluginManager.GetPropertyValue(Myni + "slider")?.ToString();
			if (null != sl)
				slider = simValues.FindIndex(i => i.Name == sl);
			path = pluginManager.GetPropertyValue(sl = Myni + "file")?.ToString();
			// Load existing JSON, using slim format
			if (!slim.Load(path))
				changed = OOpa($"Init(): {path} JSON not found");

			// Declare available properties
			// these get evaluated "on demand" (when shown or used in formulae)
			foreach(Values p in simValues)
				this.AttachDelegate(p.Name, () => p.Current);

			if ((0 == Gname.Length || 0 == CurrentCar.Length) && null != View)
					View.Model.Selected_Property = "unKnown";
			else SelectedStatus();

			this.AttachDelegate("Selected", () => View.Model.Selected_Property);
			this.AttachDelegate("New Car", () => New_Car);
			this.AttachDelegate("Car", () => CurrentCar);
			this.AttachDelegate("Game", () => Gname);
			this.AttachDelegate("Msg", () => Msg);

/*---------	this.AddAction("ChangeProperties",...)
 ;		invoked for CarId changes, based on this `NCalcScripts/JSONio.ini` entry:
 ;			[ExportEvent]
 ;			name='CarChange'
 ;			trigger=changed(200, [DataCorePlugin.GameData.CarId])
 ;--------------------------------------------------------------- */	
			this.AddAction("ChangeProperties", (a, b) =>
			{
				if (0 == simValues.Count)
					return;

				int ml = 0;
				string cname = pluginManager.GetPropertyValue("CarID")?.ToString();
				string gnew = pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame")?.ToString();

				if (null !=cname && 0 < cname.Length && null != gnew)		// valid new car?
				{
					Msg = "Current Car: " + cname;
					if (0 < Gname.Length)									 // do not save first (null) CurrentCar in game
					{
						Save_Car();
						if (changed)
							Msg += $";  {CurrentCar} saved";
					}
					ml = Msg.Length;

					for (int i = 0; i < simValues.Count; i++)				// copy Current to previous
						simValues[i].Previous = simValues[i].Current;

					// indices for new car
					int gndx = (0 < gnew.Length) ? slim.data.gList.FindIndex(g => g.cList[0].Name == gnew) : -1;
					int cndx = (0 <= gndx) ? slim.data.gList[gndx].cList.FindIndex(c => c.Name == cname) : -1;

					New_Car = (-1 == cndx) ? "true" : "false";						
					CurrentCar = cname;
					if (0 <= gndx)
					{														// copy matching values from GameList
						int i;

						GameList game = slim.data.gList[gndx];
						if (0 > cndx)
							for (i = 0; i < gCount; i++)
								simValues[i].Current = simValues[i].Default = game.cList[0].Vlist[i];
						else for (i = 0; i < pCount; i++)
						{
							simValues[i].Current = game.cList[cndx].Vlist[i];
							simValues[i].Default = game.cList[0].Vlist[i];
						}
					}														// else reuse current properties

					View.Dispatcher.Invoke(() => View.Slslider_Point());	// invoke from another thread
					SelectedStatus();
					View.Model.ButtonVisibility = System.Windows.Visibility.Visible;	// ready for business
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

				if (ml < Msg.Length)
					OOps(null);
				else Msg = "";
			});					// ChangeProperties (CarID change)

			this.AddAction("IncrementSelectedProperty", (a, b) => Ment(1));
			this.AddAction("DecrementSelectedProperty", (a, b) => Ment(-1));
			this.AddAction("NextProperty",				(a, b) => Select(true)	);
			this.AddAction("PreviousProperty",			(a, b) => Select(false)	);
			this.AddAction("SwapCurrentPrevious",		(a, b) => Swap()		);
			this.AddAction("CurrentAsDefaults",			(a, b) => SetDefault());

			// Declare an event and corresponding action
			this.AddEvent("JSONioOOps");
			this.AddAction("OopsMessageBox", (a, b) => OOpsMB());
		}	// Init()
	}		// class JSONio
}
