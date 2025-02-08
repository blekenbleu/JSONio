## JSONio error messages
- JSONio is configured by SimHub properties from `NcalcScripts/JSONio.ini`  
	- The JSONio plugin does not parse that file
	- errors (e.g. duplicates) in that file are (silently) ignored  
		when SimHub *parses properties from it*.
- A key property is `DataCorePlugin.ExternalScript.JSONio.file`
	- per-game and per-car property values are stored in that JSON file.
	- possible messagebox:&nbsp; **Init(): DataCorePlugin.ExternalScript.JSONio.file found**
- **Missing or invalid** `whatever` **properties from NCalcScripts/JSONio.ini**
	- JSONio `Init()` expects `JSONio.settings`, `JSONio.setvals`, `JSONio.setsteps`,  // global  
		`JSONio.properties`, `JSONio.values` `JSONio.steps`, // per-car   
		`JSONio.gamesettings`, `JSONio.gamevals`, `JSONio.gamesteps`,  // per-game
		- all these properties should be comma-separated strings
- Other **Init():** `whatever` **not found**
	- `JSONio.settings`, `JSONio.properties`, `JSONio.gamesettings`  
		should be comma-separated strings of *other* application-specific property names.
		- "not found" messagebox occurs for any missing (or misspelled)  
- **Init():** `count` **per-car properties;**  `count` **values;**  `count` **steps**
	- if comma-separated per-car property name, value, or step `count` mismatches
	- similarly for **per-game properties;** and **gobal properties;**
		- smallest `count` gets used..
- **Slim.Load(`path`):  bad data**
	- JSON file `path` content does not match that expected by `JSONio` plugin,
		- perhaps from obsolete version
- **Slim.Load(`path`):  pList mismatched NCalcScripts/JSONio.ini**
	- Current JSON file `path` content does not match `NcalcScripts/JSONio.ini` content.
		- JSONio `Init()` will rewrite `path` to match
- **Slim.Load(`path`): `count` null carIDs**
	- one or more missing Car list property carIDs  
		- those car objects will be deleted deleted from `path` JSON.
