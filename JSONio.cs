using GameReaderCommon;
using SimHub.Plugins;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace blekenbleu.jsonio
{
	[PluginDescription("NCalc configured properties to/from JSON")]
	[PluginAuthor("blekenbleu")]
	[PluginName("JSONio")]
	public partial class JSONio : IPlugin, IDataPlugin, IWPFSettingsV2
	{
		public DataPluginSettings Settings;
		public string NewCar = "false";

		internal static string Msg = "";
		internal static int pCount;				// append per-game settings after pCount
		internal static int gCount;				// append global settings after gCount
		internal int slider = -1;				// simValues index for configured JSONIO.properties
		internal bool set = false;

		private string CurrentCar;
		private string Gname = "";
		private int gndx = -1, cndx = -1;						// current car slim.data.gList indices
		private static readonly string My = "JSONio.";			// breaks Ini if not preceding
																// configuration source
		private static readonly string Myni = "DataCorePlugin.ExternalScript." + My;
		private string path;									// JSON file location
		private readonly double[] SliderFactor = new double[] { 0, 0 };
		private Slim slim;										// new JSON format
		private List<Property> SettingsProps;					// non-null Settings entries
		private List<int> Steps;								// 100 times actual values
		private bool write = false;								// slim should not change

		/// <summary>
		/// DisplayGrid contents
		/// </summary>
		public List<Values> simValues = new List<Values>();		// must be initialized before Init()

		/// <summary>
		/// Plugin-specific wrapper for SimHub.Logging.Current.Info();
		/// </summary>
		internal static bool Info(string str)
		{
			SimHub.Logging.Current.Info(JSONio.My + str);   // bool Info()
			return true;
		}

		void OOpsMB()
		{
			Info("OOpsMB(): " + Msg);
			View?.Dispatcher.Invoke(() => View.OOpsMB());
			Msg = "";
		}

		void OOps(string str)
		{
			Msg = str;
			OOpsMB();				// may [WatchDog] Abnormal Inactivity dump
		}

		/// <summary>
		/// Plugin manager instance
		/// </summary>
		public PluginManager PluginManager { get; set; }

		/// <summary>
		/// Gets the left menu icon. Icon must be 24x24 and compatible with black and white display.
		/// </summary>
		public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);

		/// <summary>
		/// Short plugin title to show in left menu. Return null to use the PluginName attribute.
		/// </summary>
		public string LeftMenuTitle => "JSONio " + Control.version;

		/// <summary>
		/// Called one time per game data update, contains all normalized game data,
		/// raw data are intentionnally "hidden" under a generic object type (plugins SHOULD NOT USE)
		/// This method is on the critical path, must execute as fast as possible and avoid throwing any error
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <param name="data">Current game data, including current and previous data frames.</param>
		public void DataUpdate(PluginManager pluginManager, ref GameData data)
		{}

		/// <summary>
		/// Called at plugin manager stop, close/dispose anything needed here !
		/// Plugins are rebuilt at game changes
		/// </summary>
		/// <param name="pluginManager"></param>
		public void End(PluginManager pluginManager)
		{
			SaveSlim();			// set write for changes
			// Save settings
			if (0 < Gname.Length && write) {
				int i;

				set = true;	// End(): save Current values
				Settings.properties = new List<Property> {};
				Settings.game = Gname;
				Settings.carid = CurrentCar;
				for(i = 0; i < simValues.Count; i++)
					if (null != simValues[i].Name &&  null != simValues[i].Current)
						Settings.properties.Add(new Property()
						{ Name  = string.Copy(simValues[i].Name),
						  Value = string.Copy(simValues[i].Current)
						});

				Settings.gDefaults = new List<Property> {};
				for(i = gCount; i < simValues.Count; i++)
					if (null != simValues[i].Name &&  null != simValues[i].Default)
						Settings.gDefaults.Add(new Property()
						{ Name  = string.Copy(simValues[i].Name),
					  	  Value = string.Copy(simValues[i].Default)
						});

				// capture per-game Default changes
				slim.data.gList[gndx].cList[0].Vlist = DefaultCopy();
			}

			if (set)	// .ini mismatches Settings or game run
				this.SaveCommonSettings("GeneralSettings", Settings);

			if (!write)				// End()
				return;

			string sjs = Newtonsoft.Json.JsonConvert.SerializeObject(slim.data,
						 Newtonsoft.Json.Formatting.Indented);
			if (0 == sjs.Length || "{}" == sjs)
				OOps("End():  Json Serializer failure");
			else System.IO.File.WriteAllText(path, sjs);
		}	// End()

		// try CarChange() for Game already running when JSONio is (re)launched
		// https://ironpdf.com/blog/net-help/csharp-wait-for-seconds/
		async Task AsyncRunningGame(PluginManager pm, int milliseconds)
		{
			await Task.Delay(milliseconds); // wait without blocking main thread
//			Info("AsyncRunningGame(CarChange())");
			CarChange(pm.GetPropertyValue("CarID")?.ToString(),
					  pm.GetPropertyValue("DataCorePlugin.CurrentGame")?.ToString(),
					  true);				// disable popup
        }

		/// <summary>
		/// Returns settings control or null if not required
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <returns>UserControl instance</returns>
		private Control View;	// instance of Control.xaml.cs Control()
		public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
		{
			View = new Control(this);		// invoked *after* Init()
			SetSlider();
			if (0 < Msg.Length)
			{
				Info("OOpsMB() " + Msg);
				Msg = "Init() " + Msg + ViewModel.statusText;
				View.Dispatcher.Invoke(() => View.OOpsMB());
				Msg = "";
			}
			// assignment preempts Compiler Warning CS4014
//			Info("GetWPFSettingsControl():  delayTask");
            Task delayTask = AsyncRunningGame(pluginManager, 1000);
			return View;
		}

		// add properties and settings to simValues; initialize Steps
		// if a property move among
		private void Populate(List<string>props, List<string> vals, List<string> stps)
		{
			for (int c = 0; c < props.Count; c++)
			{
				// populate DisplayGrid ItemsSource
				// JSONio.ini contents may not match saved car properties
				// default value from .ini
				int Index = SettingsProps.FindIndex(i => i.Name == props[c]);
				string ini = (c < vals.Count) ? vals[c] : (0 <= Index) ? SettingsProps[Index].Value : "0";
				// use SettingsProps value, if it exists, else from .ini
				string setting = (0 <= Index) ? SettingsProps[Index].Value : ini;

				simValues.Add(new Values {	Name = props[c],
											Default = ini,			// replaced by JSON values
											Current = setting,
											Previous = setting });
				Steps.Add((c < stps.Count)  ? (int)(100 * float.Parse(stps[c]))
											: 10);
			}
		}

		internal bool OOpa(string msg)   // defer MessageBox.Show() until GetWPFSettingsControl()
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
			CurrentCar = null;			// otherwise whatever was set before game change
			// restore Properties from settings
			Settings = this.ReadCommonSettings<DataPluginSettings>(
												"GeneralSettings", () => new DataPluginSettings());

			// restore previously saved car properties
			SettingsProps = new List<Property> {};			// deep copy
			foreach(Property p in Settings.properties)
				if (null != p.Name && null != p.Value)
					SettingsProps.Add(new Property() { Name = string.Copy(p.Name),
												  Value = string.Copy(p.Value) });

			Steps = new List<int>() {};		// for Populate()

			// property and setting names, default values and steps from JSONio.ini
			string pts, ds = pluginManager.GetPropertyValue(pts = Myni + "properties")?.ToString();
			string vts, vs = pluginManager.GetPropertyValue(vts = Myni + "values")?.ToString();
			string sts, ss = pluginManager.GetPropertyValue(sts = Myni + "steps")?.ToString();
			if ((!(null == ds && (0 == Settings.pcount || OOpa($"per-car properties not found"))))
			 && (!(null == vs && OOpa($"'{vts}' not found")))
			 && (!(null == ss && OOpa($"'{sts}' not found")))
			   )
			{
				// JSONio.ini defines per-car Properties
				List<string> CarProps = new List<string>(ds.Split(','));
				pCount = CarProps.Count;						// these are per-car
				List<string> values = new List<string>(vs.Split(','));
				List<string> steps = new List<string>(ss.Split(','));
				if (pCount != values.Count || pCount != steps.Count)
					OOpa($"{pCount} per-car properties;  "
						+$"{values.Count} values;  {steps.Count} steps");
				Populate(CarProps, values, steps);
			}
			if (Settings.pcount != simValues.Count)
			{
				set = true;
				Settings.pcount = simValues.Count;
			}

			// JSONio.ini also optionally defines per-game Properties
			string ptts = Myni + "gameprops";
			string dss = pluginManager.GetPropertyValue(ptts)?.ToString();
			string vtts = Myni + "gamevals";
			string vss = pluginManager.GetPropertyValue(vtts)?.ToString();
			string stts = Myni + "gamesteps";
			string sss = pluginManager.GetPropertyValue(stts)?.ToString();
			if ((!(null == dss && (0 == Settings.gcount || OOpa($"per-game properties not found"))))
			 && (!(null == vss && OOpa($"'{vtts}' not found")))
			 && (!(null == sss && OOpa($"'{stts}' not found")))
				)
			{
				List<string> Sprops = new List<string>(dss.Split(','));
				List<string> values = new List<string>(vss.Split(','));
				List<string> steps = new List<string>(sss.Split(','));
				if (Sprops.Count != values.Count || Sprops.Count != steps.Count)
					OOpa($"{Sprops.Count} gameprops;  {values.Count} gamevals;"
									+ $"  {steps.Count} gamesteps");
				gCount = (Sprops.Count < values.Count) ? Sprops.Count : values.Count;
				if (gCount > steps.Count)
					gCount = steps.Count + pCount;
				else gCount += pCount;
				Populate(Sprops, values, steps);
			}
			if (Settings.gcount != simValues.Count - Settings.pcount) {
				set = true;
				Settings.gcount = simValues.Count - Settings.pcount;
			}			

			// JSONio.ini also optionally defines global settings
			string pgts = Myni + "settings";
			string dgs = pluginManager.GetPropertyValue(pgts)?.ToString();
			string vgts = Myni + "setvals";
			string vgs = pluginManager.GetPropertyValue(vgts)?.ToString();
			string sgts = Myni + "setsteps";
			string sgs = pluginManager.GetPropertyValue(sgts)?.ToString();
			if ((!(null == dgs && (0 == Settings.gDefaults.Count || OOpa($"global properties not found"))))
			 && (!(null == vgs && OOpa($"'{vgts}' not found")))
			 && (!(null == sgs && OOpa($"'{sgts}' not found")))
				)
			{
				List<string> Gprops = new List<string>(dgs.Split(','));
				List<string> values = new List<string>(vgs.Split(','));
				List<string> steps = new List<string>(sgs.Split(','));
				if (Gprops.Count != values.Count || Gprops.Count != steps.Count)
					OOpa($"{Gprops.Count} settings;  {values.Count} setvals;"
									+ $"  {steps.Count} setsteps");
				Populate(Gprops, values, steps);
			}

			if (Settings.gDefaults.Count != simValues.Count - (Settings.gcount + Settings.pcount))
			{
				Settings.gDefaults = new List<Property>() {};
				set = true;
			}

			if (0 == simValues.Count)
			{
				OOpa("Missing or invalid " + Myni
					 + "properties from NCalcScripts/JSONio.ini");
				return;
			}

			// Recover default global values from Settings
			// for properties which remain global since previous game instance.
			{
				int gd, scount = SettingsProps.Count;

				for (gd = 0; gd < Settings.gDefaults.Count; gd++)
				{
					int Index = simValues.FindIndex(s => s.Name == Settings.gDefaults[gd].Name);
					if (Index >= gCount)	// still global?
						simValues[Index].Default = Settings.gDefaults[gd].Value;
				}

				string sl = pluginManager.GetPropertyValue(Myni + "slider")?.ToString();

				if (null != sl)
					slider = simValues.FindIndex(i => i.Name == sl);
			}

			// at this point, simValues has all properties from .ini,
			// with original .ini default and previous property values
			// still-configured from most recent game instance
			// Load existing JSON, using slim format
			// JSON values for still-configured properties are supposed more current than .ini
			slim = new Slim(this) {};
			if (slim.Load(path = pluginManager.GetPropertyValue(Myni + "file")?.ToString()))
			{
				if (0 < Msg.Length)
					OOpa($"Init() slim.Load({path}): " + Msg);
				slim.Data();
			}

			// Declare available properties
			// SimHub properties by AttachDelegate get evaluated "on demand"
			foreach (Values p in simValues)
				this.AttachDelegate(p.Name, () => p.Current);
			this.AttachDelegate("Selected", () => View.Model.SelectedProperty);
			this.AttachDelegate("New Car", () => NewCar);
			this.AttachDelegate("Car", () => CurrentCar);
			this.AttachDelegate("Game", () => Gname);
			this.AttachDelegate("Msg", () => Msg);

			// Declare an event and corresponding action
			this.AddAction("IncrementSelectedProperty", (a, b) => Ment(1));
			this.AddAction("DecrementSelectedProperty", (a, b) => Ment(-1));
			this.AddAction("NextProperty",				(a, b) => Select(true)	);
			this.AddAction("PreviousProperty",			(a, b) => Select(false)	);
			this.AddAction("SwapCurrentPrevious",		(a, b) => Swap()		);
			this.AddAction("CurrentAsDefaults",			(a, b) => SetDefault());
			this.AddAction("SelectedAsSlider",			(a, b) => SelectSlider());
			this.AddAction("ChangeProperties",			(a, b) => CarChange(
					pluginManager.GetPropertyValue("CarID")?.ToString(),
					pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame")?.ToString(),
					false
				)
			);

			Info($"JSONIO.Init():  simValues.Count = {simValues.Count}");
		}	// Init()
	}		// class JSONio
}
