# This is a precursor to an automated build system

### Pre-Build
- [ ] Update AssemblyInfo for kOS project
- [ ] Update AssemblyInfo for kOS.Safe project
- [ ] Update AssemblyInfo for kOS.Safe.Test project
- [ ] Update `Resources\GameData\kOS\kOS.version`
- [ ] Update `doc\source\conf.py`
- [ ] Update `CHANGELOG.MD`
- [ ] Update `doc\source\changes.rst`

### Build
- [ ] Build kOS solution in release mode
- [ ] Run all unit tests
- [ ] Ensure that all required resources are in place (module manager)
- [ ] Create zip file with a root starting in the `\Resources\` directory
- [ ] The zip file should have the GameData folder in the root
- [ ] Name the zip file with the following pattern `kOS-v<major>.<minor>.<patch>.zip` (eg kOS-v0.14.2.zip )
- [ ] Build the documentation in `\docs\`
- [ ] Push the contents of `docs\gh-pages` to the gh-pages branch of KSP-KOS/KOS and verify correct rendering

### Post-Build
- [ ] Update master branch from develop branch.
- [ ] Build Github release with changelog and title
- [ ] Copy Github release to [Curseforge](http://kerbal.curseforge.com/projects/kos-scriptable-autopilot-system?gameCategorySlug=ksp-mods&projectID=220265)
- [ ] Copy Github release to [Spacedock](http://spacedock.info/mod/60/kOS:%20Scriptable%20Autopilot%20System)
- [ ] Update [Forum thread](https://forum.kerbalspaceprogram.com/index.php?/topic/165628-13-kos-v1130-kos-scriptable-autopilot-system/) with new change log, release date and version
- [ ] Post update in the forum thread
- [ ] Post update on [reddit board](http://www.reddit.com/r/kos)
- [ ] Push the gh-pages branch to KSP-KOS/KOS_DOC
