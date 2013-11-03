kOS Mod Changelog
=================

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
