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
- C# [Declaring a DataGrid in XAML](https://blog.udemy.com/wpf-datagrid/)
	- 4 rows:
		- property name
		- default value
		- previous value
		- current value
	- first column of row labels, as above
    - programatically add a column for each property configured
	- highlight current value of only selected property
	- more references:
		- [*wpf-tutorial*:&nbsp DataGrid columns](https://wpf-tutorial.com/datagrid-control/custom-columns/) **binds cells to list of class members**
		- [DataGrid](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/datagrid?view=netframeworkdesktop-4.8)
		- [DataGrid in WPF](https://www.c-sharpcorner.com/uploadfile/mahesh/datagrid-in-wpf/)
		- [DataGrid Class](https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.datagrid?view=windowsdesktop-8.0)
		- [DataGrid Examples](https://www.dotnetperls.com/datagrid-wpf)
		- [Sizing Options in the DataGrid Control](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/sizing-options-in-the-datagrid-control?view=netframeworkdesktop-4.8)
		- [highlight Item in Datagrid](https://stackoverflow.com/questions/15467553/proper-datagrid-search-from-textbox-in-wpf-using-mvvm)
		- [Change DataGrid cell colour](https://stackoverflow.com/questions/5549617/change-datagrid-cell-colour-based-on-values)
		- [*stackoverflow*:&nbsp; highlight WPF DataGrid cell programmatically](https://stackoverflow.com/questions/3836191/how-to-select-a-row-or-a-cell-in-wpf-datagrid-programmatically)
		- [*blog.magnusmontin*:&nbsp; programmatically select and focus a WPF DataGrid cell](https://blog.magnusmontin.net/2013/11/08/how-to-programmatically-select-and-focus-a-row-or-cell-in-a-datagrid-in-wpf/)
		- [*learn.microsoft*:&nbsp; programmatically select AND focus a WPF Datagrid cell](https://learn.microsoft.com/en-us/archive/msdn-technet-forums/89df8b8f-29b8-4915-b2b6-e153e05f9ca9)
		- [programmatically add DataGrid rows](https://stackoverflow.com/questions/10063770/how-to-add-a-new-row-to-datagridview-programmatically)
		- [DataGridViewRow Class](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datagridviewrow?view=windowsdesktop-8.0)
		- [*stackoverflow*:&nbsp; change WPF DataGrid cell value programmatically](https://stackoverflow.com/questions/12164079/change-datagrid-cell-value-programmatically-in-wpf)
		- [access WPF DataGrid row and cell *example*](https://techiethings.blogspot.com/2010/05/get-wpf-datagrid-row-and-cell.html)
		- [*CodeProject*:&nbsp; MVVM and the WPF DataGrid](https://www.codeproject.com/articles/42548/mvvm-and-the-wpf-datagrid)
	- first steps in Visual Studio:
		- click `Control.xaml`
		- select View->Designer
		- drag in `DataGrid` from `Common WPF Controls`
			- fiddle with margins for Grid and DataGrid to make space for Label
			- [add DataGrid column Headers](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-add-row-details-to-a-datagrid-control?view=netframeworkdesktop-4.8)
		- drag in buttons for previous, next, +, -, etc  
			![](Documentation/DataGrid.png)  
- C# WPF XY plot
	- using [OxyPlot](https://github.com/oxyplot/oxyplot)?
		- [website](https://oxyplot.github.io/) &nbsp; [documentation](https://oxyplot.readthedocs.io/en/latest/)
		- [Bart De Meyer - Blog](https://blog.bartdemeyer.be/2013/03/creating-graphs-in-wpf-using-oxyplot/)
		- [CodeProject](https://www.codeproject.com/Articles/1164395/Wpf-application-with-real-time-data-in-OxyPlot-cha)
		- [stackoverflow example](https://stackoverflow.com/questions/44697701/create-an-oxyplot-in-wpf)
	- using [GLGraph](https://github.com/varon/GLGraph)?
