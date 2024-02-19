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
Want properties specific to each SimHub car
- a C# list of games
	- each game a name, a car with default properties and list of cars
		- each car a carID and list of properties
			- each property a name and value

My understanding of C# is that this could be a jagged array,  
but [List<>](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1) better supports adding and deleting elements.

## New to me
- C# `List<>` and particularly with non-trivial objects.
	- [Here are some snippits](https://www.tutorialsteacher.com/csharp/csharp-list) with `List<Student>`
	- [stackoverflow list search by LINQ](https://stackoverflow.com/questions/1175645/find-an-item-in-a-list-by-linq)
	- [M$ Learn List<T>.FindIndex Method](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.findindex):&nbsp; `int index = myList.FindIndex(a => a.Prop == oProp);`  
		[*index of item in a list*](https://stackoverflow.com/questions/17995706/how-can-i-get-the-index-of-an-item-in-a-list-in-a-single-step):  
		```
		var pair = myList.Select((Value, Index) => new { Value, Index }).Single(p => p.Value.Prop == oProp);
		Console.WriteLine("Index:{0}; Value: {1}", pair.Index, pair.Value);
		```
- C# JSON
	- [pretty-print JSON from C#](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions.writeindented)  AKA
		[serialize](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to)  
		```
			using System.Text.Json;

			var jsonString = JsonSerializer.Serialize(yourObj, new JsonSerializerOptions { WriteIndented = true });
		```
	- Eventually, [Read and Parse a JSON File in C#](https://code-maze.com/csharp-read-and-process-json-file/) AKA
	 [deserialize](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/deserialization)  
		- [**using .NET Core**](https://stackoverflow.com/questions/13297563/read-and-parse-a-json-file-in-c-sharp)
			[JsonSerializer Class](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer):  
			```
    		public class Item
    		{
        		public int millis;
        		public string stamp;
        		public DateTime datetime;
        		public string light;
        		public float temp;
        		public float vcc;
    		}

			Item item = JsonSerializer.Deserialize<Item>(File.ReadAllText("file.json"));

			// get the values dynamically without declaring Item class
			dynamic array = JsonConvert.DeserializeObject(json);
    		foreach(var item in array)
    		{
        		Console.WriteLine("{0} {1}", item.temp, item.vcc);
    		}

			```
