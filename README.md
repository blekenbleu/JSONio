# SimHub JSONio plugin  
 from SimHubPluginSdk
- instead of just copying that SimHubPluginSdk repository
    - Had Visual Studio create a new project, then quit
    - deleted everything in that project except `JSONio.sln` and `JSONio.csproj`
    - copied `Properties/` and source files from `SimHubPluginSdk/`
    - performed GVIM split diff on `JSONio.sln` and `JSONio.csproj`
		to preserve new `ProjectGuid`, etc
	- **forgot to** update namespace from `User.PluginSdk` to `JSONio` e.g. in `Properties/`...!

