## JSONio `Init()` and `End()`  
- `End()` saves simValues in Settings, updates `slim.data` and writes as JSON if changed
	- there were instances where `slim.data.pList` had not been initialized,
		which suggests that it *might never been validated*
- `Init()`
	- initializes slim.data
	- restores Settings
	- calls `Populate` to initialize `simValues` and `Steps` from `.ini` with `.json`
	- calls `slim.Load()` to `Reconcile()` `simValues` with `.json` 
