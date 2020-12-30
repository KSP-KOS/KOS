#### INDEX
* [Pull Requests](#pull-requests)
  * [Nobody merges their own PR](#nobody-merges-their-own-pr)
* [Setting Up Your Environment](#setting-up-your-environment)
  * [Assumptions](#assumptions)
  * [Your Repository](#setting-up-your-repository)
  * [Solution Dependencies](#setting-up-the-solution-dependencies)

Pull Requests
=============

* Please reference [Style Guide](STYLEGUIDE.md)
* All PRs ("Pull Requests") must be related to an open issue.  If there isn't already
  an issue that applies to your PR, please submit a new issue with the PR.
* If a PR changes the scripting API, it must include all related changes to the documentation.
  The documentation is edited by changing or adding to the `*.rst` files under `doc/source/`.
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
* The act of submitting a PR is presumed to be your permission to place your
  work under the license the KOS project is using (GPL3 for source code.
  Art asset files are allowed to use a different license if you feel it's more
  appropriate).
* The act of submitting a PR is presumed to be your permission for someone to
  merge it into develop *right now* if they wish to.  If you have a PR that you
  don't want merged just yet, but are just getting it ready or showing it off
  for discussion, then you *must* do one of the following two things:
  1. (This only works if you are a priviledged team member with full permissions)
     Give it the label tag "Not Ready" in order to make it clear nobody should
     try to merge it yet.  Later you remove the label "Not Ready" when you do
     want it merged.
  2. If you are not a priviledged team member with permissions to edit labels,
     then just put the phrase "[Not Ready]" in the title of the PR to
     communicate the same thing, and edit the title to remove it later when
     it is ready.

## Nobody merges their own PR

NOTE THIS RULE IS SUSPENDED.

THIS RULE IS SUSPENDED BECAUSE THE DEV TEAM SHRUNK TO JUST ONE
PERSON, MAKING IT IMPOSSIBLE TO GET ANY PR MERGED IF THIS RULE
WAS STILL BEING FOLLOWED.  (If the dev team grows again to more
people, this rule may be re-instated, as it is very good practice,
WHEN there's actually more than one person on the team who has the
time.)

(Rules for priviledged members of the team who have permission to
write directly to the main repository.)

1. (SUSPENDED - SEE ABOVE) As a general policy, even experienced developers on the team should not
   merge their own pull requests into the upstream `develop`.  Instead they
   should get another developer to merge it for them.
2. (SUSPENDED - SEE ABOVE) As such, even if you have permission to do so, never directly push a
   change to `upstream develop` except in cases where you are doing so as
   part of the process of merging somebody *else's* pull request other than
   your own, or during some of the final steps of the release checklist
   that require it.
3. (SUSPENDED - SEE ABOVE) When merging somebody else's pull request, do not "rubber stamp" it.  Actually
   try to read and understand what it does and how, and raise questions with
   the author using the github "line note" system.

The general principle is that doing this has two beneficial effects:  (1) Redundancy.
In order for a mistake to slip past, two different brains have to fail to notice it,
rather than just one, and (2) In the event one developer falls off the face of
the earth, never to be heard from again, everything they've done has had at least
one other developer on the team with a bit of familiarity with it.


Setting Up Your Environment
===========================

## Assumptions

* `$KSP` is the full path to your Kerbal Space Program installation directory.
  (i.e. `"C:\Program Files (x86)\Steam\SteamApps\common\Kerbal Space Program"`)
* `$KOS` is the full path to your KOS git repository.
* Other than examples of full paths for Windows, all paths are shown in this
  guide using the unix directory seperator of `/`.  Please change all instances
  of `/` and `\` to match the style of your development environment.
* You already have a C# IDE and are familiar with how to use it to build a
  .sln solution.

## Setting Up Your repository

1. Use the github web interface to create a fork of KSP-KOS/KOS

2. Your fork should have a web address like `https://github.com/[username]/KOS`
  and a git address like `https://github.com/[username]/KOS.git` (If there is an
  name conflict, your fork may have a number appended to ensure that it is
  unique, such as `KOS-1.git`).

3. If you use Github's desktop client, you can click the "Save to desktop"
  button. Otherwise, using the git command line to issue this command
  works too:
  ```
  git clone https://github.com/[username]/KOS.git
  ```

4. Set up KSP-KOS/KOS as the remote called `upstream`:
  ```
  git remote add upstream https://github.com/KSP-KOS/KOS.git
  ```

5. Create a new branch based on `develop` in order to contain
   the edits corresponding to each pull request you plan to make.
   Never locally edit the `develop` branch itself.  You can make
   a new branch from develop like so:
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

  (If you have made any local conflicting changes to the `develop` branch itself,
  then you're doing it wrong.  The use of the `--ff-only` option will detect this
  for you and refuse to work.  As per point (5) above, all your changes should
  be happening in a different branch other than `develop`.)

7. If the upstream develop branch changes while you are working on your branch,
  so you need to update your branch, you may (and probably should) merge the
  upstream `develop` branch directly into it or rebase:
  ```
  git checkout whatever-the-branch-i-am-working-on-is-called
  git pull upstream develop
  ```

  or:
  ```
  git checkout whatever-the-branch-i-am-working-on-is-called
  git rebase upstream develop
  ```

  See the notes above regarding `rebase`

  This makes life easier for the other developer who has to merge your pull
  request into the upstream `develop`.  You are better suited to understanding
  how to deal with the potential merge conflicts that could come up between
  your branch and the upstream `develop` changes than somebody else is.

8. You may push your branch with edits to your `origin` on github, and submit a
  pull request to KSP-KOS/KOS `develop` for review to be included.

## Setting Up The Solution Dependencies

1. Copy the folder `$KOS/Resources/GameData/kOS` to `$KSP/GameData/`

2. Get the Unity assemblies into your project. There are two options:

	1. Copy these DLLs from `$KSP/KSP_x64_Data/Managed `into `$KOS/Resources` (NB: see note below about assemblies/DLLs):

		* `Assembly-CSharp`
		* `Assembly-CSharp-firstpass`
		* `UnityEngine*.dll`

                    * ^ (Please notice the wildcard asterisk in the above point.
                      As of KSP 1.8, The version of Unity being used has split much
                      of its functionality into many different dlls when it used to
                      be combined into just a few.  To be sure you have all of the
                      pieces of Unity that KSP uses, copy all the filenames of the
                      form UnityEngine[whatever].dll from the game to your
                      Resources folder.  Some may be unnecessary, but it's
                      better to have unneeded ones than to be missing one you
                      ended up needing.)

        2. Add these files as "References" to your kOS project, if they aren't already.
           Again, these are needed because Unity split itself into separate DLLS
           in the newer version that KSP 1.8 is using.
           (Hypothetically they should already be there in the .proj file, but check
           to make sure):

               * Resources/Assembly-CSharp
               * Resources/Assembly-CSharp-firstpas
               * Resources/UnityEngine
               * Resources/UnityEngine.AnimationModule
               * Resources/UnityEngine.AudioModule
               * Resources/UnityEngine.CoreModule
               * Resources/UnityEngine.ImageConversionModule
               * Resources/UnityEngine.IMGUIModule
               * Resources/UnityEngine.InputLegacyModule
               * Resources/UnityEngine.TextRenderingModule
               * Resources/UnityEngine.UI
               * Resources/UnityEngine.UnityWebRequestAudioModule
               * Resources/UnityEngine.UnityWebRequestModule
               * Resources/UnityEngine.UnityWebRequestWWWModule

           If you get any compile errors that look like the following line
           when you do a full rebuild, it might mean there is one of these
           DLLs missing from the Resources:

               * "The type or namespace name [name here] could not be found (are you missing a using directive or an assembly reference?)"
           If you get one of these errors and don't know which DLL to use to fix it,
           here is how you find out:
               * Go to this page: https://docs.unity3d.com/ScriptReference/
               * Find the "Search Scripting" box in the top of the page.
               * Type the [name here] from the error message in that search box and hit enter
               * Click on the resulting link to go to that class's documentation page.
               * The Unity documentation for that class will start with some faint grey text at the top of the page that says, "Implemented in:", which tells you which DLL you need to reference to get code using that class to compile properly.

	3. If you do not have a copy of KSP locally, you may
	  download dummy assemblies at https://github.com/KSP-KOS/KSP_LIB

3. Make sure you are targeting this version of .Net:  ".Net 4.0 Framework".
It is *sometimes* possible to make DLLs work for newer versions of .Net,
but only on some user's computers.  To ensure that you remain compatible
with all the target platforms (which are using an older version of Mono),
you must limit yourself to .net 4.0 only.  If you use a newer feature that
did not exist yet in .Net 4.0, it may result in a DLL that works fine on
your computer, but not all the KSP target platforms.

4. Tell "NuGet" to pull the packages defined in the kOS solution.  (In Visual
Studio, this is accomplished by right clicking the kOS Solution and picking
"Restore NuGet Packages".  In other dev environments it may be a different
place on the menus.)

5. If you want building the solution to update the dlls in your KSP
   directory, create a symbolic link called `KSPdirlink` from the root
   of this repository to your KSP installation directory.

**Note**: the list of assemblies above is not necessarily exactly what you will need. The `UnityEngine.ImageConversionModule` assembly for example only exists on the macOS port of KSP.

You can build the list of assemblies yourself by building the kOS solution and looking for the "forwarded to assembly" errors. These errors should look something like this:

> …/KOS/src/kOS/Binding/FlightStats.cs(143,143): Error CS1069: The type name 'Rigidbody' could not be found in the namespace 'UnityEngine'. This type has been forwarded to assembly 'UnityEngine.PhysicsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly. (CS1069) (kOS),

In this case the assembly you are looking for is `UnityEngine.PhysicsModule` which should be provided in the `UnityEngine.PhysicsModule.dll` DLL file.
