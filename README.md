# SimHub JSONio plugin  
 from SimHubPluginSdk
- instead of just copying that SimHubPluginSdk repository
    - Had Visual Studio create a new project, then quit
    - deleted everything in that project except `JSONio.sln` and `JSONio.csproj`
    - copied `Properties/` and source files from `SimHubPluginSdk/`
    - performed GVIM split diff on `JSONio.sln` and `JSONio.csproj`
		to preserve new `ProjectGuid`, etc
	- **forgot to** update namespace from `User.PluginSdk` to `JSONio` e.g. in `Properties/`...!
## What
Want properties specific to each SimHub car:  
![](Documentation/properties.png)
- a C# list of games
	- each game a name, game=specific default properties and list of cars
		- each car a carID and its list of properties
			- each property a name and value
- `this.AddAction("ChangeProperties",(a, b) =>` saves current properties, if changed,  
	then loads properties for the new Car.
	- creates new Car object from game default if no CarID match
	- *could* implement `this.AddEvent("CarChange");`  
		in `public void Init(PluginManager pluginManager)`,  
		- then `this.TriggerEvent("CarChange");`  
			in `public void DataUpdate(PluginManager pluginManager, ref GameData data)`
	- *but instead* let SimHub do it, by `JSONio.ini`:
		```
		[ExportEvent]
		name='CarChange'
		trigger=changed(20, [DataCorePlugin.GameData.CarId]) 
		```
		- *my experience*:&nbsp; SimHub ignored this **Source** when `JSONio.ini` was first loaded...  
![](Documentation/mapping.png)  

- in `Init()`, create a new Game object in games if none exists for current Game
	- set game from games

My understanding of C# is that `games` could be a jagged array,  
but jagged [List<>](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1) better supports
e.g. [adding and deleting elements](https://csharp-station.com/c-arrays-vs-lists/).

## New to me
- C# `List<>` and particularly with non-trivial objects.
	- [Here are some snippits](https://www.tutorialsteacher.com/csharp/csharp-list) with `List<Student>`
	- [stackoverflow list search by LINQ](https://stackoverflow.com/questions/1175645/find-an-item-in-a-list-by-linq)
	- [M$ Learn List<T>.FindIndex Method](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.findindex):&nbsp;
		[*index of item in a list*](https://stackoverflow.com/questions/17995706/how-can-i-get-the-index-of-an-item-in-a-list-in-a-single-step):  
		```
			int index = properties.FindIndex(a => a.Name == name);

            if (-1 == index)
                properties.Add ( new Property() { Name=name, Value=value });
            else if (replace && properties[index].Value != value)
                properties[index].Value = value;
		```
- C# JSON
	- Visual Studio had to add `System.Text.Json` package...  
	- [pretty-print JSON from C#](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions.writeindented)  AKA
		[serialize](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to)  
		```
			using System.Text.Json;

			if (changed)
                File.WriteAllText(path, JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true }));
		```
	- Eventually, [Read and Parse a JSON File in C#](https://code-maze.com/csharp-read-and-process-json-file/) AKA
	 [deserialize](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/deserialization)  
		```
			if (File.Exists(path))  
            {  
                games = JsonSerializer.Deserialize<Games>(File.ReadAllText(path));  
            } else changed = true;  
		```
