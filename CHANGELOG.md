kOS Mod Changelog
=================

# v0.15

** Major Changes this Release. **


TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO
---------------------------------------------------------
    Before actual release, go through
    here and change all the URL's in the
    links to use KOS_DOC instead of
    KOS_DOC_DEV!!
    They currently point to _DEV so that it's possible to test the
    links before the documention gets migrated.


## NEW FEATURES:

Please follow the links to see the full information on the new features.

* [Added new kOS GUI panel to the KSP Applauncher system](http://ksp-kos.github.io/KOS_DOC_DEV/summary_topics/applauncher_panel/index.html).  With this you can alter config values, and open/close terminals from one common panel.  Just click the little kOS logo button in either the editors (VAB/SPH) or in flight view.

* [Added PILOTMAINTHROTTLE](http://ksp-kos.github.io/KOS_DOC_DEV/structure/control/index.html#pilot-commands) which lets you read/write the users throttle, you can use this to set the exit behavior for the throttle.

* Several suffixes are now [methods that you can call](ksp-kos.github.io/KOS_DOC_DEV/#structure_methods) with arguments.

* Suffix methods that perform an action do not need to be assigned to anything.  No more having to say *SET DUMMY TO MYLIST:CLEAR.*  You can now just say *MYLIST:CLEAR.* like it was a statement.

* Added suffixes to OBT for [walking orbit conic patches](http://ksp-kos.github.io/KOS_DOC_DEV/structure/orbit/index.html)
    * ORB:HASNEXTPATCH - A boolean that shows the presence of a future patch
    * ORB:NEXTPATCH - The next OBT patch 

* Added better techniques for selecting the Part you want from a Vessel:
  * Ability to give any part any name you like with the [new nametag feature](http://ksp-kos.github.io/KOS_DOC_DEV/summary_topics/nametag/index.html).
  * [Directly querying a vessel for parts](http://ksp-kos.github.io/KOS_DOC_DEV/summary_topics/ship_parts_and_modules/index.html#parts), searching for [nametags](http://ksp-kos.github.io/KOS_DOC_DEV/summary_topics/nametag/index.html), or part names or part titles.
    * SHIP:PARTSDUBBED(string)
    * SHIP:PARTSNAMED(string)
    * SHIP:PARTSTAGGED(string)
    * SHIP:PARTSTITLED(string)
    * SHIP:PARTSINGROUP(string)
    * SHIP:MODULESNAMED(string)
  * [Walking the parts Tree](http://ksp-kos.github.io/KOS_DOC_DEV/structure/part/index.html):
    * PART:CHILDREN - A ListValue of parts that are descendant from the current part
    * PART:PARENT - A PART that is the ancestor of the current part
    * PART:HASPARENT - A boolean that shows the presence of a Parent PART
    * SHIP:ROOTPART - The first part of a ship.  The start of the tree of parts.  identical to SHIP:PARTS[0].
  * *SET MyList TO SHIP:PARTS.* now does the same thing as *LIST PARTS IN MyList.*

* A [new system lets you access the PartModules](http://ksp-kos.github.io/KOS_DOC_DEV/structure/partmodule/index.html) that the stock game and modders put on the various parts.  Through this, you now have the ability to manipulate a lot of the things that are on the rightclick menus of parts:
  * PART Suffixes:
    * GETMODULE(string)
    * ALLMODULES.
  * PartModule Suffixes:
    * GETFIELD(field_name) - read a value from a rightclick menu
    * SETFIELD(field_name, new value) - change a value on a rightclick menu, if it would normally be adjustable via a tweakable control.
    * DOACTION(name_of_action_) - cause one of the actions that would normally be available to action groups *even if it hasn't been assigned to an action group*.
    * DOEVENT(event_name) - "presses a button" on the rightclick part menu.
    * Several others..

* [Lists are now saner to work with](http://ksp-kos.github.io/KOS_DOC_DEV/structure/list/index.html) with no longer needing to use weird side effects to get things done, now that there's proper methods available:
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

* Added a [new experimental COMPILE command](http://ksp-kos.github.io/KOS_DOC_DEV/command/file/index.html#compile-1-to-2), for making smaller executable-only programs to put on your probes without punishing you for writing legible code with comments and indenting.

* [Filename convention changes](http://ksp-kos.github.io/KOS_DOC_DEV/command/file/index.html#volume-and-filename-arguments):
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


~~~~


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
