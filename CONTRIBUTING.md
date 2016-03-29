####INDEX
* [Pull Requests](#pull-requests)
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
  work under the license the KOS project is using (GPL3).
* The act of submitting a PR is presumed to be your permission for someone to
  merge it into develop *right now* if they wish to.  If you have a PR that you
  don't want merged just yet, but are just getting it ready or showing it off
  for discussion, then you *must* give it the label tag "Not Ready" in order
  to make it clear nobody should try to merge it yet.  Later you remove the
  label "Not Ready" when you do want it merged.

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

####Nobody merges their own PR's.

(Rules for priviledged members of the team who have permission to
write directly to the main repository.)

1. As a general policy, even experienced developers on the team should not
   merge their own pull requests into the upstream `develop`.  Instead they
   should get another developer to merge it for them.
2. As such, even if you have permission to do so, never directly push a
   change to `upstream develop` except in cases where you are doing so as
   part of the process of merging somebody *else's* pull request other than
   your own, or during some of the final steps of the release checklist
   that require it.
3. When merging somebody else's pull request, do not "rubber stamp" it.  Actually
   try to read and understand what it does and how, and raise questions with
   the author using the github "line note" system.

The general principle is that doing this has two beneficial effects:  (1) Redundancy.
In order for a mistake to slip past, two different brains have to fail to notice it,
rather than just one, and (2) In the event one developer falls off the face of
the earth, never to be heard from again, everything they've done has had at least
one other developer on the team with a bit of familiarity with it.

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
