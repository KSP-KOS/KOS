Pull Requests
=============

* All PRs must be related to an open issue
* If a PR changes the scripting API, it must include all related changes to the documentation. 

Setting Up Your Environment
===========================

1. Copy `Resources/GameData/kOS` to `$KSP/GameData/`, `where $KSP` is your
   Kerbal Space Program installation directory.

2. Download the latest KSPAPIExtensions.dll from
   https://github.com/Swamp-Ig/KSPAPIExtensions/releases, and copy 
   it to `$KSP/GameData/kOS/Plugins`.

3. Create a file at src/kOS/kOS.csproj.user conaining the following XML,
   replacing /path/to/KSP with your own KSP install directory.

        <?xml version="1.0" encoding="utf-8"?>
        <Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
          <PropertyGroup>
            <ReferencePath>
              /path/to/KSP/KSP_Data/Managed;
              /path/to/KSP/GameData/kOS/Plugins
            </ReferencePath>
          </PropertyGroup>
        </Project>
   
4. If you want building the solution to update the dlls in your KSP
   directory, create a symbolic link called `KSPdirlink` from the root
   of this repository to your KSP installation directory.
