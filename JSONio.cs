﻿using GameReaderCommon;
using SimHub.Plugins;
using System.Collections.Generic;
using System.Windows.Media;

namespace blekenbleu.jsonio
{
	[PluginDescription("NCalc configured properties to/from JSON")]
	[PluginAuthor("blekenbleu")]
	[PluginName("JSONio_SlipGrip")]
	public partial class JSONio : IPlugin, IDataPlugin, IWPFSettingsV2
	{
		public DataPluginSettings Settings;
		public string NewCar = "false";

		public double[] random;					// ShakeIt profile replacement
		public ShakeIt S = new ShakeIt {};

		internal bool write = false;			// slim should not change
		internal static string Msg = "";
		internal static int pCount;				// append per-game settings after pCount
		internal static int gCount;				// append global settings after gCount
		internal int slider = -1;			   // simValues index for configured JSONIO.properties
		internal int[] Low, High;
		string[] Fmin, Fmax;

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
			View.Dispatcher.Invoke(() => View.OOpsMB());
			Msg = "";
		}

		void OOps(string str)
		{
			Msg = str;
			OOpsMB();				// may [WatchDog] Abnormal Inactivity dump
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
		public string LeftMenuTitle => "JSONio_SlipGrip " + Control.version;

		/// <summary>
		/// Called one time per game data update, contains all normalized game data,
		/// raw data are intentionnally "hidden" under a generic object type (plugins SHOULD NOT USE)
		/// This method is on the critical path, must execute as fast as possible and avoid throwing any error
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <param name="data">Current game data, including present and previous data frames.</param>
		public void DataUpdate(PluginManager pluginManager, ref GameData data)
		{
			random[0] = S.random.NextDouble();
			random[1] = S.random.NextDouble();
			random[2] = S.random.NextDouble();
			random[3] = S.random.NextDouble();
			S.Surge = S.Prop("AccelerationSurge");
			S.Sway = S.Prop("AccelerationSway");
			S.Heave = S.Prop("AccelerationHeave");
			// AssettoCorsaCompetizione has DataCorePlugin.GameRawData.Physics.WheelLoad properties, but they are zero
			// estimate load from DataCorePlugin.GameRawData.Physics.SuspensionTravel
			// fully unloaded suspension travel seems to be 0; fully loaded varies by car...
			// AssettoCorsa load compression peaks are higher than suspension travel, thanks to dampers...
			if ("AssettoCorsa" == pluginManager.GameName || "AssettoCorsaCompetizione" == pluginManager.GameName)
			{
				S.RAccG = S.RSS1();
				S.SG[0] = S.ACslipGrip(0);
				S.SG[1] = S.ACslipGrip(1);
				S.SG[2] = S.ACslipGrip(2);
				S.SG[3] = S.ACslipGrip(3);
			} else {
				S.SG[0] = S.SHslipGrip(0);
				S.SG[1] = S.SHslipGrip(1);
				S.SG[2] = S.SHslipGrip(2);
				S.SG[3] = S.SHslipGrip(3);
			}
		}

		/// <summary>
		/// Called at plugin manager stop, close/dispose anything needed here !
		/// Plugins are rebuilt at game changes
		/// </summary>
		/// <param name="pluginManager"></param>
		public void End(PluginManager pluginManager)
		{
			SaveCar();
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
				Settings.gDefaults = new List<Property> {};
				for(i = gCount; i < simValues.Count; i++)
					if (null != simValues[i].Name &&  null != simValues[i].Default)
						Settings.gDefaults.Add(new Property()
						{ Name  = string.Copy(simValues[i].Name),
					  	  Value = string.Copy(simValues[i].Default)
						});
				this.SaveCommonSettings("GeneralSettings", Settings);

				// capture per-game Default value changes
				if (gCount != slim.data.gList[gndx].cList[0].Vlist.Count)
					write = true;
				else
				{
					for (i = 0; i < gCount; i++)
						if (simValues[i].Default != slim.data.gList[gndx].cList[0].Vlist[i])
							break;
					if (i < gCount)
						write = true;
				}
				if (write)
					slim.data.gList[gndx].cList[0].Vlist = DefaultCopy();
			}

			if (!(Changed() || write))
				return;

			string sjs = Newtonsoft.Json.JsonConvert.SerializeObject(slim.data,
						 Newtonsoft.Json.Formatting.Indented);
			if (0 == sjs.Length || "{}" == sjs)
				OOps("End():  Json Serializer failure");
			else System.IO.File.WriteAllText(path, sjs);
		}	// End()

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
			Low = new int[] { 0, 0, 0, 0 };				// ShakeIt properties
			High = new int[] { 0, 0, 0, 0 };
			random = new double[] { 0, 0, 0, 0 };
			Fmax = new string[] { "Fmax.FrontLeft", "Fmax.FrontRight", "Fmax.RearLeft", "Fmax.RearRight" };
			Fmin = new string[] { "Fmin.FrontLeft", "Fmin.FrontRight", "Fmin.RearLeft", "Fmin.RearRight" };

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
			if ((!(null == ds && OOpa($"'{pts}' not found")))
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

			// JSONio.ini also optionally defines per-game Properties
			string ptts = Myni + "gameprops";
			string dss = pluginManager.GetPropertyValue(ptts)?.ToString();
			string vtts = Myni + "gamevals";
			string vss = pluginManager.GetPropertyValue(vtts)?.ToString();
			string stts = Myni + "gamesteps";
			string sss = pluginManager.GetPropertyValue(stts)?.ToString();
			if ((!(null == dss && OOpa($"'{ptts}' not found")))
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

			// JSONio.ini also optionally defines global settings
			string pgts = Myni + "settings";
			string dgs = pluginManager.GetPropertyValue(pgts)?.ToString();
			string vgts = Myni + "setvals";
			string vgs = pluginManager.GetPropertyValue(vgts)?.ToString();
			string sgts = Myni + "setsteps";
			string sgs = pluginManager.GetPropertyValue(sgts)?.ToString();
			if ((!(null == dgs && (0 == Settings.gDefaults.Count || OOpa($"'{pgts}' not found"))))
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

			// find Fmin, Fmax settings
			for (int i = 0; i < Fmin.Length; i++)
			{
				int j = simValues.FindIndex(k => k.Name == Fmin[i]);
				if (0 <= j)
					Low[i] = j;
				j = simValues.FindIndex(k => k.Name == Fmax[i]);
				if (0 <= j)
					High[i] = j;
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

			S.Init(this, pluginManager);						// ShakeIt effect class

			// Declare available properties
			// SimHub properties by AttachDelegate get evaluated "on demand"
			foreach (Values p in simValues)
				this.AttachDelegate(p.Name, () => p.Current);
			this.AttachDelegate("Selected", () => View.Model.SelectedProperty);
			this.AttachDelegate("New Car", () => NewCar);
			this.AttachDelegate("Car", () => CurrentCar);
			this.AttachDelegate("Game", () => Gname);
			this.AttachDelegate("Msg", () => Msg);
/*
			this.AttachDelegate("random0", () => random[0]);
			this.AttachDelegate("random1", () => random[1]);
			this.AttachDelegate("random2", () => random[2]);
			this.AttachDelegate("random3", () => random[3]);
 */

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
					pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame")?.ToString()
				)
			);

			Info($"JSONIO.Init():  simValues.Count = {simValues.Count}");
		}	// Init()
	}		// class JSONio
}
