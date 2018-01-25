# This is a precursor to an automated build system

Note, some of these steps are different if this is a backport to an
older version of KSP instead of the most recent version.  Please
note where it says "(*backport*)" below if you are doing a backport.

### Pre-Build
- [ ] Update AssemblyInfo for kOS project
- [ ] Update AssemblyInfo for kOS.Safe project
- [ ] Update AssemblyInfo for kOS.Safe.Test project
- [ ] Update `Resources\GameData\kOS\kOS.version`
- [ ] Update `CHANGELOG.MD`
- [ ] If this is a nomral release for the most recent KSP version supported:
  - [ ] Update `doc\source\conf.py`
  - [ ] Update `doc\source\changes.rst`
  - [ ] Above changes merged into `develop` branch on repo.
- [ ] Else if this for a *backport* to an older KSP release than the most recent version supported:
  - [ ] Above changes merged into `backport-for-KSPversion.number.here` branch on repo instead of into `develop` brach.

### Build
- [ ] Build kOS solution in release mode
- [ ] Run all unit tests
- [ ] Ensure that all required resources are in place (module manager) (*backport* : Note if this is a backport you may need to use an older modulemanager DLL here.)
- [ ] Create zip file with a root starting in the `\Resources\` directory
- [ ] The zip file should have the GameData folder in the root
- [ ] Name the zip file with the following pattern `kOS-v<major>.<minor>.<patch>.<build>.zip` (eg kOS-v1.1.3.0.zip )
- [ ] Build the documentation in `\docs\` (unless this is a *backport*)
- [ ] Push the contents of `docs\gh-pages` to the gh-pages branch of KSP-KOS/KOS and verify correct rendering (unless this is a *backport*)

### Post-Build
- [ ] Update master branch from develop branch.
- [ ] Build Github release with changelog and title, using the ZIP made above in Build step.
- [ ] CurseForge: Copy Github release text, and ZIP to [Curseforge](http://kerbal.curseforge.com/projects/kos-scriptable-autopilot-system?gameCategorySlug=ksp-mods&projectID=220265), as follows:
  - [ ] If this is a *normal release* for the most recent KSP version suported, mark the file as "release".
  - [ ] If this is a *backport*, mark the file as "alpha".  (So Curse won't present this as the default version).
- [ ] Copy Github release to [Spacedock](http://spacedock.info/mod/60/kOS:%20Scriptable%20Autopilot%20System)
  - [ ] If this is a *normal release* for the most recent KSP version supported, Make sure it is set as the default (using the "changelog" link on the Spacedock page).
  - [ ] If this is a *backport*, make sure it did NOT become the default, and set the default back to the most recent non-backport version (using the "changelog" link on Spacedock).
- [ ] Update [Forum thread](https://forum.kerbalspaceprogram.com/index.php?/topic/165628-13-kos-v1130-kos-scriptable-autopilot-system/) with new change log, release date and version
- [ ] Post update in the forum thread
- [ ] Post update on [reddit board](http://www.reddit.com/r/kos)
- [ ] Push the gh-pages branch to KSP-KOS/KOS_DOC (unless this is a *backport*)
