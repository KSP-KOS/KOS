# This is a precursor to an automated build system

### Pre-Build
* Update AssemblyInfo for kOS project
* Update AssemblyInfo for kOS.Safe project
* Update AssemblyInfo for kOS.Safe.Test project
* Update `Resources\GameData\kOS\kOS.version`
* Update `doc\source\conf.py`
* Update `CHANGELOG.MD`
* Update `doc\source\changes.rst`

### Build
* Build kOS solution in release mode
* Run all unit tests
* Ensure that all required resources are in place
  * `Resources/GameData/kOS/Plugins/KSPAPIExtensions.dll`
  * `Resources/GameData/ModuleManager.dll`
* Create zip file with a root starting in the `\Resources\` directory
* The zip file should have the GameData folder in the root
* Name the zip file with the following pattern `kOS-v<major>.<minor>.<patch>.zip` (eg kOS-v0.14.2.zip )
* Build the documentation in `\docs\`
* Push the contents of `docs\gh-pages` to the gh-pages branch of KSP-KOS/KOS and verify correct rendering
* Push the gh-pages branch to KSP-KOS/KOS_DOC

### Post-Build
* Update master branch from develop branch.
* Build Github release with changelog and title
* Copy Github release to Curseforge
* Copy Github release to Kerbalstuff
* Update Forum thread with new change log, release date and version http://forum.kerbalspaceprogram.com/threads/68089
* Post update in the forum thread
* Post update on reddit board http://www.reddit.com/r/kos
* Upload updated documentation
