﻿using System.Collections.Generic;

namespace blekenbleu.jsonio
{
	public class Property	   // must be public for DataPluginSettings
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}

	/// <summary>
	/// Settings class, make sure it can be correctly serialized using JSON.net
	/// </summary>
	public class DataPluginSettings
	{
		public List<Property> properties = new List<Property>()
		{
			// each current Value stored as string of integer 10x actual value
			new Property() {}	// Current per-car, then per-game, then global
		};

		// global default property values
		public List<Property> gDefaults = new List<Property>() {};

		public string game;		// keep these properties if Gname matches
		public string carid;	// replace per-car properties from JSON, if available and CarId mismatches
		public int pcount = 0;	// per-car property count in properties
		public int gcount = 0;	// per-game property count in properties
	}
}
