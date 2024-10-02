using SimHub.Plugins;
using System;

/*
 ; Replace SimHub ShakeIt CUSTOM EFFECTs JavaScript
 ; for LoadedSlipGripJSONio
 ; https://github.com/blekenbleu/SimHub-Profiles/blob/main/LoadedSlipGripJSONio.siprofile
 ; https://github.com/blekenbleu/SimHub-Profiles#suggested-tire-slip-shakeit-profiles
 ; https://blekenbleu.github.io/pedals/shakeit.htm#S/G
 ; https://www.overtake.gg/threads/simhub-shakeit-bass-shakers-custom-4-corner-tire-slip.198455/page-8
 */
namespace blekenbleu.jsonio
{
    /// <summary>
    /// based on LoadedSlipGripJSONio.siprofile
    /// </summary>
    public class ShakeIt
    {
		private JSONio J;
		private PluginManager pluginManager;
		internal Random random;
		private string[] corner;

		private int EffectStrength, gamma, SlipGain, threshold, Gscale;	// simprop indices

		private double Shaken(string pname)
		{
			var o = pluginManager.GetPropertyValue("ShakeITBSV3Plugin.Export." + pname);
		
            return (null == o) ? 0 : Convert.ToDouble(o);
		}

		private double Prop(string pname)
		{
			var o = pluginManager.GetPropertyValue("DataCorePlugin.GameData." + pname);

            return (null == o) ? 0 : Convert.ToDouble(o);
		}

		float Current (int prop)
		{
			return float.Parse(J.simprops[prop].Current);
		}

		internal void Init(JSONio j, PluginManager p)
		{
			J = j;
			pluginManager = p;
			random = new Random();	// random.NextDouble() returns a double between 0 and 1
			corner = new string[] {".FrontLeft", ".FrontRight", ".RearLeft", ".RearRight" };

			gamma = J.simprops.FindIndex(i => i.Name == "gamma");					// ProxyS() applies it to wslip
			SlipGain = J.simprops.FindIndex(i => i.Name == "SlipGain");				// ProxyS() applies it to wslip
			EffectStrength = J.simprops.FindIndex(i => i.Name == "EffectStrength");	// SlipGrip amplitude
			threshold = J.simprops.FindIndex(i => i.Name == "threshold");			// LoadedSlipGrip() applies it
			Gscale = J.simprops.FindIndex(i => i.Name == "Gscale");					// LoadedSlipGrip() applies it
/*			------- used for debugging -----------------
			J.AttachDelegate("GameName", () => pluginManager.GameName);
			J.AttachDelegate("S.EffectStrength", () =>	Current(EffectStrength));
			J.AttachDelegate("S.gamma", () => 			Current(gamma));
			J.AttachDelegate("S.SlipGain", () =>		Current(SlipGain));
			J.AttachDelegate("Surge", () =>			Prop("AccelerationSurge"));
			J.AttachDelegate("Sway", () =>			Prop("AccelerationSway"));
			J.AttachDelegate("Haccel", () => 		Haccel(Prop("AccelerationSurge"), Prop("AccelerationSway")));
			J.AttachDelegate("Grip"+corner[0], () => Grip(-1,  1));
			J.AttachDelegate("Grip"+corner[1], () => Grip( 1,  1));
			J.AttachDelegate("Grip"+corner[2], () => Grip(-1, -1));
			J.AttachDelegate("Grip"+corner[3], () => Grip( 1, -1));
 */

			if ("AssettoCorsa" == pluginManager.GameName || "AssettoCorsaCompetizione" == pluginManager.GameName)
			{
/*
				J.AttachDelegate("ACslipGrip"+corner[0], () => ACslipGrip(0));
				J.AttachDelegate("ACslipGrip"+corner[1], () => ACslipGrip(1));
				J.AttachDelegate("ACslipGrip"+corner[2], () => ACslipGrip(2));
				J.AttachDelegate("ACslipGrip"+corner[3], () => ACslipGrip(3));
 */
				J.AttachDelegate("FF"+corner[0], () => FF(ACslipGrip(0), 0, J.random0));
				J.AttachDelegate("FF"+corner[1], () => FF(ACslipGrip(1), 1, J.random1));
				J.AttachDelegate("FF"+corner[2], () => FF(ACslipGrip(2), 2, J.random2));
				J.AttachDelegate("FF"+corner[3], () => FF(ACslipGrip(3), 3, J.random3));
				J.AttachDelegate("LoadedSlipGrip"+corner[0], () => LoadedSlipGrip(ACslipGrip(0), -1,  1));
				J.AttachDelegate("LoadedSlipGrip"+corner[1], () => LoadedSlipGrip(ACslipGrip(1),  1,  1));
				J.AttachDelegate("LoadedSlipGrip"+corner[2], () => LoadedSlipGrip(ACslipGrip(2), -1, -1));
				J.AttachDelegate("LoadedSlipGrip"+corner[3], () => LoadedSlipGrip(ACslipGrip(3),  1, -1));
			} else {
/*
				J.AttachDelegate("SHslipGrip"+corner[0], () => SHslipGrip(0, Grip(-1,  1)));
				J.AttachDelegate("SHslipGrip"+corner[1], () => SHslipGrip(1, Grip( 1,  1)));
				J.AttachDelegate("SHslipGrip"+corner[2], () => SHslipGrip(2, Grip(-1, -1)));
				J.AttachDelegate("SHslipGrip"+corner[3], () => SHslipGrip(3, Grip( 1, -1)));
 */
				J.AttachDelegate("FF"+corner[0], () => FF(SHslipGrip(0, Grip(-1,  1)), 0, J.random0));
				J.AttachDelegate("FF"+corner[1], () => FF(SHslipGrip(1, Grip( 1,  1)), 1, J.random1));
				J.AttachDelegate("FF"+corner[2], () => FF(SHslipGrip(2, Grip(-1, -1)), 2, J.random2));
				J.AttachDelegate("FF"+corner[3], () => FF(SHslipGrip(3, Grip( 1, -1)), 3, J.random3));
				J.AttachDelegate("LoadedSlipGrip"+corner[0], () => LoadedSlipGrip(SHslipGrip(0, Grip(-1,  1)), -1,  1));
				J.AttachDelegate("LoadedSlipGrip"+corner[1], () => LoadedSlipGrip(SHslipGrip(1, Grip( 1,  1)),  1,  1));
				J.AttachDelegate("LoadedSlipGrip"+corner[2], () => LoadedSlipGrip(SHslipGrip(2, Grip(-1, -1)), -1, -1));
				J.AttachDelegate("LoadedSlipGrip"+corner[3], () => LoadedSlipGrip(SHslipGrip(3, Grip( 1, -1)),  1, -1));
            }	// pluginManager.GameName
		}

		private double RSS(double x, double y)
		{
			return Math.Pow(x*x + y*y, 0.5);
		}

		private double RSS1(double x, double y)
		{
			return Math.Max(0.11, RSS(x, y));
		}
/*
		public double Haccel(double surge, double sway)
		{
			return 1 + 0.99 * RSS(surge, sway);
		}
 */
		private double Raw(string physics)
		{
			var o = pluginManager.GetPropertyValue("GameRawData." + physics);

			return (null == o) ? 0 : Convert.ToDouble(o);
		}

		public double Grip(double sway, double surge)
		{
			double heave = Prop("AccelerationHeave");
			sway *= Prop("AccelerationSway");
			surge *= Prop("AccelerationSurge");
			double load = 25 + heave + sway + surge * 0.67;
			return 1 + 0.99 * 25 * RSS(surge, sway) / (25 > load ? 25 : load);
		}

		private double ProxyS(int c)
		{
			return 100 * Math.Pow(0.01 * Shaken("wSlip"+corner[c]) * Current(SlipGain), 1 / Current(gamma));
		}

		public double SHslipGrip(int corner, double grip)
		{
			return 100 * Current(EffectStrength) * ProxyS(corner) / grip; 
		}

		public double ACslipGrip(int proxyS)
		{
			string Whload = (1 + proxyS).ToString();
			double sg = 0.000005 * Current(EffectStrength) * ProxyS(proxyS) * Raw("Physics.WheelLoad0"+Whload)
						 / RSS1(Raw("Physics.AccG01"), Raw("Physics.AccG02"));
			return Math.Min(100, 100 * Math.Pow(sg, 0.5));
		}

		private double Acc(double s)	// fractional power, preserving sign
		{
			return (0 < s) ? Math.Pow(.05 * s, 0.3) : -Math.Pow(-.05 * s, 0.3);
		}

		// this corresponds to JavaScript in LoadedSlipGrip CUSTOM EFFECT
		public double LoadedSlipGrip(double sg, double sway, double surge)
		{
			double L = 25  + 25 * sway * Acc(Prop("AccelerationSway"));	// 25 +/-25% left-right distribution
			L *= (1 + surge * Acc(Prop("AccelerationSurge")));			// fore-aft distribution
			return Current(Gscale) * (Math.Max(0, Math.Min(1, 0.2 * sg)) * L - Current(threshold));
		}

		// forced frequency tire squeal to be amplitude-modulated by LoadedSlipGrip()
		double FF(double sg, int corner, double noise)	// which: ACslipGrip() or SHslipGrip()
		{
			double low = Current(J.Low[corner]), high = Current(J.High[corner]);
			double range = 0.01 * (high - low) * 3 / (1 + 3);	// scale based on 3 and range
			sg = low + range * (100 - sg);
			return sg + noise * sg / 3;
		}
    }
}
