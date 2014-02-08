If you want to make a collection of values, this is for you. Example
    
    SET FOO TO LIST().   // Creates a new list in FOO variable
    SET FOO:ADD TO 5.    // Adds a new element to the end of the list
    SET FOO:ADD TO ALTITUDE. 
    SET FOO:ADD TO ETA:APOAPSIS. 
    PRINT FOO:LENGTH.    // Prints 3
    SET FOO:REMOVE TO 1. // Removes the second element from the list
    SET BAR TO FOO:COPY. // Makes a copy of the FOO list
    FOO:CLEAR.           // Removes all elements from the FOO list.

### FOR

Lists need to be iterated over sometimes, to help with this we have FOR.

    SET FOO TO LIST().   // Creates a new list in FOO variable
    SET FOO:ADD TO 5.    // Adds a new element to the end of the list
    SET FOO:ADD TO ALTITUDE. // eg 10000
    SET FOO:ADD TO ETA:APOAPSIS. // eg 30 

    FOR BAR IN FOO { PRINT BAR. }. // Prints 5, then 10000, then 30
    PRINT BAR. // ERROR, BAR doesn't exist outside the for statement

### Built-in Lists

Builds a list of various resources and saves them to a variable.

* Bodies
* Targets
* Resources
* Parts
* Engines
* Sensors 
* Nodes

    EXAMPLE USAGE:
    LIST ENGINES IN FOO // Creats a list of the currently active engines and puts it in the FOO variable

    FOR BAR IN FOO {
        PRINT BAR:THRUST.
    }.

#### Bodies

All bodies in the system

* NAME
* DESCRIPTION
* MASS - Double
* POSITION - Vector
* ALTITUDE - Double
* APOAPSIS - Double
* PERIAPSIS - Double
* RADIUS - In Meters
* MU - Gravatational Parameter
* ATM
    * EXISTS - Bool
    * HASOXYGEN - bool
    * SCALE - double
    * HEIGHT - float
* VELOCITY  - Vector
* DISTANCE - From active vessel
* BODY - Parent Body

#### Engines

Each active engine holds the following data.
    
* MAXTHRUST
* THRUST - Current Trust
* FUELFLOW 
* ISP - Specific impulse
* FLAMEOUT - bool
* IGNITION - bool

#### Targets

* Many Properties, Left side, Handel it! TODO

#### Resources

Aggregate Resources of all parts

* NAME
* AMOUNT
* CAPACITY

#### Parts

This list can be long, use carefully 

* NAME
* STAGE
* UID 
* RESOURCES - List

#### Sensors

All Sensors on the craft (eg Pressure, Gravity, ect)

* TYPE
* ACTIVE - bool, can be set to activate/deactivate
* READOUT

#### Nodes

All docked flights for the current vessel. 

* NAME
* ID
* PARTCOUNT
* RESOURCES - List

### printout Lists

Outputs data to the console. Lists files by default.
Example:

    LIST.           // Lists files on the active volume
    LIST FILES.     // Lists files on the active volume
    LIST VOLUMES.   // Lists all volumes, with their numbers and names
    LIST BODIES.    // Lists celestial bodies and their distance
    LIST TARGETS.   // Lists target-able vessels in range
    LIST RESOURCES. // List of resources by stage
    LIST PARTS.     // Lists parts in vessel
    LIST ENGINES.   // List of engines
