# This is a precursor to an automated build system

Note, some of these steps are different if this is a backport to an
older version of KSP instead of the most recent version.  Please
note where it says "(*backport*)" below if you are doing a backport.

### Pre-Build
- [ ] Assign the new kOS version number to `AssemblyVersion` and `AssemblyFileVersion` in these C# files:
  - [ ] In `src/kOS/Properties/AssemblyInfo.cs`
  - [ ] In `src/kOS.Safe/Properties/AssemblyInfo.cs`
  - [ ] In `src/kOS.Safe.Test/Properties/AssemblyInfo.cs`
- [ ] Update `Resources\GameData\kOS\kOS.version`
- [ ] Update `CHANGELOG.MD``
- [ ] If this is a normal release for the most recent KSP version supported:
  - [ ] Update `doc\source\conf.py`
  - [ ] Update `doc\source\changes.rst`
  - [ ] Above changes merged into `develop` branch on repo.
- [ ] Else if this for a *backport* to an older KSP release than the most recent version supported:
  - [ ] Above changes merged into `backport-for-KSPversion.number.here` branch on repo instead of into `develop` branch.

### Build
- [ ] Configure build for "Release" mode not Debug
- [ ] Fully rebuild kOS solution (clean, then build) in release mode
- [ ] Run all unit tests
- [ ] Ensure that all required resources are in place (module manager) (*backport* : Note if this is a backport you may need to use an older modulemanager DLL here.)
- [ ] Create a ZIP file that has only the `GameData` folder of the `\Resources\` directory in it.  Make sure the ZIP's folder structure starts with `GameData`.
  - In other words, filenames like this: `GameData\kOS\kOS.version`, NOT this `\kOS\kOS.version`.)
- [ ] Name the zip file with the following pattern `kOS-v<major>.<minor>.<patch>.<build>.zip` (eg kOS-v1.1.3.0.zip )
- [ ] Build the documentation (unless this is a *backport*) with:
  - (if Linux or Mac: `cd docs ; make clean ; make html`)
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
- [ ] NOW that all the ZIP releases are out there on the various sites, NOW go back and merge any netkan_issue_NNNN PRs into develop and into master:
  - Any PR branches named like "netkan_issue_NNNN" are deliberately delayed until after the release ZIP file was made public in the above steps.
  - Now that the ZIP is public, now you can merge those PR's in to both develop and master.
  - Why? See the file called kOS.version.README_SUPER_IMPORTANT.md.
- [ ] Update [Forum thread](https://forum.kerbalspaceprogram.com/index.php?/topic/165628-13-kos-v1130-kos-scriptable-autopilot-system/) with new change log, release date and version
- [ ] Post update in the forum thread
- [ ] Post update on [reddit board](http://www.reddit.com/r/kos)
- [ ] Push the gh-pages branch to KSP-KOS/KOS_DOC (unless this is a *backport*). Note, if the previous push of gh-pages to KOS_DOC was done in a sloppy fashion, the attempt to do this push may result in a ridiculously large number of merge conflicts that are too much to handle.  It may be necessecary to force this push with ``git push -f``.  (The merge conflicts can happen because Sphinx re-numbers the HTML ID tags across all the HTML elements in all the files when you insert one thing.  Thus git thinks you've changed *everything* on each new commit of gh-pages.)

### CKAN FIX (Post-Post-Build)

- [ ] If this is a *backport* it will be important to check up on CKAN's database over the next day to ensure you didn't confuse it.  If all the version numbers were entered correctly in kOS.version in the branch from which you made the ZIP, it should be okay, but if not, there is potential to really mess it up.  Double check that the CKAN database is giving correct information.  (That is, that the backport is NOT being offered on new KSP installations, and is only being offered on older ones.)
