Pull Requests
=============

* Please reference [Style Guide](STYLEGUIDE.md)
* All PRs ("Pull Requests") must be related to an open issue.  If there isn't already
  an issue that applies to your PR, please submit a new issue with the PR.
* If a PR changes the scripting API, it must include all related changes to the documentation.
  The documentation is edited by changing or adding to the `*.rst` files under `doc/source/`.
* If a change is user-facing or fixes a bug, the PR should include an update to CHANGELOG.md.
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

Setting Up Your Environment
===========================

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

KOS makes use of [KSPBuildTools](https://github.com/kspmoddingLibs/kspbuildTools/).
Please refer to that documentation for details.  If you restore the nuget package
references and set your KSP install root, everything should build without any
modifications to the project or sln files.

Since KOS contains several csproj files, you should set the path to your KSP install
in a solution-wide `KOS.props.user` file adjacent to `KOS.sln`.  An example file
is below:

```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <KSPRoot>C:\Program Files (x86)\Steam\steamapps\common\KSP Stripped</KSPRoot>
  </PropertyGroup>
</Project>
```