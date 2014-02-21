## === Shared Structure ===

* DIRECTION - To Active Ship
* DISTANCE - To Active Ship
* BEARING - To Active Ship
* HEADING - To Active Ship
* PROGRADE - Direction
* RETROGRADE - Direction
* MAXTHRUST - Max thrust of all active engines
* VELOCITY - Structure 
    * ORBIT - Vector
    * SURFACE - Vector
* GEOPOSITION - Structure
    * LAT - double
    * LNG - double
    * DISTANCE - double
    * HEADING - float
    * BEARING - float
* LATITUDE - float
* LONGITUDE - float
* FACING - Direction
* UP - Direction
* NORTH - Direction
* BODY - [Body](Body)
* ANGULARMOMENTUM - Direction
* ANGULARVEL - Direction
* MASS 
* VERTICALSPEED
* SURFACESPEED
* AIRSPEED
* VESSELNAME
* ALTITUDE
* APOAPSIS
* PERIAPSIS
* SENSORS - Structure
    * ACC
    * PRES
    * TEMP
    * GRAV
* TERMVELOCITY
* OBT - Structure - [Orbit](Orbit)

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