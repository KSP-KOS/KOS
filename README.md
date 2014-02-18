kOS Mod Overview
================

kOS is a scriptable autopilot Mod for Kerbal Space Program. It allows you write small programs that automate specific tasks. 

Installation
------------

Like other mods, simply merge the contents of the zip file into your Kerbal Space Program folder.

Usage
-----

Add the Compotronix SCS part to your vessel; it’s under the “Control” category in the Vehicle Assembly Building or Space Plane Hanger. After hitting launch, you can right-click on the part and select the “Open Terminal” option. This will give you access to the KerboScript interface where you can begin issuing commands and writing programs.

KerboScript
===========

KerboScript is a programming language that is derived from the language of planet Kerbin, which _sounds_ like gibberish to non-native speakers but for some reason is _written_ exactly like English. As a result, KerboScript is very English-like in its syntax. For example, it uses periods as statement terminators.

The language is designed to be easily accessible to novice programmers, therefore it is case-insensitive, and types are cast automatically whenever possible.

A typical command in KerboScript might look like this:

    PRINT “Hello World”.

Expressions
-----------

KerboScript uses an expression evaluation system that allows you to perform math operations on variables. Some variables are defined by you. Others are defined by the system. 

There are three basic types:

### Numbers

You can use mathematical operations on numbers, like this:

    SET X TO 4 + 2.5. 
    PRINT X.             // Outputs 6.5

The system follows the order of operations, but currently the implementation is imperfect. For example, multiplication will always be performed before division, regardless of the order they come in. This will be fixed in a future release.

## Mathematical Functions
    
### Basic Functions

    ABS(1).             // Returns absolute value of input. e.g. 1
    CEILING(1.887).     // Rounds up to the nearest whole number. e.g. 2
    FLOOR(1.887).       // Rounds down to the nearest whole number. e.g. 1
    LN(5)               // Gives the natural log of the provided number
    LOG10(5)            // Gives the log base 10 of the provided number
    MOD(21,6).          // Returns remainder of an integer division. e.g. 3
    MIN(0,100).         // Returns The lower of the two e.g. 0
    MAX(0,100).         // Returns The higher of the two e.g. 100
    ROUND(1.887).       // Rounds to the nearest whole number. e.g. 2
    ROUND(1.887, 2).    // Rounds to the nearest place value. e.g. 1.89
    SQRT(7.89).         // Returns square root. e.g. 2.80891438103763
    
### Trigonometric Functions

    SIN(6).                 // Returns sine of input. e.g. 0.10452846326
    COS(6).                 // Returns cosine. e.g. 0.99452189536
    TAN(6).                 // Returns tangent. e.g. 0.10510423526
    ARCSIN(0.67).           // Returns angle whose sine is input in degrees. e.g. 42.0670648
    ARCCOS(0.67).           // Returns angle whose cosine is input in degrees. e.g. 47.9329352
    ARCTAN(0.67).           // Returns angle whose tangent is input in degrees. e.g. 33.8220852
    ARCTAN2(0.67, 0.89).    // Returns the angle whose tangent is the quotient of two specified numbers in degrees. e.g. 36.9727625

### Strings

Strings are pieces of text that are generally meant to be printed to the screen. For example:

    PRINT “Hello World!”.

To concatenate strings, you can use the + operator. This works with mixtures of numbers and strings as well.

    PRINT “4 plus 3 is: “ + (4+3).

### Directions

Directions exist primarily to enable automated steering. You can initialize a direction using a vector or a rotation.

    SET Direction TO V(0,1,0).         // Set a direction by vector
    SET Direction TO R(0,90,0).        // Set by a rotation in degrees
 
You can use math operations on Directions as well. The next example uses a rotation of “UP” which is a system variable describing a vector directly away from the celestial body you are under the influence of.

    SET Direction TO UP + R(0,-45,0).  // Set direction 45 degress west of “UP”.

Command Reference
-----------------

### ADD

Adds a maneuver node to the flight plan. 

Example: 
This statement adds a node that occurs 30 seconds from now, and has a delta-V of 100 m/s radial out, 0 m/s normal, and 200 m/s prograde.

    ADD NODE(TIME + 30, 100, 0, 200).
    
### REMOVE
    
Removes maneuver node from flight plan. Cannot remove bare nodes e.g. ADD NODE().

    SET X TO NODE(0,0,0,0).
    ADD X.
    REMOVE X.
    
    ADD NODE(0,0,0,0).
    REMOVE.             // Does not remove node.

### BREAK

Breaks out of a loop.
Example:

    SET X TO 1.
    UNTIL 0 {
        SET X TO X + 1.
        IF X > 10 { BREAK. }.       // Exits the loop when X is greater than 10
    }.

### CLEARSCREEN

Clears the screen and places the cursor at the top left.
Example:

    CLEARSCREEN.

### COPY

Copies a file to or from another volume. Volumes can be referenced by their ID numbers or their names if they’ve been given one. See LIST, SWITCH and RENAME.
Example:

    SWITCH TO 1.       // Makes volume 1 the active volume
    COPY file1 FROM 0. // Copies a file called file1 from volume 0 to volume 1
    COPY file2 TO 0.   // Copies a file called file1 from volume 1 to volume 0

## DELETE

Deletes a file. You can delete a file from the current volume, or from a named volume.
Example:

    DELETE file1.         // Deletes file1 from the active volume.
    DELETE file1 FROM 1.  // Deletes file1 from volume 1

Deleting files from ARCHIVE will delete your scripts from your hard drive.  Be sure to have back ups.

### DECLARE

Declares a variable at the current context level. Alternatively, a variable can be implicitly declared by a SET or LOCK statement.
Example:

    DECLARE X.
    
### DECLARE PARAMETER
    
Declares variables to be used as a parameter.
Example:
    
    DECLARE PARAMETER X.
    DECLARE PARAMETER X,y.
    RUN MYPROG(X).

### EDIT

Edits a program on the currently selected volume.
Example:

    EDIT filename.

### IF

Checks if the expression supplied returns true. If it does, IF executes the following command block.
Example:

    SET X TO 1.
    IF X = 1 { PRINT "X equals one.". }.            // Prints "X equals one."
    IF X > 10 { PRINT "X is greater than ten.". }.  // Does nothing
    
If statements can make use of boolean operators.
Example:

    IF X = 1 AND Y > 4 { PRINT "Both conditions are true". }.
    IF X = 1 OR Y > 4 { PRINT "At least one condition is true". }.

### LISTS

If you want to make a collection of values, this is for you. [Full Documentation](http://github.com/erendrake/KOS/wiki/List)
    
    SET FOO TO LIST().   // Creates a new list in FOO variable
    SET FOO:ADD TO 5.    // Adds a new element to the end of the list
    PRINT FOO:LENGTH.    // Prints 3
    FOO:CLEAR.           // Removes all elements from the FOO list.

#### FOR

Lists need to be iterated over sometimes, to help with this we have FOR.

    SET FOO TO LIST().   // Creates a new list in FOO variable
    SET FOO:ADD TO 5.    // Adds a new element to the end of the list
    SET FOO:ADD TO ALTITUDE. // eg 10000
    SET FOO:ADD TO ETA:APOAPSIS. // eg 30 

    FOR BAR IN FOO { PRINT BAR. }. // Prints 5, then 10000, then 30
    PRINT BAR. // ERROR, BAR doesn't exist outside the for statement

#### Built-in Lists

Builds a list of various resources and saves them to a variable.

    EXAMPLE:
    LIST ENGINES IN FOO // Creats a list of the currently active engines and puts it in the FOO variable

#### printout Lists

Outputs data to the console. Lists files by default.
Example:

    EXAMPLE:
    LIST.           // Lists files on the active volume
    LIST ENGINES.   // List of engines

### LOCK

Locks a variable to an expression. On each cycle, the target variable will be freshly updated with the latest value from expression.
Example:

    SET X TO 1.
    LOCK Y TO X + 2.
    PRINT Y.       // Outputs 3
    SET X TO 4.
    PRINT Y.      // Outputs 6

### ON

Awaits a change in a boolean variable, then runs the selected command. This command is best used to listen for action group activations.
Example:

    ON AG3 PRINT “Action Group 3 Activated!”.
    ON SAS PRINT “SAS system has been toggled”.

### PRINT

Prints the selected text to the screen. Can print strings, or the result of an expression.
Example:

    PRINT “Hello”.
    PRINT 4+1.
    PRINT “4 times 8 is: “ + (4*8).

### PRINT.. AT (COLUMN,LINE)
    
Prints the selected text to the screen at specified location. Can print strings, or the result of an expression.
Example:
    
    PRINT “Hello” at (0,10).
    PRINT 4+1 at (0,10).
    PRINT “4 times 8 is: “ + (4*8) at (0,10).
    
### LOG.. TO
    
Logs the selected text to a file on the local volume. Can print strings, or the result of an expression.
Example:
    
    LOG “Hello” to mylog.
    LOG 4+1 to mylog .
    LOG “4 times 8 is: “ + (4*8) to mylog.
    
### RENAME

Renames a file or volume.
Example:

    RENAME VOLUME 1 TO AwesomeDisk
    RENAME FILE MyFile TO AutoLaunch.
    
### REMOVE

Removes a maneuver node.
Example:

    REMOVE NEXTNODE.        // Removes the first maneuver node in the flight plan.

### RUN

Runs the specified file as a program.
Example:

    RUN AutoLaunch.

### SET.. TO

Sets the value of a variable. Declares the variable if it doesn’t already exist.
Example:

    SET X TO 1.

### STAGE

Executes the stage action on the current vessel.
Example:

    STAGE.

### SWITCH TO

Switches to the specified volume. Volumes can be specified by number, or it’s name (if it has one). See LIST and RENAME.
Example:

    SWITCH TO 0.                        // Switch to volume 0.
    RENAME VOLUME 1 TO AwesomeDisk.     // Name volume 1 as AwesomeDisk.
    SWITCH TO AwesomeDisk.              // Switch to volume 1.
    PRINT VOLUME:NAME.                  // Prints "AwesomeDisk".

### TOGGLE

Toggles a variable between true or false. If the variable in question starts out as a number, it will be converted to a boolean and then toggled. This is useful for setting action groups, which are activated whenever their values are inverted.
Example:

    TOGGLE AG1.			// Fires action group 1.
    TOGGLE SAS.			// Toggles SAS on or off.

### UNLOCK

Releases a lock on a variable. See LOCK.
Examples:

    UNLOCK X.                // Releases a lock on variable X.
    UNLOCK ALL.              // Releases ALL locks.
    
### UNTIL

Performs a loop until a certain condition is met.
Example:

    SET X to 1.
    UNTIL X > 10 {          // Prints the numbers 1-10.
        PRINT X.
        SET X to X + 1.
    }.

### WAIT

Halts execution for a specified amount of time, or until a specific set of criteria are met. Note that running a WAIT UNTIL statement can hang the machine forever if the criteria are never met.
Examples:

    WAIT 6.2.                     // Wait 6.2 seconds.
    WAIT UNTIL X > 40.            // Wait until X becomes greater than 40.
    WAIT UNTIL APOAPSIS > 150000. // You can see where this is going.
    
### WHEN.. THEN

Executes a command when a certain criteria are met. Unlike WAIT, WHEN does not halt execution.
Example:

    WHEN BCount < 99 THEN PRINT BCount + “ bottles of beer on the wall”.

### ..ON

Sets a variable to true. This is useful for the RCS and SAS bindings.
Example:

    RCS ON 			// Turns on the RCS

### ..OFF

Sets a variable to false. This is useful for the RCS and SAS bindings.
Example

    RCS OFF			// Turns off the RCS
    
### WARP

Sets game warp to provided value(0-7).

    SET WARP TO 5.      // Sets warp to 1000x.
    SET WARP TO 0.      // Sets warp to 0x aka real time.
    
### REBOOT

Reboots the kOS module.

### SHUTDOWN

Causes kOS module to shutdown.

Flight Statistics
=================

You can get several useful vessel stats for your ships 

    ALTITUDE
    ALT:RADAR           // Your radar altitude
    BODY                // The current celestial body whose influence you are under
    MISSIONTIME         // The current mission time
    VELOCITY            // The current orbital velocity
    VERTICALSPEED
    SURFACESPEED
    LATITUDE
    LONGITUDE
    STATUS              // Current situation: LANDED, SPLASHED, PRELAUNCH, FLYING, SUB_ORBITAL, ORBITING, ESCAPING, or DOCKED
    INLIGHT          // Returns true if not blocked by celestial body, always false without solar panel.
    INCOMMRANGE         // returns true if in range
    COMMRANGE           // returns commrange
    MASS
    MAXTHRUST           // Combined thrust of active engines at full throttle (kN)
    VESSELNAME

Examples:
set mylatitude to LATITUDE.

    
### TIME

Returns time in various formats.

    TIME                // Gets the current universal time
    TIME:CLOCK          // Universal time in H:M:S format(1:50:26)
    TIME:CALENDAR       // Year 1, day 134
    TIME:YEAR           // 1
    TIME:DAY            // 134
    TIME:HOUR           // 1
    TIME:MINUTE         // 50
    TIME:SECOND         // 26
    TIME:SECONDS          // Total Seconds
    
### Vectors

These return a vector object, which can be used in conjuction with the LOCK command to set your vessel's steering.

    PROGRADE
    RETROGRADE
    UP				// Directly away from current body


### Orbit geometry values

These values can be polled either for their altitude, or the vessel's ETA in reaching them. By default, altitude is returned. 

    APOAPSIS			// Altitude of apoapsis
    ALT:APOAPSIS		// Altitude of apoapsis
    PERIAPSIS			// Altitude of periapsis
    ALT:PERIAPSIS		// Altitude of periapsis
    ETA:APOAPSIS		// ETA to apoapsis
    ETA:PERIAPSIS		// ETA to periapsis
    
### Maneuver nodes

    NODE                // Direction of next maneuver node, can be used with LOCK STEERING
    ETA:NODE            // ETA to active maneuver node
    ENCOUNTER           // Returns celestial body of encounter
    NEXTNODE            // Next node in flight plan.

### Resources

### Resource Types

    LIQUIDFUEL
    OXIDIZER
    ELECTRICCHARGE
    MONOPROPELLANT
    INTAKEAIR
    SOLIDFUEL

### Stage specific values

    STAGE:LIQUIDFUEL            // Prints the available fuel for all active engines.
    STAGE:OXIDIZER
    
### Global values

    PRINT <LiquidFuel>.                         // Print the total liquid fuel in all tanks. DEPRECATED
    PRINT SHIP:LIQUIDFUEL.                      // Print the total liquid fuel in all tanks.
    PRINT VESSEL("kerbRoller2"):LIQUIDFUEL.     // Print the total liquid fuel on kerbRoller2.
    PRINT TARGET:LIQUIDFUEL.                    // Print the total liquid fuel on target.


Flight Control
==============

These values can be SET, TOGGLED, or LOCKED. Some values such as THROTTLE and STEERING explicity require the use of lock.

### Controls which use ON and OFF

    SAS				// For these five, use ON and OFF, example: SAS ON. RCS OFF.
    GEAR
    RCS
    LIGHTS
    BRAKES
    LEGS
    CHUTES	// Cannot be un-deployed.
    PANELS
    
### Controls that can be used with TOGGLE

    ABORT
    AGX             // Where x = 1 through 10. Use toggle, example: TOGGLE AG1.             	

### Controls that must be used with LOCK

    THROTTLE			// Lock to a decimal value between 0 and 1.
    STEERING			// Lock to a direction.
    WHEELTHROTTLE       // Seperate throttle for wheels
    WHEELSTEERING       // Seperate steering system for wheels

Examples: 
lock throttle to 1.  //sets throttle to 100%
lock throttle to 0.5.  //sets throttle to 50%
lock wheelsteering to 180.  //turns wheeleld vessel to point south
lock steering to heading 45 by 0.  //turns the vessel to face north-east and lowers the nose to pitch 0 (horizon)

    
Structures
==========

Structures are variables that can contain more than one piece of information. Structures can be used with SET.. TO just like any other variable. Changing valves works only with V() and NODE() at this time, cannot be used with lock.

Their subelements can be accessed by using : along with the name of the subelement.

### LATLNG (latitude, longitude)

Represents a set of geo-coordinates.

    SET X TO LATLNG(10, 20).            // Initialize point at lattitude 10, longitude 20
    PRINT X:LAT.                        // Print 10.
    PRINT X:LNG.                        // Print 20.
    PRINT X:DISTANCE.                   // Print distance from vessel to x (same altitude is presumed)
    PRINT LATLNG(10,20):HEADING.        // Print the heading to the point.
    PRINT X:BEARING.                    // Print the heading to the point relative to vessel heading.

### NODE (universalTime, radialOut, normal, prograde)

Represents a maneuver node.[Full Documentation](/wiki/Node)

    SET X TO NODE(TIME+60, 0, 0, 100).  // Creates a node 60 seconds from now with
                                        // prograde=100 m/s
    ADD X.                              // Adds the node to the flight plan.
    PRINT X:PROGRADE.                   // Returns 100.
    PRINT X:ETA.                        // Returns the ETA to the node.
    PRINT X:DELTAV                      // Returns delta-v vector.
    REMOVE X.                           // Remove node  from the flight plan.
    
    SET X TO NODE(0, 0, 0, 0).          // Create a blank node.
    ADD X.                              // Add Node to flight plan.
    SET X:PROGRADE to 500.              // Set nodes prograde to 500m/s deltav.
    SET X:ETA to 30.              // Set nodes time to 30 seconds from now.
    PRINT X:OBT:APOAPSIS.                   // Returns nodes apoapsis.
    PRINT X:OBT:PERIAPSIS.                  // Returns nodes periapsis.
    
    
### HEADING (degreesFromNorth, pitchAboveHorizon)

Represents a heading that's relative to the body of influence.

    SET X TO HEADING(45, 10).           // Create a rotation facing northeast, 10 degrees above horizon

### R (pitch, yaw, roll)

Represents a rotation.

    SET X TO PROGRADE + R(90,0,0).      // Initializes a direction to prograde plus a relative pitch of 90
    LOCK STEERING TO X.                 // Steer the vessel in the direction suggested by direction X.
    
### V (x, y, z)

Represents a vector.

    SET varname TO V(100,5,0).          // initializes a vector with x=100, y=5, z=0
    varname:X.                          // Returns 100.
    V(100,5,0):Y.                       // Returns 5.
    V(100,5,0):Z.                       // Returns 0.
    varname:MAG.                        // Returns the magnitude of the vector, in this case
    SET varname:X TO 111.               // Changes vector x value to 111.
    SET varname:MAG to 10.              // Changes magnitude of vector. e.g. V(9.98987,0.44999,0)
    
### VESSELS

All craft share a datastructure [Full Documentation](/wiki/Vessel)
                    

#### VESSEL (vesselname)

Represents a targetable vessel

    SET X TO VESSEL("kerbRoller2").     // Initialize a reference to a vessel.
    PRINT X:DISTANCE.                   // Print distance from current vessel to target.
    PRINT X:HEADING.                    // Print the heading to the vessel.
    PRINT X:BEARING.                    // Print the heading to the target vessel relative to vessel heading.
    
#### SHIP
    
Represents currently selected ship
    
    PRINT SHIP.                            // returns VESSEL("kerbRoller2")
    PRINT SHIP:DISTANCE.                   // Print distance from current vessel to target.
    PRINT SHIP:HEADING.                    // Print the heading to the vessel.
    PRINT SHIP:BEARING.                    // Print the heading to the target vessel relative to vessel heading.
    
#### TARGET

Represents targeted vessel or celestial body

    SET TARGET TO "kerbRoller2".        // target kerbRoller2
    PRINT TARGET:DISTANCE.              // Print distance from current vessel to target.
    PRINT TARGET:HEADING.               // Print the heading to the target vessel.
    PRINT TARGET:BEARING.               // Print the bearing to the target vessel relative to vessel heading.
    
System Variables
==========================
Returns values about kOS and hardware

    PRINT VERSION.            // Returns operating system version number. 0.8.6
    PRINT VERSION:MAJOR.      // Returns major version number. e.g. 0
    PRINT VERSION:MINOR.      // Returns minor version number. e.g. 8
    PRINT SESSIONTIME.        // Returns amount of time, in seconds, from vessel load.
