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
		internal double Surge, Sway, Heave, RAccG;
		internal double[] SG;
		private string[] corner;

		private int EffectStrength, gamma, SlipGain, threshold;
		internal int Gscale;	// simprop indices

		private double Shaken(string pname)
		{
			var o = pluginManager.GetPropertyValue("ShakeITBSV3Plugin.Export." + pname);
		
            return (null == o) ? 0 : Convert.ToDouble(o);
		}

		internal double Prop(string pname)
		{
			var o = pluginManager.GetPropertyValue("DataCorePlugin.GameData." + pname);

            return (null == o) ? 0 : Convert.ToDouble(o);
		}

		internal float Current (int prop)
		{
			return float.Parse(J.simprops[prop].Current);
		}

		internal void Init(JSONio j, PluginManager p)
		{
			J = j;
			pluginManager = p;
			random = new Random();	// random.NextDouble() returns a double between 0 and 1
			corner = new string[] {".FrontLeft", ".FrontRight", ".RearLeft", ".RearRight" };
			SG = new double[] { 0, 0, 0, 0 };

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
			J.AttachDelegate("Surge", () =>				Surge);
			J.AttachDelegate("Sway", () =>				Sway);
			J.AttachDelegate("Haccel", () => 			Haccel(Surge, Sway)
 */
			J.AttachDelegate("ProxyS"+corner[0], () => ProxyS(0));
			J.AttachDelegate("ProxyS"+corner[1], () => ProxyS(1));
			J.AttachDelegate("ProxyS"+corner[2], () => ProxyS(2));
			J.AttachDelegate("ProxyS"+corner[3], () => ProxyS(3));

			J.AttachDelegate("SlipGrip"+corner[0], () => SG[0]);
			J.AttachDelegate("SlipGrip"+corner[1], () => SG[1]);
			J.AttachDelegate("SlipGrip"+corner[2], () => SG[2]);
			J.AttachDelegate("SlipGrip"+corner[3], () => SG[3]);

			J.AttachDelegate("FF"+corner[0], () => FF(0));
			J.AttachDelegate("FF"+corner[1], () => FF(1));
			J.AttachDelegate("FF"+corner[2], () => FF(2));
			J.AttachDelegate("FF"+corner[3], () => FF(3));

			J.AttachDelegate("LoadedSlipGrip"+corner[0], () => LoadedSlipGrip(SG[0], -1,  1));
			J.AttachDelegate("LoadedSlipGrip"+corner[1], () => LoadedSlipGrip(SG[1],  1,  1));
			J.AttachDelegate("LoadedSlipGrip"+corner[2], () => LoadedSlipGrip(SG[2], -1, -1));
			J.AttachDelegate("LoadedSlipGrip"+corner[3], () => LoadedSlipGrip(SG[3],  1, -1));
		}

		private double RSS(double x, double y)
		{
			return Math.Pow(x*x + y*y, 0.5);
		}

		internal double RSS1()
		{
			return Math.Max(0.11, RSS(Raw("Physics.AccG01"), Raw("Physics.AccG02")));
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

		private readonly int[,] g = new int[,] { { -1, 1 }, { 1, 1 }, { -1, -1 }, { -1, 1 } }; 
		private double Grip(int i)
		{
			double sway = Sway * g[i, 0];
			double surge = Surge * g[i, 1];
			double load = 25 + Heave + sway + surge * 0.67;
			return 1 + 0.99 * 25 * RSS(surge, sway) / (25 > load ? 25 : load);
		}

		private double ProxyS(int c)
		{
			return 100 * Math.Pow(Math.Min(1, 0.01 * Shaken("wSlip"+corner[c]) * Current(SlipGain)), 1 / Current(gamma));
		}

		public double SHslipGrip(int corner)
		{
			return 100 * Math.Min(1, Current(EffectStrength) * 0.1 * ProxyS(corner) / Grip(corner));
		}

		public double ACslipGrip(int proxyS)
		{
			string Whload = (1 + proxyS).ToString();
			double sg = 0.000005 * Current(EffectStrength) * ProxyS(proxyS) * Raw("Physics.WheelLoad0"+Whload) / RAccG;
			return 100 * Math.Pow(Math.Min(1, sg), 0.5);
		}

		private double Acc(double s)	// fractional power, preserving sign
		{
			return (0 < s) ? Math.Pow(.05 * s, 0.3) : -Math.Pow(-.05 * s, 0.3);
		}

		// this corresponds to JavaScript in LoadedSlipGrip CUSTOM effect
		public double LoadedSlipGrip(double sg, int sway, int surge)
		{
			double L = 25  + 25 * sway * Acc(Sway);	// 25 +/-25% left-right distribution
			L *= (1 + surge * Acc(Surge));			// fore-aft distribution
			return Current(Gscale) * (Math.Max(0, Math.Min(1, 0.2 * sg)) * L - Current(threshold));
		}

		// forced frequency tire squeal to be amplitude-modulated by LoadedSlipGrip()
		double FF(int corner)	// which: ACslipGrip() or SHslipGrip()
		{
			double low = Current(J.Low[corner]), high = Current(J.High[corner]);
			double range = 0.01 * (high - low) * 3 / (1 + 3);	// scale based on 3 and range
			double sg = low + range * (100 - Math.Min(100, SG[corner]));
			return sg + J.random[corner] * sg / 3;
		}
    }
}
