using GameReaderCommon;
using SimHub.Plugins;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Media;
using System.Windows;
using System.Windows.Forms;
using System;

namespace blekenbleu.jsonio
{
	[PluginDescription("NCalc configured properties to/from JSON")]
	[PluginAuthor("blekenbleu")]
	[PluginName("JSONio plugin")]
	public class JSONio : IPlugin, IDataPlugin, IWPFSettingsV2
	{
		public DataPluginSettings Settings;
		public string New_Car = "false";
		public double[] random;
		public ShakeIt S = new ShakeIt {};
		internal int[] Low, High;
		internal static int pCount;														// global Property settings appended after pCount
		internal int slider = -1;														// simValues index for configured JSONIO.properties
		private static string Msg = "";
		private static readonly string My = "JSONio.";									// breaks Ini if not preceding
		private static readonly string Myni = "DataCorePlugin.ExternalScript." + My;	// configuration source
		private string[] Fmin, Fmax;
		private bool changed;
		private readonly CarID CurrentCar = new CarID {};
		private string Gname = "";
		private string path;															// JSON file location
		private Slim slim;
		private List<Property> SetProps;
		private List<int> Steps;														// 100 times actual values
		private double[] Slider_factor = new double[] { 0, 0 };

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
			random[0] = S.random.NextDouble();
			random[1] = S.random.NextDouble();
			random[2] = S.random.NextDouble();
			random[3] = S.random.NextDouble();
			S.Surge = S.Prop("AccelerationSurge");
			S.Sway = S.Prop("AccelerationSway");
			S.Heave = S.Prop("AccelerationHeave");
			S.RAccG = S.RSS1();
			if ("AssettoCorsa" == pluginManager.GameName || "AssettoCorsaCompetizione" == pluginManager.GameName)
			{
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
			// Save settings
			if (0 < Gname.Length) {
				Psave(simValues);
				this.SaveCommonSettings("GeneralSettings", Settings);
			}
			if (changed = slim.Save_Car(CurrentCar, simValues, Gname) || changed)
			{
				string sjs = JsonConvert.SerializeObject(slim.data, Formatting.Indented);
				if (0 == sjs.Length || "{}" == sjs)
					OOps("End():  Json Serializer failure");
				else File.WriteAllText(path, sjs);
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
			View.Slslider_Point();
			return View;
		}

		internal void SetSlider()
		{
			if (0 > slider)
				return;

			Control.Model.Slider_Property = simValues[slider].Name;
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
			return Slider_factor[1] * Convert.ToDouble(simValues[slider].Current);
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
			if (null == Control.Model)
				return;
			Control.Model.Selected_Property = simValues[View.Selection].Name;
			Control.Model.StatusText = Gname + " " + CurrentCar.ID + ":\t" + Control.Model.Selected_Property;
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

		internal void New_defaults()	// List<GameList> Glist)
		{
			if (0 == Gname.Length)
				return;

			List<GameList> Glist = slim.data.gList;
			int p, Index = Glist.FindIndex(i => i.cList[0].Name == Gname);

			if (0 <= Index)
			{
				for (p = 0; p < pCount; p++)
					Glist[Index].cList[0].Vlist[p] =
					simValues[p].Default = simValues[p].Current;
				for (; p < simValues.Count; p++)
					simValues[p].Default = simValues[p].Current;
			}
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

				simValues.Add(new Values { Name = props[c], Default = s, Current = p, Previous = p });

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
			random = new double[] { 0, 0, 0, 0 };

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
			string pts, ds = pluginManager.GetPropertyValue(pts = Myni + "properties")?.ToString();
			string vts, vs = pluginManager.GetPropertyValue(vts = Myni + "values")?.ToString();
			string sts, ss = pluginManager.GetPropertyValue(sts = Myni + "steps")?.ToString();
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
			string ptts, dss = pluginManager.GetPropertyValue(ptts = Myni + "settings")?.ToString();
			string vtts, vss = pluginManager.GetPropertyValue(vtts = Myni + "setvals")?.ToString();
			string stts, sss = pluginManager.GetPropertyValue(stts = Myni + "setsteps")?.ToString();
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

			if (0 == simValues.Count)
			{
				string oops = "Missing or invalid " + Myni + "properties from NCalcScripts/JSONio.ini";

				OOps(oops);
				if (null != Control.Model)
					Control.Model.StatusText = oops;
				return;
			}

			string sl = pluginManager.GetPropertyValue(Myni + "slider")?.ToString();
			if (null != sl)
				slider = simValues.FindIndex(i => i.Name == sl);

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

			path = pluginManager.GetPropertyValue(Msg = Myni + "file")?.ToString();
			// Load existing JSON, using slim format
			if (slim.Load(path = pluginManager.GetPropertyValue(Msg = Myni + "file")?.ToString(), simValues))
				Msg = "Init():  " + Msg + " loaded";
			else
				changed = OOps($"Init(): {Msg} not found");

			// Declare available properties
			// these get evaluated "on demand" (when shown or used in formulae)
			foreach(Values p in simValues)
				this.AttachDelegate(p.Name, () => p.Current);

			if ((0 == Gname.Length || 0 == CurrentCar.ID.Length) && null != Control.Model)
					Control.Model.Selected_Property = "unKnown";
			else SelectedStatus();

			this.AttachDelegate("Selected", () => Control.Model.Selected_Property);
			this.AttachDelegate("New Car", () => New_Car);
			this.AttachDelegate("Car", () => CurrentCar.ID);
			this.AttachDelegate("Game", () => Gname);
			this.AttachDelegate("Msg", () => Msg);
/*
			this.AttachDelegate("random0", () => random[0]);
			this.AttachDelegate("random1", () => random[1]);
			this.AttachDelegate("random2", () => random[2]);
			this.AttachDelegate("random3", () => random[3]);
 */
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
					if (0 < Gname.Length									// do not save first (null) CurrentCar.ID in game
					 && slim.Save_Car(CurrentCar, simValues, Gname))
					{
						changed = true;
						slim.Save_Car(CurrentCar, simValues, Gname);
						Msg += $";  {CurrentCar.ID} saved";
					}
					ml = Msg.Length;

					for (int i = 0; i < pCount; i++)						// copy Current to previous
						simValues[i].Previous = simValues[i].Current;

					// indices for new car
					int gndx, cndx = slim.Car_Change(out gndx, gnew, cname);

					New_Car = (-1 == cndx) ? "true" : "false";						
					CurrentCar.ID = cname;
					if (0 <= gndx)
					{														// copy matching values from GameList
						GameList game = slim.data.gList[gndx];
						if (0 > cndx)
							for (int i = 0; i < pCount; i++)
								simValues[i].Current = simValues[i].Default = game.cList[0].Vlist[i];
						else for (int i = 0; i < pCount; i++)
						{
							simValues[i].Current = game.cList[cndx].Vlist[i];
							simValues[i].Default = game.cList[0].Vlist[i];
						}
					}														// else reuse current properties

					View.Dispatcher.Invoke(() => View.Slslider_Point());
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
