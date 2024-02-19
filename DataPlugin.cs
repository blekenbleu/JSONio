﻿using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace JSONio
{
	[PluginDescription("Device Extension Demo")]
	[PluginAuthor("Author")]
	[PluginName("JSONio plugin")]
	public class DataPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
	{
		public DataPluginSettings Settings;
		internal static readonly string My = "JSONio."; 
		internal static readonly string Ini = "DataCorePlugin.ExternalScript." + My; // configuration source
		private string path;			// JSONio.ini 'JSONio.file' property value
		private bool changed;
		public Games games;
		internal List<Property> previous, current, init;

		/// <summary>
		/// wraps SimHub.Logging.Current.Info(); prefixes MIDIio.My
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
		/// Gets a short plugin title to show in left menu. Return null if you want to use the title as defined in PluginName attribute.
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
			// Define the value of our property (declared in init)
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
			Settings.properties = current;
			this.SaveCommonSettings("GeneralSettings", Settings);
			if (changed)
				File.WriteAllText(path, JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true }));
				
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
			SimHub.Logging.Current.Info("JSONio Init()");
			changed = false;	// write JSON file during End() only if true

			// Load properties from settings
			Settings = this.ReadCommonSettings<DataPluginSettings>("GeneralSettings", () => new DataPluginSettings());
			previous = Settings.properties;
			init = new List<Property>();

			// Load properties from JSONio.ini
			string pts, ds = pluginManager.GetPropertyValue(pts = Ini + "properties")?.ToString();
			List<string> props;
			if (!(null == ds && Info($"Init(): '{pts}' not found")))
			{
//				string report = $"'{pts}'\n";
				props = new List<string>(ds.Split(','));
				foreach (string pname in props) {
					string prop = pluginManager.GetPropertyValue(pts = Ini + pname)?.ToString();
					if (!(null == prop && Info($"Init(): '{pts}' not found")))
					{
						init.Add ( new Property() { Name=pname, Value=prop });
//						report += $"\tName={pname}, Value={prop}\n";
					}
				}
//				Info("Init(): "+report);

				// append any new properties to previous
				foreach (Property p in init)
				{
					int Index = previous.FindIndex(i => i.Name == p.Name);

					if (-1 != Index)
						continue;

					Info("Init(): new property " + p.Name);
					previous.Add(p);
					if (null != games && null != games.list)
						foreach (Game g in games.list)
							if (g.append(p.Name, p.Value))
								changed = true;
				} 
			}

			current = previous;

			// Declare a property available in the property list, this gets evaluated "on demand" (when shown or used in formulas)
			foreach(Property p in current)
				this.AttachDelegate(My+p.Name, () => p.Value);

			// Declare an event
			//this.AddEvent("SpeedWarning");

			// Declare actions which can be called
			this.AddAction("ChangeProperties",(a, b) =>
			{
				SimHub.Logging.Current.Info("New Car: " + pluginManager.GetPropertyValue("CarID")?.ToString());
			});

			this.AddAction("IncrementCurrentProperty", (a, b) =>
			{
				SimHub.Logging.Current.Info("property incremented");
			});

			this.AddAction("DecrementCurrentProperty", (a, b) =>
			{
				SimHub.Logging.Current.Info("property decremented");
			});

			this.AddAction("NextProperty", (a, b) =>
			{
				SimHub.Logging.Current.Info("current property = ");
			});

			this.AddAction("PreviousProperty", (a, b) =>
			{
				SimHub.Logging.Current.Info("current property = ");
			});

			// Load existing JSON
			path = pluginManager.GetPropertyValue(Ini + "file")?.ToString();
			if (File.Exists(path))
			{
				games = JsonSerializer.Deserialize<Games>(File.ReadAllText(path));
			} else changed = true;
		}
	}
}
