# This is a precursor to an automated build system

### Pre-Build
* Update the kOS.Core.cs VersionInfo
* Update AssemblyInfo for kOS project
* Update AssemblyInfo for kOS.Safe project
* Update Resources\GameData\kOS\kOS.version
* Update doc\source\conf.py
* Update CHANGELOG.MD

### Build
* Build kOS solution in release mode
* Create zip file with a root starting in the \Resources\ directory
* Name the zip file with the following pattern kOS.v<major>.<minor>.<patch>.zip (eg kOS.v0.14.2.zip )
* Build the documentation in \docs\

### Post-Build
* Build Github release with changelog and title
* Copy Github release to Curseforge
* Copy Github release to Kerbalstuff
* Update Forum thread with new change log, release date and version http://forum.kerbalspaceprogram.com/threads/68089
* Post update in the forum thread
* Post update on reddit board http://www.reddit.com/r/kos
* Upload updated documentation

