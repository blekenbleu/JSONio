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
		Random random;

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

			gamma = J.simprops.FindIndex(i => i.Name == "gamma");					// ProxyS() applies it to wslip
			SlipGain = J.simprops.FindIndex(i => i.Name == "SlipGain");				// ProxyS() applies it to wslip
			EffectStrength = J.simprops.FindIndex(i => i.Name == "EffectStrength");	// SlipGrip amplitude
			threshold = J.simprops.FindIndex(i => i.Name == "threshold");			// LoadedSlipGrip() applies it
//			Gscale = J.simprops.FindIndex(i => i.Name == "Gscale");
/*
			J.AttachDelegate("S.EffectStrength", () => Current(EffectStrength));
			J.AttachDelegate("S.gamma", () => Current(gamma));
			J.AttachDelegate("S.SlipGain", () => Current(SlipGain));
			J.AttachDelegate("Surge", () => Prop("AccelerationSurge"));
			J.AttachDelegate("Sway", () => Prop("AccelerationSway"));
			J.AttachDelegate("Haccel", () => Haccel(Prop("AccelerationSurge"), Prop("AccelerationSway")));
 */
			J.AttachDelegate("Grip.FrontLeft", () => Grip(Prop("AccelerationHeave"),
												-Prop("AccelerationSway"),
												Prop("AccelerationSurge")));
			J.AttachDelegate("Grip.FrontRight", () => Grip(Prop("AccelerationHeave"),
												Prop("AccelerationSway"),
												Prop("AccelerationSurge")));
			J.AttachDelegate("Grip.RearLeft", () => Grip(Prop("AccelerationHeave"),
												-Prop("AccelerationSway"),
												-Prop("AccelerationSurge")));
			J.AttachDelegate("Grip.RearRight", () => Grip(Prop("AccelerationHeave"),
												Prop("AccelerationSway"),
												-Prop("AccelerationSurge")));

			J.AttachDelegate("GameName", () => pluginManager.GameName);

			if ("AssettoCorsa" == pluginManager.GameName || "AssettoCorsaCompetizione" == pluginManager.GameName)
			{
				J.AttachDelegate("ACslipGrip.FrontLeft", () =>	ACslipGrip(ProxyS("wSlip.FrontLeft"),	Raw("Physics.WheelLoad01")));
				J.AttachDelegate("ACslipGrip.FrontRight", () =>	ACslipGrip(ProxyS("wSlip.FrontRight"), 	Raw("Physics.WheelLoad02")));
				J.AttachDelegate("ACslipGrip.RearLeft", () =>	ACslipGrip(ProxyS("wSlip.RearLeft"), 	Raw("Physics.WheelLoad03")));
				J.AttachDelegate("ACslipGrip.RearRight", () =>	ACslipGrip(ProxyS("wSlip.RearRight"),	Raw("Physics.WheelLoad04")));
			} else {
				J.AttachDelegate("SHslipGrip.FrontLeft", () =>	SHslipGrip(ProxyS("wSlip.FrontLeft"),
																		Grip(Prop("AccelerationHeave"),
																			Prop("AccelerationSway"),
																			Prop("AccelerationSurge"))));
				J.AttachDelegate("SHslipGrip.FrontRight", () =>	SHslipGrip(ProxyS("wSlip.FrontRight"),
																		Grip(Prop("AccelerationHeave"),
																			-Prop("AccelerationSway"),
																			Prop("AccelerationSurge"))));
				J.AttachDelegate("SHslipGrip.RearLeft", () =>	SHslipGrip(ProxyS("wSlip.RearLeft"),
																		Grip(Prop("AccelerationHeave"),
																			Prop("AccelerationSway"),
																			-Prop("AccelerationSurge"))));
				J.AttachDelegate("SHslipGrip.RearRight", () =>	SHslipGrip(ProxyS("wSlip.RearRight"),
																		Grip(Prop("AccelerationHeave"),
																 			-Prop("AccelerationSway"),
																 			-Prop("AccelerationSurge"))));
			}	// pluginManager.GameName
		}

		private double RSS(double x, double y)
		{
			return Math.Pow(x*x + y*y, 0.5);
		}

		private double RSS1(double x, double y)
		{
			return Math.Max(0.11, Math.Pow(x*x + y*y, 0.5));
		}

		public double Haccel(double surge, double sway)
		{
			return 1 + 0.99 * RSS(surge, sway);
		}

		public double Grip(double heave, double sway, double surge)
		{
			double load = 25 + heave + sway + surge * 0.67;
			if (25 > load)
				load = 25;
			return 1 + 0.99 * 25 * Haccel(surge, sway) / load;
		}

		private double ProxyS(string wslip)
		{
			return 100 * Math.Pow(0.01 * Shaken(wslip) * Current(SlipGain), 1 / Current(gamma));
		}

		public double SHslipGrip(double proxyS, double grip)
		{
			return 100 * Current(EffectStrength) * proxyS / grip; 
		}

		private double Raw(string physics)
		{
			var o = pluginManager.GetPropertyValue("GameRawData." + physics);

			return (null == o) ? 0 : Convert.ToDouble(o);
		}

		public double ACslipGrip(double proxyS, double Whload)
		{
			double sg = 0.000005 * Current(EffectStrength) * proxyS * Whload / RSS1(Raw("Physics.AccG01"), Raw("Physics.AccG02"));
			return 100 * Math.Pow(sg, 0.5);
		}

		private double SlipGate(string sg)
		{
			return Math.Min(1, 0.2 * Shaken(sg));
		}

		private double AbsAcc(double a)
		{
			return 100 * Math.Pow(Math.Abs(.05 * a), 0.3);
		}

		public double LoadedSlipGrip(string grip, double surge, double sway)
		{
			double d = (0 > sway) ? 4 : -4;
			double L = 25  + AbsAcc(sway) / d;	// 25 +/-25% left-right distribution
			d = (0 < surge) ? 100 : -100;
			L *= (1 + AbsAcc(surge) / d); // fore-aft distribution
			return Math.Max(0, SlipGate(grip) * L - Current(threshold));
		}

		// forced frequency
		double FF(double sg, int fmin, int fmax)	// which: ACslipGrip() or SHslipGrip()
		{
			double low = Current(fmin), high = Current(fmax);
			double range = 0.01 * (high - low) * 3 / (1 + 3);	// scale based on 3 and range
			sg = low + range * (100 - sg);
			return sg + random.NextDouble() * sg / 3;
		}
    }
}
