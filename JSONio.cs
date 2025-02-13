using GameReaderCommon;
using SimHub.Plugins;
using System.Collections.Generic;
using System.Windows.Media;

namespace blekenbleu.jsonio
{
	[PluginDescription("NCalc configured properties to/from JSON")]
	[PluginAuthor("blekenbleu")]
	[PluginName("JSONio plugin")]
	public partial class JSONio : IPlugin, IDataPlugin, IWPFSettingsV2
	{
		public DataPluginSettings Settings;
		public string New_Car = "false";
		internal static int pCount, gCount;		// append per-game settings after pCount, global after gCount
		internal int slider = -1;				// simValues index for configured JSONIO.properties
		internal static string Msg = "";
		internal bool changed = false, write = false;			// slim should not change
		private static readonly string My = "JSONio.";			// breaks Ini if not preceding
		private static readonly string Myni						// configuration source
											= "DataCorePlugin.ExternalScript." + My;
		private string CurrentCar;
		private string Gname = "";
		private int gndx = -1, cndx = -1;						// current car slim.data.gList indices
		private string path;									// JSON file location
		private Slim slim;										// new JSON format
		private List<Property> SettingsProps;
		private List<int> Steps;								// 100 times actual values
		private readonly double[] Slider_factor = new double[] { 0, 0 };

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

		/// <summary>
		/// Called at plugin manager stop, close/dispose anything needed here !
		/// Plugins are rebuilt at game changes
		/// </summary>
		/// <param name="pluginManager"></param>
		public void End(PluginManager pluginManager)
		{
			Save_Car();
			// Save settings
			if (0 < Gname.Length) {
				int i;
				Settings.properties = new List<Property> {};
				Settings.game = Gname;
				Settings.carid = CurrentCar;
				for(i = 0; i < simValues.Count; i++)
					if (null != simValues[i].Name &&  null != simValues[i].Current)
						Settings.properties.Add(new Property()
						{ Name  = string.Copy(simValues[i].Name),
						  Value = string.Copy(simValues[i].Current)
						});
				Settings.GlobalDefaults = new List<Property> {};
				for(i = gCount; i < simValues.Count; i++)
					if (null != simValues[i].Name &&  null != simValues[i].Default)
						Settings.GlobalDefaults.Add(new Property()
						{ Name  = string.Copy(simValues[i].Name),
					  	  Value = string.Copy(simValues[i].Default)
						});
				this.SaveCommonSettings("GeneralSettings", Settings);
			}

			if (!changed && !write)
				return;

			string sjs = Newtonsoft.Json.JsonConvert.SerializeObject(slim.data,
						 Newtonsoft.Json.Formatting.Indented);
			if (0 == sjs.Length || "{}" == sjs)
				OOps("End():  Json Serializer failure");
			else System.IO.File.WriteAllText(path, sjs);
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
			View.Slslider_Point();
			if (0 < Msg.Length)
			{
				Info("OOpsMB(): " + Msg);
				Msg += ViewModel.statusText;
				View.Dispatcher.Invoke(() => View.OOpsMB());
				Msg = "";
			}
			View.Model.Selected_Property = "unKnown";
			return View;
		}

		// add properties and settings to simValues; initialize Steps
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
			CurrentCar = null;
			// restore Properties from settings
			Settings = this.ReadCommonSettings<DataPluginSettings>(
												"GeneralSettings", () => new DataPluginSettings());

			// restore previously saved car properties
			SettingsProps = new List<Property> {};			// deep copy
			foreach(Property p in Settings.properties)
				if (null != p.Name && null != p.Value)
					SettingsProps.Add(new Property() { Name = string.Copy(p.Name),
												  Value = string.Copy(p.Value) });

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
					OOpa($"Init(): {pCount} per-car properties;  "
						+$"{values.Count} values;  {steps.Count} steps");
				Populate(CarProps, values, steps);
			}

			// JSONio.ini also optionally defines per-game Properties
			string ptts = Myni + "gameprops";
			string dss = pluginManager.GetPropertyValue(ptts)?.ToString();
			string vtts = Myni + "gamevals";
			string vss = pluginManager.GetPropertyValue(vtts)?.ToString();
			string stts = Myni + "gamesteps";
			string sss = pluginManager.GetPropertyValue(stts)?.ToString();
			if ((!(null == dss && OOpa($"Init(): '{ptts}' not found")))
			 && (!(null == vss && OOpa($"Init(): '{vtts}' not found")))
			 && (!(null == sss && OOpa($"Init(): '{stts}' not found")))
				)
			{
				List<string> Sprops = new List<string>(dss.Split(','));
				List<string> values = new List<string>(vss.Split(','));
				List<string> steps = new List<string>(sss.Split(','));
				if (Sprops.Count != values.Count || Sprops.Count != steps.Count)
					OOpa($"Init(): {Sprops.Count} gameprops;  {values.Count} gamevals;"
									+ $"  {steps.Count} gamesteps");
				gCount = (Sprops.Count < values.Count) ? Sprops.Count : values.Count;
				if (gCount > steps.Count)
					gCount = steps.Count + pCount;
				else gCount += pCount;
				Populate(Sprops, values, steps);
			}

			// JSONio.ini also optionally defines global settings
			string pgts = Myni + "settings";
			string dgs = pluginManager.GetPropertyValue(pgts)?.ToString();
			string vgts = Myni + "setvals";
			string vgs = pluginManager.GetPropertyValue(vgts)?.ToString();
			string sgts = Myni + "setsteps";
			string sgs = pluginManager.GetPropertyValue(sgts)?.ToString();
			if ((!(null == dgs && OOpa($"Init(): '{pgts}' not found")))
			 && (!(null == vgs && OOpa($"Init(): '{vgts}' not found")))
			 && (!(null == sgs && OOpa($"Init(): '{sgts}' not found")))
				)
			{
				List<string> Gprops = new List<string>(dgs.Split(','));
				List<string> values = new List<string>(vgs.Split(','));
				List<string> steps = new List<string>(sgs.Split(','));
				if (Gprops.Count != values.Count || Gprops.Count != steps.Count)
					OOpa($"Init(): {Gprops.Count} settings;  {values.Count} setvals;"
									+ $"  {steps.Count} setsteps");
				Populate(Gprops, values, steps);
			}

			if (0 == simValues.Count)
			{
				OOpa("Missing or invalid " + Myni
					 + "properties from NCalcScripts/JSONio.ini");
				return;
			}

			string sl = pluginManager.GetPropertyValue(Myni + "slider")?.ToString();
			if (null != sl)
				slider = simValues.FindIndex(i => i.Name == sl);

			slim = new Slim(this) {};

			// at this point, simValues has all properties from .ini,
			// with original .ini default and previous property values
			// still-configured from most recent game instance
			// Load existing JSON, using slim format
			// JSON values for still-configured properties are supposed more current than .ini
			if (slim.Load(path = pluginManager.GetPropertyValue(sl = Myni + "file")?.ToString()))
			{
				OOpa($"Init() slim.Load({path}): " + Msg);
				slim.Data();
			}

			// Declare available properties
			// SimHub properties by AttachDelegate get evaluated "on demand"
			foreach(Values p in simValues)
				this.AttachDelegate(p.Name, () => p.Current);
			this.AttachDelegate("Selected", () => View.Model.Selected_Property);
			this.AttachDelegate("New Car", () => New_Car);
			this.AttachDelegate("Car", () => CurrentCar);
			this.AttachDelegate("Game", () => Gname);
			this.AttachDelegate("Msg", () => Msg);

			// Declare an event and corresponding action
			this.AddEvent("JSONioOOps");
			this.AddAction("OopsMessageBox",			(a, b) => OOpsMB());
			this.AddAction("IncrementSelectedProperty", (a, b) => Ment(1));
			this.AddAction("DecrementSelectedProperty", (a, b) => Ment(-1));
			this.AddAction("NextProperty",				(a, b) => Select(true)	);
			this.AddAction("PreviousProperty",			(a, b) => Select(false)	);
			this.AddAction("SwapCurrentPrevious",		(a, b) => Swap()		);
			this.AddAction("CurrentAsDefaults",			(a, b) => SetDefault());
			this.AddAction("ChangeProperties",			(a, b) => {
/*-------------------------------------------------------------- 
 ;		invoked for CarId changes, based on this `NCalcScripts/JSONio.ini` entry:
 ;			[ExportEvent]
 ;			name='CarChange'
 ;			trigger=changed(200, [DataCorePlugin.GameData.CarId])
 ;--------------------------------------------------------------- */	
				if (0 == simValues.Count)
					return;

				// CarID change
				CarChange(pluginManager.GetPropertyValue("CarID")?.ToString(),
						  pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame")?.ToString());
			});
			Info($"JSONIO.Init():  simValues.Count = {simValues.Count}");
		}	// Init()
	}		// class JSONio
}
