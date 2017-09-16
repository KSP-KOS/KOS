kOS Mod Changelog
=================

# v1.1.2.0 (for KS 1.3) Bug Swatting

### BREAKING CHANGES

(Can't think of any.)

### NEW FEATURES
* Terminal input using any Unicode character, not just ASCII.
  (Technically not a new feature, but a bug fix to a feature
  from the previous version, but since the bug made the feature
  never work *at all* in the past, it feels like a new feature).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2062)
* New StartTracking suffix for "unknown objects" (asteroids).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2077)

### BUG FIXES
* A large refactor of how the various flight control methods track
  which vessel they control.  This appears to have fixed a lot of
  bugs where kOS lost the ability to control the ship unless
  you reloaded the scene.  (After a docking, undocking, staging,
  vessel switch, or scene switch, this would sometimes happen,
  but not consistently enough to be easy to debug).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2100)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2063)
* Program aborts caused by external events such as poweroff,
  shutdown, or control-C no longer leave garbage behind in
  memory still hooked into parts of kOS.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2019)
* Documentation now more explicitly mentions how SAS and lock steering
  fight with each other.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2111)
* Documentation for GUIskin:add() was wrong.  Fixed.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2098)
* The waypoint() constructor used to fail on waypoints which
  were *not* part of a cluster yet were named as if they
  were part of a cluster anyway ("my waypoint Alpha",
  "my waypoint Beta", "my waypoint Gamma", etc).  This doesn't
  happen in stock, but does happen with several mods that use
  ContractConfigurator.  kOS will now deal with such waypoints.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2093)
* Documentation that claimed obsoleted TERMVELOCITY still
  exists has been removed or edited.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2067)
* Trying to examine the NoDelegate object no longer causes
  nullref error.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2082)
* Equality operator ( == ) when comparing a Path to a Path now
  fires off correctly.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2089)
* GUI's ONRADIOCHANGE callback hook now no longer depends
  on the existence of an ONTOGGLE hook to fire off.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2088)
* Compiler no longer creates incorrect opcodes for indexed
  collections used as arguments to a function call that's
  on the lefthand side of an assignment statement.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2079)
* Font resizing in scripts no longer causes the terminal to mangle
  its size and width/height character count
  [pull request](https://github.com/KSP-KOS/KOS/pull/2081)
* Signal delay progress bar (when using Remote Tech) will now resize
  properly when you have a nonstandard sized terminal window.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2076)
* Compile command now works properly when run from the interpreter.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2071)
* Vessel:isDead working properly now
  [pull request](https://github.com/KSP-KOS/KOS/pull/2070)
* Stretching the terminal to a large size no longer causes
  the rounded corner to obscure text in the window.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2060)
* Full unicode keyboard and file save support was getting
  mangled by wiping out the high byte leaving only the 8-bit
  ASCII part left.  Fixed.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2062)
* Toolbar Panel setting changes no longer require there to
  exist a kOS part loaded into the scene.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2058)

# v1.1.1 (for KSP 1.3) KSP 1.3 compatibility recompile.

No known intentional changes other than editing a few method calls
to the KSP API to make it work with KSP 1.3.

Also updated the included ModuleManager to version 2.8, which
is a necessity for compatibility with KSP 1.3.


# v1.1.0 (for KSP 1.2.2) Ewww, everything's GUI.

### BREAKING CHANGES
* Because of changes to make the terminal use a real font from your OS, we had
  to obsolete TERMINAL:CHARWIDTH.  You can only choose TERMINAL:CHARHEIGHT.
  Each font has its own hardcoded notion of how wide a letter will be at a
  given height, which you can't override.
* CONFIG:BRIGHTNESS was moved back to the global config section, and is no longer
  set on the "difficulty" options screen, because it's not supposed to be a
  per-saved-game setting, but a user-interface preference that spans all saved games.
* ATM:SEALEVELPRESSURE now gives the answer in different units than it used to.
  (It was in KiloPascals even though the documentation claimed it was in atmospheres.
  Now it's in atmospheres to agree with the documentation.)

### NEW FEATURES
* **GUI-making toolkit**. You are now able to make a GUI window that your kerboscript
  code can control, including buttons, sliders, toggles, checkboxes, etc.  It uses the
  KSP game's default skin (kind of big letters) but the skin can be customized by the
  script a bit to change things.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1878)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2006)
  documentation: search for "GUI" (http://ksp-kos.github.io/KOS_DOC/structures/gui.html).
* **Background compilation**.  Now the game continues its simulation normally and physical
  events keep happening, while kOS is taking a few seconds to compile a script.
  (Gets rid of that familiar frozen game effect when you first issue a ``RUN`` command.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1941)
* **Terminal Font**.  Now the kOS in-game terminal window uses a real font from your OS itself
  to render the text terminal.  (This allows the display of any Unicode character the font can
  render, and it allows nicer looking font size changes.)  Previously kOS painted images for
  letters from a hardcoded texture image file.
  [pull request 1](https://github.com/KSP-KOS/KOS/pull/1948)
  [pull request 2](https://github.com/KSP-KOS/KOS/pull/2008)
* **Allow any unicode**.  The kerboscript parser now allows identifiers and literal strings to
  contain letters outside the limited ASCII-only range it used to accept.  The in-game terminal now
  allows you to type any letter your keyboard can type.  (But it does not implement the ALT-numpad
  technique of entering characters.  You have to have a keyboard that types the character directly.
  However, the ALT-numpad technique will work through the telnet terminal, if your telnet client's
  window can do it.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1994)
* **Regular expression part searches** for part/tag names.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1918), documenation: search for "PARTSTAGGEDPATTERN" (http://ksp-kos.github.io/KOS_DOC/structures/vessels/vessel.html#method:VESSEL:PARTSTAGGEDPATTERN).
* **Choose the IP address** of the telnet server, from the ones your computer has available, instead
  of kOS picking one arbitrarily.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1976)
* **Allow local variables in triggers** In order to support the kOS callback
  system used by the GUI, we also finally had to add support for proper local
  variable scoping to triggers like WHEN and ON.  A trigger's condition
  variables are no longer limited to having to be global.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2031)
* **Pressure at a given altitude** is now something you can query from an atmosphere.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2000), documentation: search for "ALTITUDEPRESSURE" (http://ksp-kos.github.io/KOS_DOC/structures/celestial_bodies/atmosphere.html#method:ATMOSPHERE:ALTITUDEPRESSURE).
* **Get a LATLNG for some other body than the current one.**
  [pull request](https://github.com/KSP-KOS/KOS/pull/2001), documentation: search for "GEOPOSITIONLATLNG" (http://ksp-kos.github.io/KOS_DOC/structures/celestial_bodies/body.html#method:BODY:GEOPOSITIONLATLNG).

### BUG FIXES
* Fix kOS toolbar button sometimes failing to appear in Blizzy Toolbar Mod.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1902)
* Fix SKID Chip emulator's sync lag when physics is slow.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1915/commits/c9d9dcd18561903e122531605194b2685fc4fb15)
* Fix SKID Chip emulator unable to use voices 6 through 9 because of how they were initialized.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1927)
* Forgot to document GETMODULEBYINDEX.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1962)
* Fix inability of a script to SET TARGET when KSP game is not the focused window.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1934)
* Fix iterator that lets you walk the characters in a string with "for" loop.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1938)
* Removed some Unity hooks that despite being empty and doing nothing,
  nonetheless still ate up a bit of time to pointlessly call and return from.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1965)
* Fix use of the min()/max() function on string comparisons
  [pull request](https://github.com/KSP-KOS/KOS/pull/1967)
* Fix science data transmissions
  [pull request](https://github.com/KSP-KOS/KOS/pull/1979)
* Fix unnessary duplicated of clones of vessel objects (was causing large garbage collection hangs).
  [pull request](https://github.com/KSP-KOS/KOS/pull/1983)
* Fixed several small documentation errors:
  [pull request](https://github.com/KSP-KOS/KOS/pull/1928)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1986)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1992)
* Fixed float->boolean mapping error.  The values no longer round to integer before becoming boolean.
  (i.e. 0.01 should be True, not get rounded to False (0) like it used to.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1990)
* Fixed ATM:SEALEVELPRESSURE units to agree with the documentation.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2000)
* Fixed bug that had made the sounds fail to emit for beep and keyclick.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2003)
* Fixed vessel:TOSTRING to return "Vessel(blarg)" instead of "Ship(blarg").
  [pull request](https://github.com/KSP-KOS/KOS/pull/2005)
* Fixed null-ref errors when using NEXTPATCH when there is no next patch.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2009)
* Fixed a few bugs related to kOS cleaning up after itself when the vessel splits into two
  or two vessels join together, or a vessel blows up.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2010)

# v1.0.3 (for KSP 1.2.2) Make a little noise! (Part Deux)

This release is nearly identical to v1.0.2, except that it was compiled against
binaries from KSP v1.2.2 (released just before we published) and the version numbers
have been advanced.  While it appears that kOS v1.0.2 is compatible with KSP v1.2.2,
we wanted to err on the side of caution and provide an explicitly compatible release.
Please review the changelog for v1.0.2 if you are upgrading from an earlier version.

# v1.0.2 (for KSP 1.2.1) Make a little noise!

### BREAKING CHANGES
* As always, if you use the compiler feature to make KSM files, you should
  recompile the KSM files when using a new release of kOS or results will
  be unpredictable.
* Most in game settings are now integrated with KSP's difficulty settings window.
  You will be prompted to migrate existing settings when you load your save game.
  Telnet settings are still stored in the old config file, but everything else is
  now stored within the save file.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1843) | [documentation](http://ksp-kos.github.io/KOS_DOC/general/settingsWindows.html#ksp-difficulty-settings-window)
* Calls to resource suffixes on the `stage` bound variable are no longer rounded to 2 decimal places.
  Previously they were rounded to assist in detecting "zero" fuel, but they cause inequality issues
  when comparing to the newer `stage:resources` list or `stage:resourceslex` values.
* The behavior of the resource suffixes on the `stage` bound variable has changed with regard
  to asparagus staging.  If you have smaller tanks that **can** be staged, `stage:liquidfuel`
  will return `0` even if you still have an engine firing.  This is a break from previous versions
  of kOS, but is aligned with the current UI design.  Previous versions also aligned with the KSP
  UI, but the UI mechanic was updated with KSP 1.2.x

### NEW FEATURES
* Official release for KSP version 1.2.1!
* kOS now has a procedural sound system!  You can use it to play customized error
  tones or make your own musical notes.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1859) | [documentation](http://ksp-kos.github.io/KOS_DOC/general/skid.html)
* Support for CommNet and modifications to make RemoteTech and CommNet use similar systems.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1850) | [documentation](http://ksp-kos.github.io/KOS_DOC/commands/communication.html#connectivity-managers)
* Trajectories integration enabled via new `ADDONS:TR`
  [pull request](https://github.com/KSP-KOS/KOS/pull/1603) | [documentation](http://ksp-kos.github.io/KOS_DOC/addons/Trajectories.html)
* Added new setting for default terminal brightnes, and updated default value to 70%
  [pull request](https://github.com/KSP-KOS/KOS/pull/1872) | [documentation](http://ksp-kos.github.io/KOS_DOC/general/settingsWindows.html#ksp-difficulty-settings-window)
* Added `VELOCITY` and `ALTITUDEVELOCITY` suffixes to `geocoordinates
  [pull request](https://github.com/KSP-KOS/KOS/pull/1874) | [documentation](http://ksp-kos.github.io/KOS_DOC/math/geocoordinates.html#attribute:GEOCOORDINATES:VELOCITY)
* Added `TONUMBER` and `TOSCALAR` suffixes to `string` values for parsing numerical values
  [pull request](https://github.com/KSP-KOS/KOS/pull/1883) | [documentation](http://ksp-kos.github.io/KOS_DOC/structures/misc/string.html#method:STRING:TONUMBER)
* New `steeringmanager` suffix `ROLLCONTROLANGLERANGE` to dictate the maximum value
  of `ANGLEERROR` for which the manager will attempt to control roll
  [commit](https://github.com/KSP-KOS/KOS/commit/3c1d5d15fa5834858204a07870c2e768870fce72) | [documentation](http://ksp-kos.github.io/KOS_DOC/structures/misc/steeringmanager.html#attribute:STEERINGMANAGER:ROLLCONTROLANGLERANGE)
* KSM files are now gzip compressed internally, dramatically reducing the file size.
  Existing KSM files **should** still load, but see above for the recommendation to
  recompile all KSM files.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1858)

### BUG FIXES
* Fix for throwing errors when another mod uses dynamic assembly
  [pull request](https://github.com/KSP-KOS/KOS/pull/1851)
* Update Blizzy toolbar wrapper to the most recent version
  [pull request](https://github.com/KSP-KOS/KOS/pull/1851)
* Fix for local kOS hard disks breaking when loading with 4 byte long files
  [pull request](https://github.com/KSP-KOS/KOS/pull/1858)
* kOS no longer uses a write-only lock when writing to the archive, preventing
  an error when accessing a file opened for reading by another program
  [pull request](https://github.com/KSP-KOS/KOS/pull/1870)
* Fix for duplicate functions/locks breaking ksm files
  [pull request](https://github.com/KSP-KOS/KOS/pull/1871)
* Fix for null ref error when editing node suffixes on KSP 1.2.1
  [pull request](https://github.com/KSP-KOS/KOS/pull/1876)
* Fix for issue where a body with the same name as one of our bound variables would block
  access to said variable (specifically Eta in Galileo's Planet Pack blocked the `eta` bound variable)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1888)
* Fix for getting the science value and transmit value in sandbox mode
  [pull request](https://github.com/KSP-KOS/KOS/pull/1889)
* Fix error where `unlock all` inside a trigger will try to
  unlock functions too
  [pull request](https://github.com/KSP-KOS/KOS/pull/1889)

# v1.0.1 (for KSP 1.1.3) Let's take some input!

## Why 1.1.3 and not 1.2?

We wanted to get the last bug fixes and new features into the hands of any users
who might not update KSP to 1.2 right away.  Traditionally there are some mods
that take a while to update when KSP releases a new version, and many users
choose to wait for all of their favorite mods to update before upgrading KSP.
By releasing in conjunction with the update, we can ensure that as many users as
possible have access to these latest updates.  We will be releasing a version of
kOS that is compatible with KSP 1.2 as soon as possible after the final build is
released to the public.

### BREAKING CHANGES

* As always, if you use the compiler feature to make KSM files, you should
  recompile the KSM files when using a new release of kOS or results will
  be unpredictable.
* The `stage` command/function now implements the yield behavior, waiting until
  the next physics tick to return.  This ensures that all vessel stats are
  updated together. (https://github.com/KSP-KOS/KOS/pull/1807)

### NEW FEATURES

* Functions and opcodes can now tell the CPU to yield (wait) based on their own
  arbitrary logic.  This allows future functions to be "blocking" (preventing
  further execution) without blocking KSP itself.
  (https://github.com/KSP-KOS/KOS/issues/1805,
  https://github.com/KSP-KOS/KOS/pull/1807, and
  https://github.com/KSP-KOS/KOS/pull/1820)
* New `timewarp` structure, available on the `kuniverse` bound variable. This
  structure provides additional information and control over time warp. The old
  warp bound variables remain in place.
  (https://github.com/KSP-KOS/KOS/issues/1790 and
  https://github.com/KSP-KOS/KOS/pull/1820)
* Introducing a new `terminalinput` structure for keyboard interaction from
  within scripts!  Currently support is only provided for getting single
  characters.
  (https://github.com/KSP-KOS/KOS/pull/1830)

Please check http://ksp-kos.github.io/KOS_DOC/changes.html for more detailed
explanations for the new features.

### BUG FIXES

* Fix for formatting of `time:clock` to pad zeros
  (https://github.com/KSP-KOS/KOS/issues/1771 and
  https://github.com/KSP-KOS/KOS/pull/1772)
* Fix for not being able to construct a `vessel("foo")` if "foo" is the name of
  the current vessel (https://github.com/KSP-KOS/KOS/issues/1565 and
  https://github.com/KSP-KOS/KOS/pull/1802)
* RemoteTech steering should be fixed.  At worst you may see a 1sec gap with
  the controls, as we now refresh the steering callback about once per second.
  (https://github.com/KSP-KOS/KOS/issues/1806 and
  https://github.com/KSP-KOS/KOS/pull/1809)
* Named functions defined within anonymous functions will no longer throw an
  error (https://github.com/KSP-KOS/KOS/issues/1801 and
  https://github.com/KSP-KOS/KOS/pull/1811)
* `lock steering` no longer throws an exception inside of an anonymous functions
  (https://github.com/KSP-KOS/KOS/issues/1784 and
  https://github.com/KSP-KOS/KOS/pull/1811)
* Compiled programs that include a large number of named functions should no
  longer throw an error (https://github.com/KSP-KOS/KOS/issues/1796 and
  https://github.com/KSP-KOS/KOS/pull/1812)
* Fixed the first call to `wait` after the cpu boots
  (https://github.com/KSP-KOS/KOS/issues/1785)
* Various documentation fixes (https://github.com/KSP-KOS/KOS/pull/1810,
  https://github.com/KSP-KOS/KOS/pull/1823, and
  https://github.com/KSP-KOS/KOS/pull/1834)


# v1.0.0 (for KSP 1.1.3) Hey let's stop calling it Beta.

### About the name:

kOS has been around long enough that we figured it was long overdue
for us to stop calling it 0.something.  Lots of people are using it,
and we're worried about backward compatibility enough that we're not
really treating it like a Beta anymore.  This version contains mostly
a few things that we knew might break backward compatibility so we'd
been putting them off for a long time.  A jump to 1.0 seems a good time
to add those changes.

Of course, it has lots of other changes for whatever else was being
worked on since the last release.

### BREAKING CHANGES

* As always, if you use the compiler feature to make KSM files, you should
  recompile the KSM files when using a new release of kOS or results will
  be unpredictable.
* New Subdirectories ability has deprecated several filename commands such
  as ``delete``, ``copy``, and ``rename``.  They will still work, but will
  complain with a message every time you use them, as we may be removing
  them eventually.  The new commands ``deletepath``, ``copypath``, and
  ``movepath`` described below are meant to replace them.
* When using a RemoteTech antenna that requires directional aiming,
  in the past you could aim it at mission control with
  ``SETFIELD("target", "mission-control")`` and now you have to
  say ``SETFIELD("target", "Mission Control")`` instead, due to
  changes in RT's naming schemes.
* Previously the Y and Z axes of SUN:VELOCITY:ORBIT were swapped.
  (https://github.com/KSP-KOS/KOS/issues/1764)
  This has been fixed so it is now the same as for any other body,
  however scripts might exist that had previously been swapping them
  back to compensate for this, and if there were they would now break
  since that swapping is no longer needed.

### NEW FEATURES

* **Subdirectories:** (http://hvacengi.github.io/KOS/commands/files.html)
  You are now able to store subdirectories ("folders") in your volumes,
  both in the archive and in local volumes.  To accomodate the new feature
  new versions of the file manipulation commands had to be made (please
  go over the documentation in the link given above).  In the Archive,
  which is really your ``Ships/Script/`` directory on your computer,
  these subdirectories are stored as actual directories in your computer
  filesystem.  (For example, the file ``0:/dir1/dir2/file.ks`` would be
  stored at ``Kerbal Space Program/Shipts/Script/dir1/dir2.file.ks`` on
  your real computer.) In local volumes, they are stored in the persistence.sfs
  savegame file like usual.
  (Pull Request discussion record: https://github.com/KSP-KOS/KOS/pull/1567)
  * Boot subdirectory: (http://hvacengi.github.io/KOS/general/volumes.html#special-handling-of-files-in-the-boot-directory)
    To go with Subdirectories, now you make a subdirectory in your archive
    called ``boot/``, and put all the candidate boot files there.  When
    selecting a boot file in the VAB or SPH, the selections are taken from
    there and need not contain the "boot_" prefix to the filename anymore.
    Old boot files will be grandfathered in that are named the old way,
    however.
  * CORE:BOOTFILENAME is now a full path.  i.e. ``boot/myfile.ks``.
  * PATH structure now allows you to get information about
    the new full subdirectories system from your scripts.
    (http://hvacengi.github.io/KOS/structures/volumes_and_files/path.html)
  * New RUNPATH command now allows any arbitrary string expression to be
    used as the name of the file to be run.  i.e.
    ``set basename to "prog". set num to 1. runpath(basename+num, arg1). // same as run prog1(arg1)``.
    As part of the support for this, programs with a large number of RUN
    commands (or RUNPATH commands) should now take up a bit less
    of a memory footprint than they used to in their compiled form
    (and thus in KSM files too).
    (http://hvacengi.github.io/KOS/commands/files.html#runpath-and-runoncepath)
* **Communication between scripts** on different CPUs of the same vessel or
  between different vessels.
  (http://hvacengi.github.io/KOS/commands/communication.html)
  * A new structure, the ``Message``, contains some arbitrary piece of
    data you choose (a number, a string, a list collection, etc), and
    some header information kOS will add to it that describes where it
    came from, when it was sent, and so on.  What you choose to do
    with these arbitrary chunks of data is up to you.  kOS only lets
    you send them.  You design your own protocol for what the data means.
  * If RemoteTech is installed, a connection is needed to send a message
    to another vessel (but not to a CPU on the same vessel).  And, the
    message won't actually show up in the other vessel's queue until the
    required lightspeed delay.
  * To handle KSP's inability to have different vessels far away from each
    other both fully loaded and active, you do have to switch scenes back
    and forth between distant vessels if you want them to have a conversation
    back and forth.  Messages that were meant to arrive on a vessel while
    it wasn't within active loading range will wait in the recever's vessel
    queue until you switch to it, so you don't have to hurry and switch
    "in time" to get the message.
* **Added anonymous functions :**
  (http://hvacengi.github.io/KOS/language/anonymous.html)
  By placing arbitrary braces containing the body of a function anywhere
  within the script that an expression is expected, the compiler builds
  the function code right there and then returns a delegate of it as the
  value of the expression.
* **New 3rd-party addon framework** (https://github.com/KSP-KOS/KOS/tree/develop/src/kOS/AddOns/Addon%20Readme.md)
  allows authors of other KSP mods to add hooks into kOS so that kOS
  scripts can interface with their mods more directly, without kOS
  developers having to maintain that code themselves in the kOS
  repository.
  (Pull Request discussion record: https://github.com/KSP-KOS/KOS/pull/1667)
* **allow scripted vessel launches**
  ``KUNIVERSE:GETCRAFT()``, ``KUNIVERSE:LAUNCHCRAFT()``, ``KUNIVERSE:CRAFTLIST()``,
  and ``KUNIVERSE:LAUNCHCRAFTFROM()`` allow you to script the changing of scenes
  and loading of vessels into those scenes.  While this breaks the 4th wall
  quite a bit (how would an autopilot choose to manufacture an instance of the
  plane?), it's meant to help with script testing and scripts that try to
  repeatedly run the same mission unattended.
  (http://hvacengi.github.io/KOS/structures/misc/kuniverse.html)
* **eta to SOI change:**
  Added SHIP:OBT:NEXTPATCHETA to get the time to the next orbit patch
  transition (SOI change).
  (http://hvacengi.github.io/KOS/structures/orbits/orbit.html#attribute:ORBIT:NEXTPATCHETA)
* **get control-from:**
  Added ``SHIP:CONTROLPART`` to return the ``Part`` of the vessel that is
  currently set as its "control from here" part.
  (http://hvacengi.github.io/KOS/structures/vessels/vessel.html#attribute:VESSEL:CONTROLPART)
* **maneuver nodes as a list:**(
  New ``ALLNODES`` bound variable that returns a list of all the currently
  planned manuever nodes (the nodes you could iterate through with
  ``NEXTNODE``, but rendered into one list structure).
  (http://hvacengi.github.io/KOS/bindings#allnodes)
* Several new **pseudo-action-groups** (akin to "panels on", that aren't
  action groups as far as stock KSP is concerned, but kOS treats them like
  action groups) were added.  (http://hvacengi.github.io/KOS/commands/flight/systems#kos-pseudo-action-groups)
* Ability to **get/set the navball mode** (surface, orbital, target) with
  the ``NAVMODE`` bound variable:
  i.e. ``SET NAVMODE TO "SURFACE".``.
* **UniqueSet structure.** (http://hvacengi.github.io/KOS/structures/collections/uniqueset.html)
  A collection intended for when all you care about is whether a equivalent
  object exists or doesn't exist yet in the collection, and everything else
  (order, etc) doesn't matter.

### BUG FIXES

* In some cases (https://github.com/KSP-KOS/KOS/issues/1661) the program
  wouldn't stop immediately when you execute a  ``kuniverse`` command that
  reloads a save or switches scenes.  It would instead finish out the
  remainder of the IPU instructions in the current physics tick.
  After the fix, causing a scene change (or reload) automatically stops the
  program right there since anything it does after that would be moot as
  the game is about to remove everything it's talking about from memory.
* If using "Start on archive", with Remote Tech, a misleading "power starved"
  error was thrown when you reboot a probe that's out of antenna range.
  (https://github.com/KSP-KOS/KOS/issues/1363)
* ``unchar("a")`` was apparently broken for a few months and we hadn't noticed.
  The root cause was that its implementation had to be edited to comply with
  the change that enforced the VM to only use kOS ``Structure`` types on the
  stack.  The need for that change had been missed.
  (https://github.com/KSP-KOS/KOS/issues/1692)
* Previously Infernal Robotics allowed you to move servos that weren't even
  on your own vessel and you shouldn't have direct control over.  This has
  been fixed.  (https://github.com/KSP-KOS/KOS/issues/1540)
* Refactored previous non-working technique for quicksave/quickload to
  turn it into something that works.
  (https://github.com/KSP-KOS/KOS/issues/1372)
* There were cases where using CTRL-C to abort a program would cause some
  old cruft to still be leftover in the VM's stack.  This made the system
  fail to clear out the names of functions that were no longer loaded in
  memory, making it act like they were still reachable and call-able.
  (https://github.com/KSP-KOS/KOS/issues/1610)
* Some types of ``Resource`` didn't contain the ``:DENSITY`` suffix like the
  documentation claimed they would.
  (https://github.com/KSP-KOS/KOS/issues/1623)

# v0.20.1 KSP 1.1.2 and bug repair

The biggest reason for this release is to handle two game-breaking
problems caused by recent alterations in the API that kOS hadn't
adapted to correctly yet.

The "remit" of this release is purely to fix a few bugs, and patch up
a few things where KSP 1.1 had changes we didn't catch.  Mostly,
that's cases where previously working code in kOS had now become a
bug, but it also includes a few other bug fixes not related to KSP 1.1.

But any new features (rather than bug fixes) in the pipeline not directly
related to that "remit" are not in this release.

### BREAKING CHANGES
* `STEERINGMANAGER:SHOWRCSVECTORS` and `STEERINGMANAGER:SHOWENGINEVECTORS` are now obsolete and will throw an error.

### BUG FIXES
* Infinitely growing mass:  Realism Overhaul users could not use kOS anymore, because kOS was re-adding its small module mass to the part again and again each physics tick.  Even though the mass of kOS is small, adding it to the part 25 times a second quickly made the vessel grow too massive to do anything with.  The bug was not caught earlier because it only happened if kOS was added to parts other than the parts kOS ships with (i.e. by using ModuleManager), and those parts also had other mass-affecting modules on them.  Although discovered in Realism Overhaul, the problem could have been affecting any users who used kOS in that same fashion.  The cause was traced to an incorrect use of the new mass API by kOS and has been fixed. (https://github.com/KSP-KOS/KOS/pull/1644).
* "SET TARGET TO FOO." while the terminal is open was failing.  Now it works.  (The kOS terminal locks out all other inputs so your keypresses don't affect the ship, but as of KSP 1.1 the "all" input lock it was using to do so also includes the ability to set target, which it didn't before.) (https://github.com/KSP-KOS/KOS/pull/1636)
* Incorrect value for MeanAnomalyAtEpoch fixed.  It was multiplying the value by the conversion factor for radians-to-degrees twice, rather than just once.  (https://github.com/KSP-KOS/KOS/pull/1642)
* GeoCoordinates were not serializing properly.  Now they are. (https://github.com/KSP-KOS/KOS/pull/1615).
* Finally fully obsoleted the years-old suffixes for trying to do antenna range the old way (before we just relied on Remote Tech to do antenna work for us).  (https://github.com/KSP-KOS/KOS/pull/1607).
* Bug fixes for catching a few more cases where staging or decoupling part of the craft away was still confusing SteeringManager into trying to lock out, or take control of, the wrong half of the craft.  (https://github.com/KSP-KOS/KOS/pull/1544).

### NEW FEATURES
* KSP 1.1 now allows you to lock the gimbals for the three pitch/yaw/roll axes individually on engines, as 3 different settings, rather than just lock the whole gimbal for all directions.  kOS now lets you access this ability (https://github.com/KSP-KOS/KOS/pull/1622).

# v0.20.0 KSP 1.1 Hype!

This release is functionally identical to v0.19.3, it is recompiled against the
KSP 1.1 release binaries (build 1230)

### BUG FIXES
* [KSP1.1] Removing a node leaves an artifact (https://github.com/KSP-KOS/KOS/issues/1572 https://github.com/KSP-KOS/KOS/issues/1576)
* [KSP1.1] Toolbar button doesn't display (https://github.com/KSP-KOS/KOS/issues/1573 https://github.com/KSP-KOS/KOS/issues/1569)

# v0.19.3 Last (intended) 1.0.5 update.

(This is the last planned update to work with KSP 1.0.5 unless
it breaks something big that requires an emergency patch.)

### BREAKING CHANGES
* Triggers may now go beyond the limits of the IPU (https://github.com/KSP-KOS/KOS/pull/1542) but are no longer guaranteed to execute within a single update frame.  See http://ksp-kos.github.io/KOS_DOC/general/cpu_hardware.html#triggers and http://ksp-kos.github.io/KOS_DOC/general/cpu_hardware.html#cpu-update-loop for more details.

### NEW FEATURES
* Profiling output via `ProfileResult()` (https://github.com/KSP-KOS/KOS/pull/1534)

### BUG FIXES
* Removed delay when enabling/disabling auto changeover for multi mode engines (https://github.com/KSP-KOS/KOS/pull/1451)
* Improve performance of various math functions (https://github.com/KSP-KOS/KOS/issues/1553 https://github.com/KSP-KOS/KOS/pull/1523 https://github.com/KSP-KOS/KOS/pull/1563)
* `on` logic now evaluates expressions and suffixes, instead of requiring a raw variable (https://github.com/KSP-KOS/KOS/issues/1376 https://github.com/KSP-KOS/KOS/pull/1542)
* Documentation no longer inserts a space around highlighted search terms (https://github.com/KSP-KOS/KOS/pull/1548)
* You can now use lock objects with the same identifier from within compiled scripts, like `lock throttle...` (https://github.com/KSP-KOS/KOS/issues/691 https://github.com/KSP-KOS/KOS/issues/1253 https://github.com/KSP-KOS/KOS/issues/1557 https://github.com/KSP-KOS/KOS/pull/1561)
* The script parsing logic has been updated to improve compile times by roughly 50% (https://github.com/KSP-KOS/KOS/pull/1566)

# v0.19.2

This release is here primarily to fix a problem that made
the new v0.19.1 terminal unusable for users who have to
use low resolution texture settings in the Unity graphics
configuration panel.

### BREAKING
* Nothing new breaking in this version is known about.

### NEW FEATURES
* New alias KUNIVERSE:FORCEACTIVE() can be used instead of the longer name KUNIVERSE:FORCESETACTIVEVESSEL().
* More robust use of the font_sml.png file allows for replacement of font_sml.png by the end-user.
  (However this may only be useful for a limited time, as Unity5 might make us implement the font differently
  anyway.)

### BUG FIXES
* New terminal now works again at low texture resolution settings
  (https://github.com/KSP-KOS/KOS/issues/1513).
* New terminal shows grey color on power-off again
  (https://github.com/KSP-KOS/KOS/issues/1525).
* Terminal now shows a boot message that mentions the documentation URL
  (https://github.com/KSP-KOS/KOS/issues/1527).
* Fixed a situation that could make KSP itself crash if a script
  attempted to perform an equality comparison on types that hadn't
  had a meaningful implementation of equality defined.  (Instead
  of a proper error message about it from kOS, kOS got stuck in
  recursion.)


# v0.19.1

This release is a patch to v0.19.0, fixing some things
found by the user community in the two days shortly after
v0.19.0 released.

It also happens to contain a few terminal window features
that were being worked on before v0.19.0 but were not deemed
ready yet when 0.19.0 was released.

### NEW FEATURES
* PIDLoop tutorial section in the docs edited to mention new PIDLoop()
  function that did not exist back when that page was first written.
  (http://ksp-kos.github.io/KOS_DOC/tutorials/pidloops.html)
* New Terminal GUI doodads and widgets: A brightness slider,
  and the ability to zoom the character width and height.  Also
  made the transparency and dimming of the 'non-active' terminals
  a bit less severe so you can still read them when un-focused.
  Also, these new features can be script controlled by new
  suffixes, however it is unclear if that feature (doing it from
  a script) will remain in the future so use it with care:
  (http://ksp-kos.github.io/KOS_DOC/structures/misc/terminal.html)

### BUG FIXES
* Fixed file rename bug on local hard disks:
  (https://github.com/KSP-KOS/KOS/issues/1498)
* Fixed boot files can be larger than the local disk
  (https://github.com/KSP-KOS/KOS/issues/1094)
* Fixed a bug where Infernal Robotics would break when switching vessels or
  reverting. (https://github.com/KSP-KOS/KOS/issues/1501)
* Fixes problems with using PartModule's SetField(), and infernal Robotics which
  had been failing for all cases where the field was a "float".
  (https://github.com/KSP-KOS/KOS/issues/1503).
  There may have been other places this bug affected, but this is
  where it was noticed.  Hypothetically, anywhere the stock game's
  library insists on only accepting a single-precision float and
  not a double would have had the problem.
* Improve steering when small control magnitudes are required.
  (https://github.com/KSP-KOS/KOS/issues/1512)

# v0.19.0

### BREAKING CHANGES
* As usual, you must recompile any KSM files when using the new version.
* Vecdraw :SCALE no longer applied to :START.  Only applied to :VEC.
* Varying power consumption might make it so if you have high IPU settings some designs might run out of power when they didn't before.  (in most cases it should draw less power for most people).
* !!!! Default extension of ".ks" is no longer applied to all new filenames created.  But it still will be looked for when reading existing files if you leave the extension off !!!!
* FileInfo information now moved to Volume (http://ksp-kos.github.io/KOS_DOC/structures/volumes_and_files/volume.html).
* VOLUME:FILES was returning a LIST(), now it returns a LEXICON who's keys are the filename.
* String sort-order comparisons with "<" and ">" operators were implemented wrongly and just compared lengths.  Now they do a character-by-character comparison (case-insensitively).  On the off chance that anyone was actually trying to use the previous weird length-comparison behavior, that would break.

### NEW FEATURES
* Art asset rework.  The meshes and textures of the kOS CPU parts have recieved an update, and a new KAL9000 high-end computer part was included.
* Varying power consumption.  Units of electric charge used now varies depending on CPU speed and how much the CPU is being actually used.  If your IPU setting is low, or if your program isn't doing very much and is just stuck on a `wait` statement, it won't use as much power. (http://ksp-kos.github.io/KOS_DOC/general/cpu_hardware#electricdrain)
* Ability to read and write whole files at a time as one big string. (http://ksp-kos.github.io/KOS_DOC/structures/volumes_and_files/volumefile.html)
* User Functions can now be referred to with function pointers, or "delegates".  (http://ksp-kos.github.io/KOS_DOC/language/delegates.html)
* Automatic serialization system to save/load some kinds of data values to JSON-format files (http://ksp-kos.github.io/KOS_DOC/commands/files.html#writejson-object-filename)
* User Programs and Functions now allow trailing optional parameters with defaulted values. (http://ksp-kos.github.io/KOS_DOC/language/user_functions.html#optional-parameters-parameter-defaults).
* There are now some suffixes that work on all value types, even primitive scalars.  To accomplish this, a new "encapsulation" system has wrapped all kOS structures and primitive types inside a generic base type.  (http://ksp-kos.github.io/KOS_DOC/structures/reflection.html)
* ENGINE type now supports multi-mode cases and has its gimbal accessible through :GIMBAL suffix (http://ksp-kos.github.io/KOS_DOC/structures/vessels/engine.html)
* Added GIMBAL:LIMIT suffix. (http://ksp-kos.github.io/KOS_DOC/structures/vessels/gimbal.html)
* Better support for DMagic's Orbital Science mod (http://ksp-kos.github.io/KOS_DOC/addons/OrbitalScience.html)
* Char() and Unchar() functions for translating unicode numbers to characters and visa versa (http://ksp-kos.github.io/KOS_DOC/math/basic.html#function:CHAR)
* New Range type for iterating over hardcoded lists (http://ksp-kos.github.io/KOS_DOC/structures/collections/range.html).
* Ability to iterate over the characters in a string using a FOR loop, as if the string was a LIST() of chars.
* New higher level cpu part. (https://github.com/KSP-KOS/KOS/pull/1380)
* HASTARGET and HASNODE functions (http://ksp-kos.github.io/KOS_DOC/bindings.html?highlight=hastarget)
* :JOIN suffix for LIST to make a string of the elements (http://ksp-kos.github.io/KOS_DOC/structures/collections/list.html#method:LIST:JOIN)
* KUNIVERSE now lets you read hours per day setting (http://ksp-kos.github.io/KOS_DOC/structures/misc/kuniverse.html#attribute:KUNIVERSE:HOURSPERDAY)
* The reserved word ARCHIVE is now a first-class citizen with proper binding, so you can do SET FOO TO ARCHIVE and it will work like you'd expect.
* New Lexicon creation syntax to make a Lexicon and populate it all in one statement. (http://ksp-kos.github.io/KOS_DOC/structures/collections/lexicon.html?highlight=lexicon#constructing-a-lexicon)

### BUG FIXES
* Numerous additional checks to prevent control of other vessels the kOS CPU isn't attached to.
* The error beep and keyboard click sounds now obey game's UI volume settings. (https://github.com/KSP-KOS/KOS/pull/1287)
* Fixed two bugs with obtaining waypoints by name. (https://github.com/KSP-KOS/KOS/issues/1313) (https://github.com/KSP-KOS/KOS/pull/1319)
* Removed unnecessary rounding of THRUSTLIMIT to nearest 0.5%, now it can be more precise. (https://github.com/KSP-KOS/KOS/pull/1329)
* Removed the ability to activate both modes on multi-mode engine simultaneously.
* LIST ENGINES now lists all engines and displays part names instead of module names. (https://github.com/KSP-KOS/issues/1251)
* Fixed bug that caused hitting ESC to crash the telnet server. (https://github.com/KSP-KOS/KOS/issues/1328)
* Some exceptions didn't cause beep, now they all do. (https://github.com/KSP-KOS/KOS/issues/1317)
* Vecdraw :SCALE no longer applied to :START.  Only applied to :VEC. (https://github.com/KSP-KOS/KOS/issues/1200)
* Fixed bug that made up-arrow work incorrectly when the cursor is at the bottom of the terminal window. (https://github.com/KSP-KOS/KOS/issues/1289)
* A multitude of small documentation fixes (https://github.com/KSP-KOS/KOS/pull/1341)
* Fixed a bug when performing an undock (https://github.com/KSP-KOS/KOS/issues/1321)
* IR:AVAILABLE was reporting incorrectly ()
* Boot files now wait until the ship is fully unpacked and ready (https://github.com/KSP-KOS/KOS/issues/1280)
* The Vessel :HASBODY (aliases :HASOBT and :HASORBIT) suffix was in the documentation, but had been lost in a refactor last year.  It is put back now.
* String sort-order comparisons with "<" and ">" operators were implemented wrongly and just compared lengths. Now they do a character-by-character comparison (case-insensitively)
* Small documentation edits and clarifications all over the place.

### KNOWN issues
* Using `lock` variables in compiled scripts with a duplicate identifier (like "throttle") throws an error (https://github.com/KSP-KOS/KOS/issues/1347 and https://github.com/KSP-KOS/KOS/issues/1253).
* Occasionally staging with a probe core or root part in the ejected stage will break cooked steering (https://github.com/KSP-KOS/KOS/issues/1492).
* The limitations of RemoteTech integration can be bypassed by storing a volume in a variable before the ship looses a connection to the KSC (https://github.com/KSP-KOS/KOS/issues/1464).

### CONTRIBUTORS THIS RELEASE

(These are generated from records on Github of anyone who's Pull Requests are part of this release.)
(Names are simply listed here alphabetically, not by code contribution size.  Anyone who even had so much as one line of change is mentioned.)

Stephan Andreev (ZiwKerman) https://github.com/ZiwKerman
Bert Cotton (BertCotton) https://github.com/BertCotton
Kevin Gisi (gisikw) https://github.com/gisikw
Peter Goddard (pgodd) https://github.com/pgodd
Steven Mading (Dunbaratu) https://github.com/Dunbaratu
Eric A. Meyer (meyerweb) https://github.com/meyerweb
Tomek Piotrowski (tomekpiotrowski) https://github.com/tomekpiotrowski
Brad White (hvacengi) https://github.com/hvacengi
Chris Woerz (erendrake) https://github.com/erendrake  (repository owner)
(name not public in github profile) (alchemist_ch) https://github.com/AlchemistCH
(name not public in github profile) (tdw89) https://github.com/TDW89
Philip Kin (pipakin) https://github.com/pipakin

# v0.18.2

[Insert witty title here :-P]
------------------------------

### BREAKING CHANGES
* As usual, you MUST recompile all KSM files before running them on the new version.  Some of the changes have altered how the VM works.
* Nothing else... we hope.

### NEW FEATURES
* Compatibility with KSP version 1.0.5
* `run once ...` syntax to run a script only once per session ( http://ksp-kos.github.io/KOS_DOC/commands/files.html#run-once-program )
* Volumes and processors have better integration ( http://ksp-kos.github.io/structures/vessels/volume.html#structure:VOLUME )
* Volume titles default to the name tag of the Processor part (only on launch) ( http://ksp-kos.github.io/KOS_DOC/general/volumes.html#naming-volumes )
* New suffixes for interacting with kOS Processor modules (including `core`) ( http://ksp-kos.github.io/KOS_DOC/commands/processors.html )
* `debuglog(...)` function to print directly to the KSP log file ( http://ksp-kos.github.io/KOS_DOC/structures/misc/kuniverse.html#method:KUNIVERSE:DEBUGLOG )
* New `queue` and `stack` data structures ( http://ksp-kos.github.io/KOS_DOC/structures/misc/queue.html and http://ksp-kos.github.io/KOS_DOC/structures/misc/stack.html )

### BUG FIXES
* The processor's mode (on/off/starved) is now saved and restored ( https://github.com/KSP-KOS/KOS/issues/1172 )
* Fixed stage resources again to address a change in KSP 1.0.5 ( https://github.com/KSP-KOS/KOS/issues/1242 )
* Fix occasional instances of flight controls getting disabled during a docking/undocking/staging event ( https://github.com/KSP-KOS/KOS/issues/1205 )
* kOS can now trigger module events with RemoteTech installed and no KSC connection ( https://github.com/RemoteTechnologiesGroup/RemoteTech/issues/437 )
* Fixed handling of multiple thrust/gimbal transforms and corrected some of their directions ( https://github.com/KSP-KOS/KOS/issues/1259 )

# v0.18.1

Steering More Much Betterer
----------------------

### Changes
* Changed default MaxStoppingTime to 2 seconds ( was 1 )

### BUG FIXES
* Fixed a issue where the effect of the Kd parameter of PIDLoop was having a reversed effect #1229
* Fixes an issue where NO_FLOW resources ( eg SolidFuel ) were not reporting correctly #1231

# v0.18

Steering Much Betterer
----------------------

### BREAKING CHANGES
* As usual, you MUST recompile all KSM files before running them on the new version.  Some of the changes have altered how the VM works.
* New LOADDISTANCE obsoletes the previous way it worked ( http://ksp-kos.github.io/KOS_DOC/structures/misc/loaddistance.html )
* Fixed broken spelling of "ACQUIRE" on docking ports.  The old spelling of "AQUIRE" won't work anymore.
* Changed the bound variable "SURFACESPEED" to "GROUNDSPEED" instead, as the meaning of "SURFACESPEED" was confusingly ambiguous.
* New arg/param matching checks make some previously usable varying argument techniques not work.  (We don't think anyone was using them anyway).
* Disabled the ability to control vessels the kOS computer part is not actually attached to.  This always used to be possible, but it shouldn't have been as it breaks the theme of kOS.  This affects all the following: vessel:control, part:controlfrom, part:tag (can still get, but not set), partmodule:doaction, partmodule:doevent, partmodule:setfield (can still getfield).  These things become read-only when operating on any vessel other than the one the executing kOS module is actually part of.

### NEW FEATURES
* THE BIG ONE:  Fix to Cooked Steering! Should help people using torque-less craft like with Realism Overhaul. Removed the old steering logic and replaced it with a nice auto-tuning system. ( https://github.com/KSP-KOS/KOS/pull/1118 )
* SteeringManager structure to let users tweak parts of the new steering system ( http://ksp-kos.github.io/KOS_DOC/structures/misc/steeringmanager.html )
* PIDLoop structure to let users see parts of the new steering system, and to let them use the built-in PID system for their own needs ( http://ksp-kos.github.io/KOS_DOC/structures/misc/pidloop.html  )
* String manipulation methods. ( http://ksp-kos.github.io/KOS_DOC/structures/misc/string.html )
* New Lexicon (Associateive Array) type. ( http://ksp-kos.github.io/KOS_DOC/structures/misc/lexicon.html )
* New Science Experiment control interface allows scripts to directly execute science experiments, bypassing the on-screen prompts. ( http://ksp-kos.github.io/KOS_DOC/structures/vessels/scienceexperiment.html )
* CrewMember API to let you query the registered crew - their class, gender, and skill ( http://ksp-kos.github.io/KOS_DOC/structures/vessels/crewmember.html )
* Infernal Robotics API now lets you get Part containing a servo ( https://github.com/KSP-KOS/KOS/issues/1103 )
* (user docs) Better tutorial for KSP 1.0 areo mode. ( https://github.com/KSP-KOS/KOS/pull/1081 )
* A few more constants: C, ATMTOKPA, KPATOATM. ( http://ksp-kos.github.io/KOS_DOC/math/basic.html )
* DYNAMICPRESSURE, or Q ( https://github.com/KSP-KOS/KOS/pull/1085 )
* DEFINED keyword ( http://ksp-kos.github.io/KOS_DOC/language/variables.html#defined )
* Load and Pack Distance manipulation ( http://ksp-kos.github.io/KOS_DOC/structures/misc/loaddistance.html )
* KUniverse structure letting you break the 4th wall and revert from a script ( http://ksp-kos.github.io/KOS_DOC/structures/misc/kuniverse.html )
* Added SolarPrimeVector to provide universal longitude direction ( http://ksp-kos.github.io/KOS_DOC/bindings.html#solarprimevector )

### BUG FIXES
* Made `stage:liquidfuel` more sane. ( https://github.com/KSP-KOS/KOS/issues/513 )
* LIST BODIES returned unusuable structure type ( https://github.com/KSP-KOS/KOS/issues/1090 )
* Made "ORBIT" and alias for "OBT" and visa versa ( https://github.com/KSP-KOS/KOS/issues/1089 )
* Made vecdraws stop showing bogus atmospheric burning effects ( https://github.com/KSP-KOS/KOS/pull/1108 )
* Removed non-functional broken attempts to save/restore variables ( https://github.com/KSP-KOS/KOS/issues/1098 )
* KSM files didn't store relative jumps right, breaking short-circuit boolean logic ( https://github.com/KSP-KOS/KOS/issues/1137 )
* (user docs) many minor docs fixes.
* Lock throttle inside a FROM loop was broken ( https://github.com/KSP-KOS/KOS/issues/1117 )
* Unlock anything inside a Trigger body was broken ( https://github.com/KSP-KOS/KOS/issues/1151 )
* Replaced KSP's incorrect ground speed with our own calculation ( https://github.com/KSP-KOS/KOS/issues/1097 )
* SASMODE "radialin" and "raidialout" were swapped in the KSP API ( https://github.com/KSP-KOS/KOS/issues/1130 )

* Bug with remote tech allowing access without antenna in one case ( https://github.com/KSP-KOS/KOS/pull/1171 )
* Wheelsteering by integer compass heading was broken ( https://github.com/KSP-KOS/KOS/issues/1141 )
* SHUTDOWN didn't shut down immediately ( https://github.com/KSP-KOS/KOS/issues/1120 )
* Remote Tech delay, and the `wait` command, were ignoring the time warp multiplier ( https://github.com/KSP-KOS/KOS/issues/723 )
* Better detection of arg/param matching.  ( https://github.com/KSP-KOS/KOS/issues/1107 )
* Doing PRINT AT that runs offscreen threw an error ( https://github.com/KSP-KOS/KOS/issues/813 )

# v0.17.3

1.0.4 Release
-----------

### BREAKING CHANGES
* Removed all `ETA_` and `ALT_` bindings, please use `ETA:` and `ALT:` instead
* `TRUEANOMALY` and `MEANANOMALYATEPOCH` are now expressed in degrees to conform to our policy
* Deprecated INCOMMRANGE - now throws an exception with instructions to use the new addons:rt methods.
* Updated maxtthrust and availablethrust calculations for KSP v1.0.x.  Due to the way KSP handles thrust, neither available thrust nor maxthrust values are constant at all altitudes around bodies with atmospheres.
* Boot files are now stored on local hard drives with their original names.  You may get or set the boot file name using CORE:BOOTFILENAME suffix.
* Some undocumented and nonsensical bool math operations have been removed
* The Steering deadzone is much smaller now, this will allow for every precise RCS maneuvers.

### New Hotness
* You can now point RemoteTech antenna directly from script
* You can now get RemoteTech's 'local control' status
* Infernal Robotics integration improvements
* New loop structure to allow for more flexible iteration
* New struct object `CORE:` to interact with the currently running processor.
* Added vessel:dockingports and vessel:elements suffixes.
* Added element:dockingports and element:vessel suffixes.
* Added availablethrust suffix to engines which mirrors the availablethrust suffix for vessels.
* Added maxthrustat, availablethrustat, and ispat suffixes to engines to read the values at specified atmoshperic pressures.  See the documentation for details.
* Added maxthrustat and availablethrustat suffixes to vessels to read the values at a specified atmospheric pressures.  See the documentation for details.
* You can now use bootfiles while "Start on Archive volume" is enabled
* Many new sound effects have been added (error, beep, and an option for key click)
* Boolean AND and OR operations can now short circuit
* Add new WARPTO command that uses the new KSP function
* Added new `BODY:SOIRADIUS`
* Added new suffixes to part that lets you get the bare names of events, actions, and modules
* Many new sound effects have been added (error, beep, and an option for key click)
* Added `CLEARVECDRAWS` that will remove all VECDRAWS
* Any floating point value that has no floating component will be converted to an integer

### Old and busted
* Fixed empty return statements crashing with an argument count exception #934
* Fix setting vector:mag to a new value actually setting the magnitude to 1 #952
* Fix electricity being consumed while the game was paused #526
* Fix Part Resource string representation #1062
* Fix UNLOCK inside brace statements #1048 #1051
* Fix setting PHYSICS warp mode #989
* Fix printing engine list duplication #1026, #1057
* Fix terminal lockout when RemoteTech has no connection to the KSC, but the ship has local control.
* Fixed a crappy parser error that was causing `,` to do bizarre things to some code #925
* Fix running an empty program resetting the parent #858
* Fix some error printing related to nodes #905
* Fix kOS processor sinking into launch pad #980
* Fix `rename file` command #971
* Fix `return` statement breaking closure #923
* Fix docking port query #937
* better expression support inside square brackets #935
* you can now `LOCK` in a loop #954
* the kOS toolbar button should be better behaved now
* Volume indexes will truncate floating values rather than throwing an error
* `LIST FILES IN` syntax now works for archive
* electricity consumption is better behaved
* setting the target to an empty string will always unset target

# v0.17.2

1.0 Release
-----------

### New Hotness

* New infernal robotics integration
* Better error reporting


### Old and busted

* fixes keyword lexxing

# v0.17.1

Corrections and omissions
-------------------------

### "New" features

* Due to erendrake's inability to correctly use git. The new list constructor was omitted from the 0.17.0 release binaries.

### Bug Fixes:

* Many Doc fixes
* Fixed bug with setting KAC Alarm action to correct value
* Fixed some unneeded log spamming


# v0.17.0

FUNCTIONS! FUNCTIONS! FUNCTIONS!
--------------------------------
Big feature: You can make your own user-defined functions, that
can handle recursion, and can use local variable scoping.  You can
build a library of your own function calls and load them into your
script.

**New Documentation change page**:

For those users who just want to see what new features
exist without reading the entire documentation again
from scratch, we have created a changes page in the main documentation:

* New Changes Page: http://ksp-kos.github.io/KOS_DOC/changes.html

For the features mentioned below, you can go to the page above
and get a more verbose description of the new features.

### New Features:

A brief list of what's new:

* Variables can now be local
* Kerboscript has User Functions
* Community Examples Library
* LIST() now takes args to initialize the list.
* Physics Ticks not Update Ticks
* Ability to use SAS modes from KSP 0.90
* Blizzy ToolBar Support
* Ability to define colors using HSV
* Ability to highlight a part in color
* Better user interface for selecting boot scripts
* Disks can be made bigger with tweakable slider
* You Can Transfer Resources
* Kerbal Alarm Clock support
* Query the docked elements of a vessel
* Support for Action Groups Extended
* ISDEAD suffix for Vessel

This update is so full of new features that instead of describing all of their
details here, you can go see them on the main docs page at the following link:

http://ksp-kos.github.io/KOS_DOC/changes.html

### Bug Fixes:

- Using the same FOR iterator in two loops no longer name clashes because it's not global anymore.
- Repaired a number of boot file selection bugs.
- Removed a few unnecessary debug log spamming message.
- Fixed a minor issue with the special hidden file .DS_Store that Macs insert into the Scripts folder.
- Fixed bug spamming nullrefs when panel was open in the VAB/SPH editor.
- Fixed bugs where setting warp could crash KSP. Now it clamps warp to valid values.
- Fixed bug where kOS CPU's were drawing power from the batteries even when the game was paused.
- Fixed bug where rate of power consumption varied depending on animation frame rate.
- Fixed bug where WAIT 0 crashed scripts.  Now WAIT 0 waits the min. possible time (1 physics tick).
- Fixed small order of operations problem with expressions containing unary operators like '-', '+', and 'not'.
- Fixed problem where SET TARGET didn't really set it until the next physics tick.  Now it sets immediately.
- Fixed some issues with the use of Action Groups above 10, when Action Groups Extended is installed.
- Fixed bug where VOLUME:RENAMABLE returned the name string, rather than a boolean.
- Fixed bun when printing a VOLUME to the screen and failing to "stringify" it properly.
- Using the unary negation '-' on vectors and directions now works.
- Fixed some major bugs in how the kOS toolbar panel was dealing with scene changes and getting "stuck" on screen.
- Fixed some bugs with the kos Name Tag typing window getting stuck on screen and locking the user out of the UI.
- Fixed bug with reboot not clearing out the state properly.
- Fixed bug where any syntax error caught by the compiler resulted in bogus additional second error message.

###BREAKING:

- **RECOMPILE YOUR KSM FILES!!!** - If you used the COMPILE command in
  the past, changes to the kOS machine code that were needed to support
  variable scoping ended up invalidating any existing compiled KSM files.

- **KSM FILES ARE BIGGER** - compiled KSM files are now larger than
  they used to be for the same source code.  They might not be an
  efficient way to pack your code down to a small disk footprint
  anymore.

- *CONFIG:IPU should be slightly increased*  The new default
  we ship with is 200, to reflect both the change in ML code, and the
  movement to Unity's FixedUpdate for physics ticks.  However if you
  have played kOS in the past, your settings don't get automatically
  overwritten.  You will need to change the setting manually.

- *DECLARE has a new syntax*
  DECLARE _VARNAME_ now requires an initializer syntax as follows:
  - DECLARE _VARNAME_ TO _VALUE_.
  If you leave the TO _VALUE_ off, it will now be a syntax error.
  Also, you can say LOCAL or GLOBAL instead of, or in addition to,
  the word DECLARE.

- *DECLAREd variables are now local*
  Using the DECLARE _VARNAME_ TO _VALUE_ statement now causes the
  variable to have local scope that only exists within the local block
  of curly braces ('{'...'}') that it was declared inside of. To get
  the old behavior you can explicitly say:
  DECLARE GLOBAL _VARNAME_ to _VALUE.

- *FOR iterator now is local*
  The _VARIABLE_ in loops of the form FOR _VARIABLE_ IN _SOMELIST_ now
  has local scope to just that loop, meaning it stops existing after
  the loop is done and you can't use it outside the loop's body.


# v0.16.2

##HOTFIX

* Fixes #609 KOS ignores run command in FOR loop
* Fixes #610 Print AT draws in the wrong place on telnet after clearscreen.
* Fixes #612 doesn't update telnet screen when cur command is longer than prev and you up-arrow

# v0.16.1

##HOTFIX

this fixes #603 the mess that I made of the Node structure, thanks Tabris from the forums for bringing this to our attention.

# v0.16.0

### BREAKING
* Body:ANGULARVEL is now a Vector instead of a Direction.  (This is the same as the change that was done to Vessel:ANGULARVEL in v0.15.4, but we missed the fact that Body had the same problem).  It was pretty useless before so this shouldn't hurt many scripters :)
* Both Body:ANGULARVEL and Vessel:ANGULARVEL now are expressed in the same SHIP_RAW coordinate system as everything else in kOS, rather than in their own private weirdly mirrored reference frame. (Thanks to forum user @thegreatgonz for finding the problem and the fix)
* #536 the 1.5m kOS part has always had trouble with clipping into other parts due to the rim of the cylinder sticking up past the attachment points. The part definition has been changed to fix this, but in KSP new part definitions don't affect vessels that have already been built or have already had their design saved in a craft file in the VAB/SPH.  To see the fix you'll need to start a new vessel design from scratch, otherwise you'll still have the old clipping behavior.

### New Features
* TELNET SERVER.  The biggest new feature this update is the introduction of a **telnet server** you can use to access the terminals in game.  For security, it's turned off by default, but you can enable it with the config radio button.  Full documentation on this new feature is at http://ksp-kos.github.io/KOS_DOC/general/telnet.html
	* Synopsis:
		* Telnet to 127.0.0.1, port 5410
		* Select CPU from welcome menu by typing a number and hitting Return.
		* Your telnet client is now a clone of that CPU's terminal window and can control it.
		* If you want to open it up to others to use (i.e. controlling your KSP game from a second computer),
		  you can use an ssh tunnel to access the local loopback address, or if you just want to throw
		  caution to the wind, you can tell it to stop using loopback and use your real IP address.
		  Be aware of the security risk if you choose this.
* Added HUDTEXT that lets you add text to the screen. Thanks @pgodd !
	* more information here: http://ksp-kos.github.io/KOS_DOC/commands/terminal.html#HUDTEXT
* #72 - Added STAGE:NUMBER and STAGE:READY to allow for staging very close together
* #522 - Added BODY:GEOPOSITIONOF and BODY:ALTITUDEOF for getting body-relative info about a 3D point in space.
* #524 and #523 - mission waypoints now have 3d positions
* In game Terminal is now resizable!  From a script with SET TERMINAL:WIDTH and SET TERMINAL:HEIGHT, or from dragging the lower-right corner of the GUI window.

### Bug Fixes
* Fixes #389 - LOCK STEERING broken for RCS-only (no torque) ships.
* Fixes #516 - kOSTags are now applied in the correct MM pass
* Fixes #541 - All BODY: suffixes should now work properly when the body is the Sun without crashing.
* Fixes #544 - Terminal subbuffer won't shrink when up-arrowing to a previous smaller command.
* Fixes #548 - If SHIP is not the same as ActiveVessel, then executing STAGE stages the wrong vessel.
* Fixes #581 - SHIP:CONTROL:PILOTFORE and SHIP:CONTROL:PILOTSTARBOARD are no longer inverted.
* Fixes #578 - renamed our use of RemoteTech2 to RemoteTech to follow their new naming.
* Fixes #427 - Stack now clears when interactive commands throw exceptions.  (no longer reports false stack traces).
* Fixes #409 - Delete no longer leaves file in memory.
* Fixes #172 - Lock states no longer persist through power cycling unit. Now they become default for unlocked state
   * Also #358, #362, #568
* Fixes #580 - RT "signal lost. waiting to re-aquire signal" check previously disallowed manned terminal use.  Now it only disables the terminal if the vessel is unmanned.
* Fixes #344 - KOSArgumentMismatchException reported wrong arg number (i.e. it would claim your 3rd argument is wrong when it's really your 1st argument).  Fixed.


# v0.15.6

### BREAKING
* PART:UID is now a string. This will only break you if you were doing math on UIDs?
* ELEMENT:PARTCOUNT was poorly named and duplicated by ELEMENT:PARTS:LENGTH so it was removed.

### New Features
* (AGX) Action Groups Extended Support! Thanks @SirDiazo
	* Getting or setting groups 11-250 should behave the same as the stock groups if you have AGX installed.
	* Groundwork is laid for getting parts and modules by the new action groups.
* Gimbals are now a well known module. providing read access to its state
* Added PART:GETMODULEBYINDEX(int). This is most useful when you have a part with the same module twice. Thanks @jwvanderbeck
* More documentation work. http://ksp-kos.github.io/KOS_DOC/

### Bug Fixes
* Fixes RemoteTech Integration
* Structures can now be correctly ==, <> and concatenated with +
* STAGE:RESOURCE[?]:CAPACITY is now spell correctly :P

# v0.15.5
The KSP 0.90 compatibility release.
(The full thematic following of KSP 0.90's new way of
thinking will come in a future version. This is just
to make sure everything works.)

###BREAKING CHANGES
* Now respects the limitations of [0.90 career mode upgrades](http://ksp-kos.github.io/KOS/general/career_limits.html), which may make a few features not work anymore in career mode until you get further progressed along in your building upgrades.

###New Stuff
* Thanks to a new dev team contributer Johann Goetz (@theodoregoetz on github), we have a new, much better and cleaner looking [documentation site](http://ksp-kos.github.io/KOS_DOC/)
* Better flight input handling to detect the pilot controls and keep them isolated.
* "plays nice" with other autopilots a bit better, using KSP 0.90's new autopiloting hooks.
* Ability to read [more data about a ship resource](TODO - Are these in the docs?  Put URL here if so.) TODO:  i.e. SingleResourceValue:FLOWMODE, for example - see PR #452)
* New [suffixes to handle directions better](http://ksp-kos.github.io/KOS/math/direction.html) as mentioned in [long detail in this video](https://www.youtube.com/watch?v=7byYiZZBBVc)
* Separate Dry Mass, Wet Mass, and Current Mass readings for parts and for the vessel as a whole (TODO: Link here, but the public gh-pages hasn't be regenned yet so I don't know the link yet)
* Added new [WAYPOINT object](http://ksp-kos.github.io/KOS/structures/waypoint.html) to help with locations of some contracts.
* Added new :POSITION and :ALTITUDEPOSITION suffixes to [Geocoordinates](http://ksp-kos.github.io/KOS/math/geocoordinates.html) to obtain 3D vectors of their positions in ship-raw coordinate space.

* ADDED muliple new ways to deal with resources.
	* STAGE:RESOURCES, SHIP:RESOURCES and TARGET:RESOURCES will let you get a list of the resources for the craft, the difference being that SHIP: and TARGET: includes all resources and STAGE: includes only the resoures that are for "this stage". All three of these will let you get a list of :PARTS that can contain that resource.
	* Part resources now gives you access to the resource's tweakable :ENABLE and :TOGGLEABLE can let you remove add a resource to the normal resource flow.

###Bug Fixes
* Better handling of range checking and loading the boot file when remotetech is installed (thanks to hvacengi for this contribution)
* Boot file overwrite fix (thanks to pakrym)
* (For developers) fixed compile error on UNIX platforms that was due to filename case-sensitivity differences.
* LOG command to the Archive now appends to the file properly instead of rewriting the entire contents each time just to tack on one line.  It is now possible to read its output from outside KSP using a tool like the UNIX "tail -f" program.
* Better calculations of stage resource values, using SQUAD'S provided API for it instead of trying to walk the tree ourselves (which broke in 0.90).
* Fixed lonstanding [bug with geocoordinates:TERRAINHEIGHT](https://github.com/KSP-KOS/KOS/issues/478)

###Small maintenence issues
* Bundling a newer version of ModuleManager
* Better use of the "skin" system for the app panel.  Should see no obvious effect on the surface.


# v0.15.4
###BREAKING CHANGES
* Issue #431: SHIP:ANGULARMOMENTUM and SHIP:ANGULARVEL have been changed from directions to vectors to me more consistant with their nature

#### New Stuff:
* Should now be compatible with KSP 0.90

#### Bug Fixes:
* Issue #421: some local files are corrupt
* Issue #423: its possible to create a file with no extension
* Issue #424: additional bootfile suffix protection
* Issue #429: files sent to persistence file no longer get truncated


# v0.15.3
BugFixes:
* Issue #417: No error message on nonexistent function.
* Issue #413: Name tag window should get keyboard focus when invoked.
* Issue #405: Equality operator is broken for Wrapped structure objects.
* Issue #393: Files on local volume do not persist through save/load.

# v0.15.2

BugFixes:
* :MODULESNAMED returns something useful now #392
* array syntax bugs #387
* Added :PORTFACING to docking ports that should always have the correct facing for the port itself #398
* BREAKING: Partfacing should now come out of the top rather than the side #394

# v0.15.1

BugFixes:
* All Lists have suffixes again
* in the config panel, IPU no longer gets stuck at 50


# v0.15

## NEW FEATURES:

Please follow the links to see the full information on the new features.

* [Added new kOS GUI panel to the KSP Applauncher system](http://ksp-kos.github.io/KOS_DOC/summary_topics/applauncher_panel/index.html).  With this you can alter config values, and open/close terminals from one common panel.  Just click the little kOS logo button in either the editors (VAB/SPH) or in flight view.

* [Added pilot input to flight controls](http://ksp-kos.github.io/KOS_DOC/structure/control/index.html#pilot-commands) which lets you read/write the users control state, you can use this to set the exit behavior for the mainthrottle.

* Several suffixes are now [methods that you can call](ksp-kos.github.io/KOS_DOC/#structure_methods) with arguments.
	* eg before to add to a list it was SET LIST:ADD TO "FOO". Now it would be LIST:ADD("FOO").

* Suffix methods that perform an action do not need to be assigned to anything.  No more having to say *SET DUMMY TO MYLIST:CLEAR.*  You can now just say *MYLIST:CLEAR.* like it was a statement.

* Added suffixes to OBT for [walking orbit conic patches](http://ksp-kos.github.io/KOS_DOC/structure/orbit/index.html)
	* ORB:HASNEXTPATCH - A boolean that shows the presence of a future patch
	* ORB:NEXTPATCH - The next OBT patch

* Added better techniques for selecting the Part you want from a Vessel:
  * Ability to give any part any name you like with the [new nametag feature](http://ksp-kos.github.io/KOS_DOC/summary_topics/nametag/index.html).
  * [Directly querying a vessel for parts](http://ksp-kos.github.io/KOS_DOC/summary_topics/ship_parts_and_modules/index.html#parts), searching for [nametags](http://ksp-kos.github.io/KOS_DOC/summary_topics/nametag/index.html), or part names or part titles.
	* SHIP:PARTSDUBBED(string)
	* SHIP:PARTSNAMED(string)
	* SHIP:PARTSTAGGED(string)
	* SHIP:PARTSTITLED(string)
	* SHIP:PARTSINGROUP(string)
	* SHIP:MODULESNAMED(string)
  * [Walking the parts Tree](http://ksp-kos.github.io/KOS_DOC/structure/part/index.html):
	* PART:CHILDREN - A ListValue of parts that are descendant from the current part
	* PART:PARENT - A PART that is the ancestor of the current part
	* PART:HASPARENT - A boolean that shows the presence of a Parent PART
	* SHIP:ROOTPART - The first part of a ship.  The start of the tree of parts.  identical to SHIP:PARTS[0].
  * *SET MyList TO SHIP:PARTS.* now does the same thing as *LIST PARTS IN MyList.*

* A [new system lets you access the PartModules](http://ksp-kos.github.io/KOS_DOC/structure/partmodule/index.html) that the stock game and modders put on the various parts.  Through this, you now have the ability to manipulate a lot of the things that are on the rightclick menus of parts:
  * PART Suffixes:
	* GETMODULE(string)
	* ALLMODULES.
  * PartModule Suffixes:
	* GETFIELD(field_name) - read a value from a rightclick menu
	* SETFIELD(field_name, new value) - change a value on a rightclick menu, if it would normally be adjustable via a tweakable control.
	* DOACTION(name_of_action_) - cause one of the actions that would normally be available to action groups *even if it hasn't been assigned to an action group*.
	* DOEVENT(event_name) - "presses a button" on the rightclick part menu.
	* Several others..

* [Lists are now saner to work with](http://ksp-kos.github.io/KOS_DOC/structure/list/index.html) with no longer needing to use weird side effects to get things done, now that there's proper methods available:
  * :ADD has changed:
	* Old Way: *SET MyList:ADD TO NewVal.*
	* New Way: *MyList:ADD(NewVal).*
  * :REMOVE has changed:
	* Old Way: *SET MyList:REMOVE TO indexnumber.*
	* New Way: *MyList:REMOVE(indexnumber).*
  * :CLEAR has changed:
	* Old Way: *SET Dummy to MyList:CLEAR.*
	* New Way: *MyList:CLEAR().*

* Added ENGINE:AVAILABLETHRUST suffix. A value that respects the thrust limiter

* Added SHIP:AVAILABLETHRUST suffix. A sum of all of the ship's thrust that respects thrust limiters

* Added a [new experimental COMPILE command](http://ksp-kos.github.io/KOS_DOC/command/file/index.html#compile-1-to-2), for making smaller executable-only programs to put on your probes without punishing you for writing legible code with comments and indenting.

* [Filename convention changes](http://ksp-kos.github.io/KOS_DOC/command/file/index.html#volume-and-filename-arguments):
  * Commands that deal with filenames will now allow any arbitrary expressions as the filename, except for the RUN command.
  * *Exception*: The above does NOT apply to the ```RUN``` command.  The run command requires that the filenames are known at compile time, and it cannot allow arbitrary expressions evaluated later at runtime.
  * Program files are now called *.ks instead of *.txt.  When you first run the new version, it will give you an option to rename your files to the new name for you as they are moved to the new location.
  * Although the default script filenames use *.ks, you can override this and explicitly mention filename extensions in all the filename commands.  What you can't do is have filenames with no extensions.

* Added support for CKAN

* Added config file so kOS will now show up in Automatic Version Checker (AVC)

## CHANGES BREAKING OLD SCRIPTS:

* BREAKING: **.txt files are now .ks files**: The new assumed default file extension for files used by kOS is *.ks rather than *.txt.  This may confuse old IDE's, or your computer's assumed file associations.
* BREAKING: **VesselName**: Removed previously deprecated term "VESSELNAME", use "SHIPNAME"
* BREAKING: **SHIP:ORB:PATCHES**: Moved SHIP:ORB:PATCHES to SHIP:PATCHES and it now contains all orbit patches
* BREAKING: **Lists**: New syntax for using :ADD, and :REMOVE suffixes for lists requires old code to be altered.  See features above for the new way.
* WARNING: **Bundled ModuleManager**: Because kOS now needs ModuleManager, and ModuleMangaer complains about being run on Windows 64bit, you now see a new warning message if you run kOS on Windows 64bit, but the message is ignorable and kOS still runs.
* BREAKING: **identifiers as filenames**: If you use the same name for a filename as the name of a variable, in a file command such as COPY, DELETE, etc, then kOS will no longer use the variable's name as the filename but will now use the variable's contents as the filename.

## BUG FIXES:

* [PartValue:POSITION not using ship-relative coords](https://github.com/KSP-KOS/KOS/issues/277)
* [Boot file name is case sensitive](https://github.com/KSP-KOS/KOS/issues/311)
* [Engine reports maxthrust on :ISP suffix](https://github.com/KSP-KOS/KOS/issues/331)
* [LIST VOLUMES IN <list> makes an empty list.](https://github.com/KSP-KOS/KOS/issues/308)
* [Parser doesn't understand THING[num]:THING[NUM] or thing[index]:suffix(arg)](https://github.com/KSP-KOS/KOS/issues/268)
* [ship:obt:patches seems to be missing some of the patches](https://github.com/KSP-KOS/KOS/issues/252)  (Note: in addition to fixing it, the patches list was moved to just ship:patches, which makes more sense).
* [Compiler should throw exception on trying to put a WAIT in a trigger.](https://github.com/KSP-KOS/KOS/issues/254)


- - -


## V0.14.2

* Added entry cost to kOS parts
* extended button lockout to include the full throttle button (default z)
* updated the reference to RemoteTech rather than RemoteTech2

## V0.14.1

* Kerbal Space Program 0.25 Support
* OnSave and OnLoad should no longer disappear your craft. This is a bit of a stop-gap fix that doesn't guarantee that the kOS part will be happy, but at least your craft will still be there.
* Resolves #257 by unbinding the X key from throttle cutoff while the window is in focus.
* KSP AVC Support
* Added Body:RotationalPeroid

# v0.14

### New Hotness
* Updated fonts, Thanks @MrOnak
* Now runtime errors show source location and call stack trace (Github issues #186 and #210).  Example:
~~~
	Tried To push Infinity into the stack.
	At MyProgramFile2 on Archive, line 12
		PRINT var1/var2.
				  ^
	Called from MyProgramFile1 on Archive, line 213
	RUN MyProgramFIle2("hello").
	^
	Called from StartMission on Archive, line 2.
	RUN MyProgramFile1.
	^
	_
~~~
* (WHEN and ON) Triggers that are taking longer than an Update is meant to take, and thus can freeze KSP are caught and reported (Github issue #104).  Gives the user an explanatory message about the problem.
  * WARNING: Because of a change that had to be done for this, it is **_Highly_ recommended that you increase your *InstructionsPerUpdate* setting in config.xml to 150% as much** as it was before (i.e. from 100 to 150, or if it was 200, make it 300.).
* Multiple Terminal Windows - possible to have one open per CPU part.  (Github issue #158)

![Multiple Windows!](https://github.com/KSP-KOS/KOS/blob/master/Docs/Images/MultiEdit.png)

### Old and Busted ( now fixed )
* "rename" was deleting files instead of moving them. (Github issue #220).
* Was parsing array index brakets "[..]" incorrectly when they were on the lefthand side of an assignment.  (Github issue #219)
* SHIP:SENSORS were reading the wrong ship's sensors sometimes in multi-ship scenarios.  (GIthub issue #218 )
* Integer and Floating point numbers were not quite properly interchangable like they were meant to be. (Github issue #209)


# v0.13.1
* Fixed an issue with Dependancies that kept kOS modules from registering

# v0.13

## MAJOR
* BREAKING: Commrange has more or less been removed from stock kOS, we realized that most of the behavior of it was copied by other mods and was invisible to users
* BREAKING: All direction references are now relative to the controlling part, not the vessel, this will only break on vessels there these two directions are not the same.
* BREAKING: Direction:Vector will always return a unit vector.
* BREAKING: Body:Velocity now returns a <a href="http://ksp-kos.github.io/KOS_DOC/structure/orbitablevelocity/">pair of orbit/surface velocities</a> just like Vessel:Velocity does. (previously it returned just the orbit velocity as a single vector.)
* BREAKING: Direction*Vector now returns the rotated Vector, and vectors can be rotated with DIRECTION suffix.
* BREAKING: DOCKINGPORT:DOCKEDVESSELNAME is not DOCKINGPORT:DOCKEDSHIPNAME
* SHIP:APOAPSIS and SHIP:PERIAPSIS are deprecated for removal later, you can find them both under SHIP:OBT
* SHIP:VESSELNAME is deprecated for later removal, use SHIP:NAME or SHIPNAME


## New Features
* Added the ability to get and set the current timewarp "Mode" either RAILS or PHYSICS
* Added Boot files that will run when you get to the pad automatically, you select which one will run in the VAB thanks @WazWaz
* <a href="http://ksp-kos.github.io/KOS_DOC/structure/vessel/">Vessels</a> and <a href="http://ksp-kos.github.io/KOS_DOC/structure/body/">Bodies</a> now <a href="http://ksp-kos.github.io/KOS_DOC/structure/orbitable/">can be used interchangeably as much as possible.</a>
* Three new prediction routines for <a href="http://ksp-kos.github.io/KOS_DOC/command/prediction/"> finding state of an object at a future time: </a>
* POSITIONAT( Object, Time ).
* VELOCITYAT( Object, Time ).
* ORBITATAT( Object, Time ).
* you can now get the FACING of all parts.
* ITERATOR:END is now split into :NEXT and :ATEND
* Direction can now always return a proper vector.
	* IE SHIP:FACING returned V(0,0,0) before
* Added a 3d Drawing tool for letting you draw lines and labels.
	* Tour: https://www.youtube.com/watch?v=Vn6lUozVUHA
* Added a new and improved file editor so the edit command actually works again in game!
* Added the ability to switch to MapView and back in code
* ACTIVESHIP alias links to the ship that is currently under user direct control
* added GEOPOSITION suffixes BODY and TERRAINHEIGHT


## Known Issues
* due to issues with the new version of RemoteTech, you will always have a connection available for use with kOS.

## Fixes
* if you have a target and attempt to set a new target and that fails, you would no longer have a target
* increased power requirement of the kOS Module
* Bodies are now targetable
* MAXTHRUST no longer includes flamed out engines
* resource floating values are now truncated to 2 significant digits to match the game UI and behavior
* files saved to the local volume maintain their linebreaks
* radar altimiter now returns a double
* fixed an issues where setting some controls blocked the rest.
* allow empty bodies on {} blocks
* locks called from another lock are not correctly recognized
* Neutralizing the controls will clear the values of all controls.
* fixed node initialization
* Better resource processing
* LIST:COPY returns a kOS type that you can actually use
* ORBIT:TRANSITION returns a string type that you can actually use.
* Comments in code dont cause data loss on load/save

# v0.12.1

BREAKING: DOCKINGPORT:ORIENTATION is now DOCKINGPORT:FACING

* Fixed Terminal linewrap @ the bottom of the terminal
* Fixed "Revert to Launch" button, it was blowing up the world and not allowing control before
* Fixed LOCK s in subprograms
* Fixed RemoteTech integration blowing up everything
* Fixed flight controls not releasing when they should
* Disabled RemoteTech Integration while RT development is stalled
* Fix exception when trying to type a multiline instruction in the interpreter
* srfprograde is available as a new shortcut
* BODY now has an OBT suffix
* Parts now have a SHIP suffix
* You can now work with your target if that target is a docking port
* Added a new PRESERVE keyword for repeating a trigger.
* all active triggers are removed when a script is finished.

# v0.12.0

* the aforementioned new parser by @marianoapp with all of its speed improvements and other goodies.
* There's a config SpecialValue that can be used to control how some of the mod features work, like setting the execution speed, the integration with RT2 and starting from the archive volume.
* The terminal screen can be scrolled using PageUp and PageDown
* Negative numbers/expressions can be written starting with a minus sign, so no more "0-..."
* Added ELSE syntax!
* Added ELSE syntax!
* Added NOT syntax!!
* Added List square brackets [] as list subelement accessor
* you can use variables as arguments for PRINT AT statements

This version adds a new 0.625m part. Thanks to SMA on this neat new addition.
* it works as a kOS computer core
* has 5000 units of code space
* as a smaller part it is unlocked with "precision engineering" in career mode.
* also has a light that will be controllable before the actual release


Bug fixes
* Cannot "set" a variable that later will become a "lock" #13
* Sanitize values sent to KSP #14
* Strange order of operations: "and" seems to evaluate before ">" #20
* moved some names back to "kOS"
* Work on some structure's ToString return.
* Parameters now get passed in the correct order
* Ship resources no longer generate an error if they arent present
* Ctrl+C now interrupts correctly once again.
* ETA:TRANSITION returns the correct time.
* Better handling of types.

# v0.11.1

* BREAKING: Disk Space is now defined by the kOS part, existing missions might have the available space reduced to 500. (whaaw)
* BREAKING: Vector * Vector Operator has been changed from V( X * X, Y * Y, Z * Z) to a dot product.

* Added Ctrl+Shift+X hotkey to close the terminal window (jwvanderbeck)
* Improved RemoteTech integration (jwvanderbeck) Current state is discussed https://github.com/erendrake/KOS/pull/51
* Added engine stats to the enginevalue
	* ACTIVE (get/set)
	* ALLOWRESTART (get
	* ALLOWSHUTDOWN (get)
	* THROTTLELOCK (get)
	* THRUSTLIMIT (get/set)

* Added to BODY:ATM:SEALEVELPRESSURE
* Added a DockingPort Part Type, You can access it by "LIST DOCKINGPORTS IN ..."
* Added PART:CONTROLFROM which centers the transform on that part.

* Vector now has two new Suffixes
	* NORMALIZED - Vector keeps same direction, but will have a magnitude of 1.
	* SQRMAGNITUDE - https://docs.unity3d.com/Documentation/ScriptReference/Vector3-sqrMagnitude.html

* New math operators involving Vectors
	* VECTORCROSSPRODUCT (VCRS)
	* VECTORDOTPRODUCT (VDOT)
	* VECTOREXCLUDE (VXCL) - projects one vector onto another
	* VECTORANGLE (VANG) - Returns the angle in degrees between from and to.

* Direct control of vessel and nearby vessels (SHIP:CONTROL, TARGET:CONTROL)
	* __GETTERS__
	* YAW - Rotation (1 to -1)
	* PITCH - Rotation (1 to -1)
	* ROLL - Rotation (1 to -1)
	* FORE - Translation (1 to -1)
	* STARBOARD - Translation (1 to -1)
	* TOP - Translation (1 to -1)
	* ROTATION - Vector
	* TRANSLATION - Vector
	* NEUTRAL - bool,
	* MAINTHROTTLE (1 to -1)
	* WHEELTHROTTLE (1 to -1)
	* WHEELSTEER (1 to -1)
	* __SETTERS__
	* YAW - Rotation (1 to -1)
	* PITCH - Rotation (1 to -1)
	* ROLL - Rotation (1 to -1)
	* FORE - Translation (1 to -1)
	* STARBOARD - Translation (1 to -1)
	* TOP - Translation (1 to -1)
	* ROTATION - Vector
	* TRANSLATION - Vector
	* NEUTRALIZE - bool, releases vessel control,
	* MAINTHROTTLE (1 to -1)
	* WHEELTHROTTLE (1 to -1)
	* WHEELSTEER (1 to -1)
* changing systems vessel load distance
	* LOADDISTANCE get/set for adjusting load distance for every vessel
	* VESSELTARGET:LOAD bool - is the vessel loaded
	* VESSELTARGET:PACKDISTANCE - Setter for pack distance for every vessel.
* Added RANDOM() generator (0 - 1)

* Power requirements are now directly tied to the active volume's size, the ARCHIVE's size is unlimited so it is capped at the equivalent of 50KB.

### 0.11.0

- Thanks to enkido and jwvanderbeck for your help.

- BREAKING: BODY, SHIP:BODY, TARGET:BODY now all return a Body structure rather than the name of the body
- BREAKING: Removed NODE:APOAPSIS and NODE:PERIAPSIS. They are now available in NODE:ORBIT:APOAPSIS

- Basic RemoveTech Intergration
- Added VOLUME:NAME to getting the current volume
- Lists can now be populated with basic data that you can loop over or index [Full Info](/wiki/List/)
	- Bodies (eg Kerbin, Mun, Duna)
	- Targets - All Vessels other than current
	- Engines - Engines on the craft
	- Resources - All Ship Resources
	- Parts - All Ship Parts (slow)
	- Sensors - (eg Pres, Grav, Accel)
	- Elements - All flights connected to the active vessel
- A Lot of bug fixes and refactoring
- Constants (eg G, E, PI) are now retrieved using CONSTANT() rather than spreadout.
- Commands resolve in order of descending specificity, rather than in the pseudorandom order they were in before
- Added Math operators LN, LOG10, MIN, MAX.

### 0.10.0

- Compatible with KSP 0.23 Thanks to Logris and MaHuJa for Commits
- Added List() which creates a collection and the following commands
	- ADD - Adds the value of any variable
	- CONTAINS - Tests and returns if the value exists in the list
	- REMOVE - removes the item from the list if the list contains the item
	- LENGTH - returns a count of the items in the list
	- COPY - creates a copy of the list
	- You can also index into a list with # (ie LIST#1 gives you the second item in the list).
- Added the following stats
	- OBT:PERIOD - http://en.wikipedia.org/wiki/Orbital_period
	- OBT:INCLINATION - http://en.wikipedia.org/wiki/Orbital_inclination
	- OBT:ECCENTRICITY - http://en.wikipedia.org/wiki/Orbital_eccentricity
	- OBT:SEMIMAJORAXIS - http://en.wikipedia.org/wiki/Semi-major_axis
	- OBT:SEMIMINORAXIS - http://en.wikipedia.org/wiki/Semi-major_axis
	- VOLUME:NAME - Name of the current Volume
	- ETA:TRANSITION - Seconds until next patch
	- OBT:TRANSITION - Type of next patch: possibilities are
		- FINAL
		- ENCOUNTER
		- ESCAPE
		- MANEUVER
- Adding a few BODY members
	- RADIUS
	- MU - G * Body Mass
	- G - Gravitational Constant
	- ATM atmosphere info with sub elements
		- EXISTS
		- HASOXYGEN
		- SCALE
		- HEIGHT

- Added ORBIT to NODE
- Added the following commands
	- UNSET #VARIABLE - remove the variable, ALL removes all variables Thanks a1070
	- FOR #USERVARIABLE IN #LIST takes a list and loops over it, exposing each item in the collection as a user defined variable
- New close window action binding
- Performance fixes

### 0.9.2

- Fixed a bug where you couldn't have an IF or WHEN inside an UNTIL
- Added INLIGHT

### 0.9.1

- Fixed a bug where AND and OR wouldn't work on boolean values

### 0.9

- New expression system added
- Version Info can be grabbed programatically
- Targeting of bodies, getting stats
- Get values from other ships, just like current ship
- Send commands to chutes, legs and solar panels individually
- Round function to x decimals, modulo
- Setting values on structures
- Vectors now use double precision
- Get apoapsis and periapsis of a node

### 0.85

- Problems adding R() structures to headings
- Some commands wouldn't allow dashes in filenames
- Editor X-scrolling bug fixed
- Delimeter matcher would cause error inside comments
- Editor was crashing when you hit end
- Error messages wouldn't have line numbers if the error occurred inside curly braces.
- For nodes, :DELTAV:MAG now works correctly. Note that :DELTAV is now effectively the same as :BURNVECTOR
- You can now use right & left arrows to edit lines in immediate mode
- There is now a LEGS binding for landing legs, use it just like SAS or GEAR
- Same with CHUTES for parachutes

### 0.82

- Couldn't switch to a volume using it's number
- All curly braces were horribly horribly broken

### 0.8

- Maneuver node support added
- Time structure added
- Programs that call other programs no longer give multiple "Program ended" messages

### 0.7

- Official release of mod interoperability
- New altitude radar system
- Improved whitespace support

### 0.65

- Trigonometry Functions ARCSIN, ARCCOS, ARCTAN and ARCTAN2 are now implemented.
- Programs can now contain parameters
- You can now get the distance to an arbitrary latlng with :distance
- Cpu clock speed has been raised from 1 to 5.
- Variables now persist
- Ranges have been increased

### 0.6

- Simplified steering system
- Support for driving your rover wheels to a specific location
- Support for surface vectors
- Rovers can be steered towards arbitrary geo coordinates
- Preliminary support for 3rd party mod integration (Still testing)
- Bug fixes

### 0.5

- Rover bindings
- Range limits for archive drive
- Bug fixes
- Trig functions
- ABS function

### 0.45

- Fix for the matching on boolean operators

### 0.44

- Support for AND and OR in IF statements added
- Fix for nested IF statements

### 0.43

- Fixes locking and waiting for compound values

### 0.42

- Fixes handling of compound values in certain circumstances

### 0.41

- Fixes a bug dealing with the Archive folder

### 0.4

- Bug fixes
- Interact with subelements of R()
- Targetting
- Radar altitude
- Plaintext editing
- Toggle terminal & power from action group
- Maximum thurst variable
- Surface speed variable

### 0.35

- Fix for the typing when not focussed bug
- Fix for file renaming not checking for previous existing file
- Resource tags re-enabled
- Support for limited sub-expressions within R() expressions

### 0.34

- Added non-qwerty keyboard support
- Fix for laggy systems dropping typed characters

### 0.33

- Fix for the case of texture paths that would break on linux

### 0.32

- Implemented a friendly message that your textures are missing

### 0.31

- Fixed an issue that would kill saves
- Added support for maneuver nodes from Ehren Murdick's code
- Added stage:solidfuel from Pierre Adam's code

### 0.3

- Support for loops
- Support for the IF statement
- Support for the BREAK statement

### 0.2

- Initial public release!
- Execution system redesigned to be more heirarchical
- Added support for compound statements
- Successful test of synchronized orbiting

### 0.1

- First trip to orbit successfully done
- Terminal created
- KerboScript designed and implemented
- VAB Part created
- Flight stats and bindings created
