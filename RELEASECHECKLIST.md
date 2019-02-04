# This is a precursor to an automated build system

Note, some of these steps are different if this is a backport to an
older version of KSP instead of the most recent version.  Please
note where it says "(*backport*)" below if you are doing a backport.

### Pre-Build
- [x] Update AssemblyInfo for kOS project
- [x] Update AssemblyInfo for kOS.Safe project
- [x] Update AssemblyInfo for kOS.Safe.Test project
- [x] Update `Resources\GameData\kOS\kOS.version`
- [x] Update `CHANGELOG.MD`
- [x] If this is a normal release for the most recent KSP version supported:
  - [x] Update `doc\source\conf.py`
  - [x] Update `doc\source\changes.rst`
  - [x] Above changes merged into `develop` branch on repo.
- [ ] Else if this for a *backport* to an older KSP release than the most recent version supported:
  - [ ] Above changes merged into `backport-for-KSPversion.number.here` branch on repo instead of into `develop` branch.

### Build
- [x] Build kOS solution in release mode
- [x] Run all unit tests
- [x] Ensure that all required resources are in place (module manager) (*backport* : Note if this is a backport you may need to use an older modulemanager DLL here.)
- [X] Create zip file with a root starting in the `\Resources\` directory
- [X] The zip file should have the GameData folder in the root
- [X] Name the zip file with the following pattern `kOS-v<major>.<minor>.<patch>.<build>.zip` (eg kOS-v1.1.3.0.zip )
- [ ] Build the documentation in `\docs\` (unless this is a *backport*)
- [ ] Push the contents of `docs\gh-pages` to the gh-pages branch of KSP-KOS/KOS and verify correct rendering (unless this is a *backport*)

### Post-Build
- [ ] If this is a *normal release* for the most recent KSP version suported:
  - [ ] Update master branch from develop branch.
- [ ] Else if this is a *backport* then do NOT update the master branch.  Keep it in the `backport-for-KSPversion.number.here` branch.
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

### CKAN FIX (Post-Post-Build)

- [ ] If this is a *backport* it will generally be necessary to manually make a PR on the CKAN repo to fix up its .ckan file for kOS.  (CKAN's automated scanning of the version file only sees what's in our Master branch, and can't see backports that we keep out of the master branch.)
