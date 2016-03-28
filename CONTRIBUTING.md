####INDEX
* [Pull Requests](#pull-requests)
* [Setting Up Your Environment](#setting-up-your-environment)
  * [Assumptions](#assumptions)
  * [Your Repository](#setting-up-your-repository)
  * [Solution Dependencies](#setting-up-the-solution-dependencies)

Pull Requests
=============

* Please reference [Style Guide](STYLEGUIDE.md)
* All PRs must be related to an open issue.  If there isn't already an issue
  that applies to your PR, please submit a new issue with the PR.
* If a PR changes the scripting API, it must include all related changes to the documentation.
* Try to keep types that do not rely directly on KSP types in the `kOS.Safe` project
* Try to include unit tests in `kOS.Safe.Test` for additions to `kOS.Safe`
* Try to include kerboscript test scripts for new language features in `kerboscript_tests`
* Do not commit changes to the `.sln` or `.csproj` other than to add new source
  files or new *required* references.
* Do not commit files to the `Resources` folder (or its subfolders) that are not
  *required* to be included in the redistributable zip file.
* When updating your pull requests to the current develop branch, both `rebase`
  and `merge` are acceptable.  If your pull request is knowingly being used as
  the base for another pull request or another user, please avoid `rebase`.

Setting Up Your Environment
===========================
####Assumptions
* `$KSP` is the full path to your Kerbal Space Program installation directory.
  (i.e. `"C:\Program Files (x86)\Steam\SteamApps\common\Kerbal Space Program"`)
* `$KOS` is the full path to your KOS git repository.
* Other than examples of full paths for Windows, all paths are shown in this
  guide using the unix directory seperator of `/`.  Please change all instances
  of `/` and `\` to match the style of your development environment.
* You already have a C# IDE and are familiar with how to use it to build a
  .sln solution.

####Setting Up Your repository
1. Use the github web interface to create a fork of KSP-KOS/KOS

2. Your fork should have a web address like `https://github.com/[username]/KOS`
  and a git address like `https://github.com/[username]/KOS.git` (If there is an
  name conflict, your fork may have a number appended to ensure that it is
  unique, such as `KOS-1.git`).

3. If you use Github's desktop client, you can click the "Save to desktop"
  button, otherwise using the git command line:
  ```
  git clone https://github.com/[username]/KOS.git
  ```

4. Set up KSP-KOS/KOS as the remote `upstream`:
  ```
  git remote add upstream https://github.com/KSP-KOS/KOS.git
  ```

5. Create a new branch based on `develop` whenever you make edits.
  ```
  git branch -m develop [new-branch-name]
  ```

6. Reserve your local `develop` branch for tracking the upstream (KSP-KOS/KOS)
  `develop` branch.  You should update the local `develop` branch whenever there
  is a change to the upstream branch:
  ```
  git pull --ff-only upstream develop
  ```

  If you have not made any local conflicting changes to the `develop` branch
  itself, the "fast forward" pull will update your local branch without adding
  a merge commit.

7. If you need to update a local branch you are working on, you may "pull" the
  upstream branch or rebase:
  ```
  git pull upstream develop
  ```

  or:
  ```
  git rebase upstream develop
  ```

  See the notes above regarding `rebase`
8. You may push your branch with edits to your `origin` on github, and submit a
  pull request to KSP-KOS/KOS `develop` for review to be included.

####Setting Up The Solution Dependencies
1. Download the latest KSPAPIExtensions.dll from
   https://github.com/Swamp-Ig/KSPAPIExtensions/releases, and copy
   it to `$KSP/GameData/kOS/Plugins` and `$KOS/Resources/GameData/kOS/Plugins`.

2. Copy the folder `$KOS/Resources/GameData/kOS` to `$KSP/GameData/`

3. Copy `Unity-Engine.dll` and `Assembly-Csharp.dll` from `$KSP/KSP_Data/Managed`
  into `$KOS/Resources`.  If you do not have a copy of KSP locally, you may
  download dummy assemblies at https://github.com/KSP-KOS/KSP_LIB

4. If you want building the solution to update the dlls in your KSP
   directory, create a symbolic link called `KSPdirlink` from the root
   of this repository to your KSP installation directory.
