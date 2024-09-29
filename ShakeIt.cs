using SimHub.Plugins;
using System;

namespace blekenbleu.jsonio
{
    /// <summary>
    /// based on LoadedSlipGripJSONio.siprofile
    /// </summary>
    public class ShakeIt
    {
		private JSONio J;
		private PluginManager P;
		private int EffectStrength, gamma, SlipGain;

		private double Shaken(string pname)
		{
			if (null == P || null == P.GetPropertyValue("ShakeITBSV3Plugin.Export." + pname))
				return 0;
            return Convert.ToDouble(P.GetPropertyValue("ShakeITBSV3Plugin.Export." + pname));
		}

		private double Prop(string pname)
		{
			if (null == P || null == P.GetPropertyValue("DataCorePlugin.GameData." + pname))
				return 0;
            return Convert.ToDouble(P.GetPropertyValue("DataCorePlugin.GameData." + pname));
		}

		float Current (int prop)
		{
			return float.Parse(J.simprops[prop].Current);
		}

		internal void Init(JSONio j, PluginManager p)
		{
			J = j;
			P = p;
			EffectStrength = J.simprops.FindIndex(i => i.Name == "EffectStrength");
			gamma = J.simprops.FindIndex(i => i.Name == "gamma");
			SlipGain = J.simprops.FindIndex(i => i.Name == "SlipGain");
			J.AttachDelegate("S.EffectStrength", () => Current(EffectStrength));
			J.AttachDelegate("S.gamma", () => Current(gamma));
			J.AttachDelegate("S.SlipGain", () => Current(SlipGain));
			J.AttachDelegate("Grip", () => Grip(Prop("AccelerationHeave"),
												Prop("AccelerationSway"),
												Prop("AccelerationSurge")));
			J.AttachDelegate("SHslipGrip.FrontLeft", () => SHslipGrip(ProxyS(Shaken("OutputSlip.FrontLeft")),
															Grip(Prop("AccelerationHeave"),
																 Prop("AccelerationSway"),
																 Prop("AccelerationSurge"))));
		}

		private double RMS(double x, double y)
		{
			return Math.Pow(x*x + y*y, 0.5);
		}

		public double Haccel(double surge, double sway)
		{
			return RMS(surge, sway);
		}

		public double Grip(double heave, double sway, double surge)
		{
			double load = heave + sway + surge * 0.67;
			return 25 * RMS(surge, sway) / (25 > load ? 25 : load);
		}

		private double ProxyS(double slip)
		{
			return 100 * Math.Pow(0.01 * slip * float.Parse(J.simprops[SlipGain].Current),
								 1 / float.Parse(J.simprops[gamma].Current));
		}

		public double SHslipGrip(double proxyS, double grip)
		{
			if (0 == grip)
				return 0;
			return 100 * float.Parse(J.simprops[EffectStrength].Current) * proxyS / grip; 
		}

		public double ACslipGrip(double proxyS, double Whload)
		{
			return 0.0005 * float.Parse(J.simprops[EffectStrength].Current) * proxyS * Whload
				/ RMS((double)P.GetPropertyValue("GameRawData.Physics.AccG01"), (double)P.GetPropertyValue("GameRawData.Physics.AccG02"));
		}

		public double LoadedSlipGrip()
		{
			return 0;
		}
    }
}
