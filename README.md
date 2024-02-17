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
	- each game a name and list of cars
		- each car a name, carID and list of properties
			- each property a name and value
	- one game named 'default', with a car named 'default' with default property list

My understanding of C# is that this could be a jagged array,  
but lists are better for adding and deleting elements.
