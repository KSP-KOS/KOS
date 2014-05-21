kOS Mod Changelog
=================

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
