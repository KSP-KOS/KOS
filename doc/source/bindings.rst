.. _bindings:

Catalog of Bound Variable Names
===============================

This is the list of special reserved keyword variable names that kOS
will interpret
to mean something special. If they are used as normal variable names by
your kOS script
program they may not work. Understanding them and their meaning is
crucial to creating
effective kOS scripts.

NAMED VESSELS AND BODIES
------------------------

SHIP:

| **Variable name**: SHIP
| **Gettable**: yes
| **Settable**: no
| **Type**: `Vessel <structures/vessels/vessel.html>`__
| **Description**: Whichever vessel happens to be the one containing the CPU part that is running this Kerboscript code at the moment. This is the `CPU Vessel <general/cpu_vessel.html>`__.
 
TARGET:

| **Variable Name**: TARGET
| **Gettable**: yes
| **Settable**: yes
| **Type**: `Vessel <structures/vessels/vessel.html>`__ or `Body <structures/celestial_bodies/body.html>`__ 
| **Description**: Whichever `Orbitable <structures/orbits/orbitable.html>`__ object happens to be the one selected as the current KSP target. If set to a string, it will assume the string is the name of a vessel being targetted and set it to a vessel by that name. For best results set it to Body("some name") or Vessel("some name") explicitly.

Alias shortcuts for SHIP fields
-------------------------------

The following are all alias shortcuts for accessing the fields of the
SHIP vessel.
To see their definition, please consult the
`Vessel <structures/vessels/vessel.html>`__
page, as they are all just instances of the standard vessel suffixes.

================ ==============================================================================
Variable         Same as
================ ==============================================================================
HEADING          Same as SHIP:HEADING
PROGRADE         Same as SHIP:PROGRADE
RETROGRADE       Same as SHIP:RETROGRADE
FACING           Same as SHIP:FACING
MAXTHRUST        Same as SHIP:MAXTHRUST
VELOCITY         Same as SHIP:VELOCITY
GEOPOSITION      Same as SHIP:GEOPOSITION
LATITUDE         Same as SHIP:LATITUDE
LONGITUDE        Same as SHIP:LONGITUDE
UP               Same as SHIP:UP
NORTH            Same as SHIP:NORTH
BODY             Same as SHIP:BODY
ANGULARMOMENTUM  Same as SHIP:ANGULARMOMENTUM
ANGULARVEL       Same as SHIP:ANGULARVEL
ANGULARVELOCITY  Same as SHIP:ANGULARVEL
COMMRANGE        Same as SHIP:COMMRANGE
MASS             Same as SHIP:MASS
VERTICALSPEED    Same as SHIP:VERTICALSPEED
SURFACESPEED     Same as SHIP:SURFACESPEED
AIRSPEED         Same as SHIP:AIRSPEED
VESSELNAME       Same as SHIP:VESSELNAME
ALTITUDE         Same as SHIP:ALTITUDE
APOAPSIS         Same as SHIP:APOAPSIS
PERIAPSIS        Same as SHIP:PERIAPSIS
SENSORS          Same as SHIP:SENSORS
SRFPROGRADE      Same as SHIP:SRFPROGRADE
SRFREROGRADE     Same as SHIP:SRFREROGRADE
OBT              Same as SHIP:OBT
STATUS           Same as SHIP:STATUS
VESSELNAME       Same as SHIP:NAME
================ ==============================================================================

Resource Types
--------------

Any time there is a resource on the ship it can be queried. The
resources are the values that appear when you click on the upper-right
corner of the screen in the KSP window. |Resources|

::

    LIQUIDFUEL
    OXIDIZER
    ELECTRICCHARGE
    MONOPROPELLANT
    INTAKEAIR
    SOLIDFUEL

All of the above resources can be queried using either the prefix SHIP
or STAGE, depending on whether you are trying to query how much is left
in the current stage or the entire ship:

How much liquid fuel is left in the entire ship:

::

    PRINT "There is " + SHIP:LIQUIDFUEL + " liquid fuel on the ship.".

How much liquid fuel is left in just the current stage:

::

    PRINT "There is " + STAGE:LIQUIDFUEL + " liquid fuel in this stage.".

How much liquid fuel is left in the target vessel:

::

    PRINT "There is " + TARGET:LIQUIDFUEL + " liquid fuel in the target ship.".

Any other resources that you have added using other mods should be
query-able this way, provided that you spell
the term exactly as it appears in the resources window.

You can also get a list of all resources, either in SHIP: or STAGE: with the :RESOURCES suffix. 

ALT ALIAS
---------

The special variable ALT is a unique exception. It behaves like a
structure with suffixes but it's actually a bit "fake" in that it's not
really a structure. The following terms are just exceptions that don't
fit anywhere else:

============== ======== ==========
Variable       Type      Meaning
============== ======== ==========
ALT:APOAPSIS   number   The altitude of the apoapsis of the current ship.  Identical to SHIP:APOAPSIS.
ALT:PERIAPSIS  number   The altitude of the periapsis of the current ship.  Identical to SHIP:PERIAPSIS.
ALT:RADAR      number   The altitude of the current ship above the terrain.  Does not have an alias anywhere.
============== ======== ==========

ETA ALIAS
---------

The special variable ETA is a unique exception. It behaves like a
structure with suffixes but it's actually a bit "fake" in that it's not
really a structure. The following terms are just exceptions that don't
fit anywhere else:

============== ======== ==========
Variable       Type      Meaning
============== ======== ==========
ETA:APOAPSIS   number   seconds until SHIP will reach its apoapsis.
ETA:PERIAPSIS  number   seconds until SHIP will reach its periapsis.
ETA:TRANSITION number   seconds until SHIP will leave its SOI to enter the SOI of another body.
============== ======== ==========

ENCOUNTER
---------

The body being encountered next by the current vessel. Returns the
special string "None" if there is no expected encounter, or an object of
type `Body <structures/celestial_bodies/body.html>`__ if an encounter is
expected.

BOOLEAN TOGGLE FIELDS:
----------------------

These are variables that behave like boolean flags. They can be True or
False, and can be set or toggled
using the "ON" and "OFF" and "TOGGLE" commands.
Many of these are for action group flags.
**NOTE ABOUT ACTION GROUP FLAGS:** If the boolean flag is for an action
group, be aware that each time the
user presses the action group keypress, it *toggles* the action group,
so you might need to check for both
the change in state from false to true AND the change in state from true
to false to see if the key was hit.

============== ==========   ========= ===============
Variable Name  Can Read     Can Set   Description
============== ==========   ========= ===============
SAS            yes          yes       (Same as "SAS" indicator on the navball.)
RCS            yes          yes       (Same as "RCS" indicator on the navball.)
GEAR           yes          yes       Is the GEAR enabled right now? (Note, KSP does some strange things with this flag, like needing to hit it twice the first time).
LEGS           yes          yes       Are the landing LEGS extended? (as opposed to GEAR which is for the wheels of a plane.)
CHUTES         yes          yes       Are the parachutes extended? (Treats all parachutes as one single unit. Does not activate them individually.)
LIGHTS         yes          yes       Are the lights on? (like the "U" key in manual flight.)
PANELS         yes          yes       Are the solar panels extended? (Treats all solar panels as one single unit. Does not activate them individually.)
BRAKES         yes          yes       Are the brakes on?
ABORT          yes          yes       Abort Action Group.
AG1            yes          yes       Action Group 1.
AG2            yes          yes       Action Group 2.
AG3            yes          yes       Action Group 3.
AG4            yes          yes       Action Group 4.
AG5            yes          yes       Action Group 5.
AG6            yes          yes       Action Group 6.
AG7            yes          yes       Action Group 7.
AG8            yes          yes       Action Group 8.
AG9            yes          yes       Action Group 9.
AG10           yes          yes       Action Group 10.
AGn            yes          yes       If you have the Action Groups Extended mod installed, you can access its groups the same way, i.e. AG11, AG12, AG13, etc.
============== ==========   ========= ===============

Flight Control
--------------

There are bound variables used in controlling the flight of a ship, which
can be found at the following links:

If you want to let kOS do a lot of the work of aligning to a desired
heading for you, use `Cooked Control <commands/flight/cooked.html>`__.

If you want your script to manipulate the controls directly (as in "set
yaw axis halfway left for a few seconds (using the 'A' key)", then
use `Raw Control <commands/flight/raw.html>`__.

If you want to be able to READ what the player is attempting to do
while your script is running, and perhaps respond to it, then use
`Reading the Pilot's Control settings (i.e reading what the manual input is attempting) <commands/flight/pilot.html>`__ 
(By default your script will override manual piloting attempts, but
you can read what the pilot's controls are set at and make your
autopilot take them under advisement - sort of like how a
fly-by-wire plane works.)


Controls that must be used with LOCK
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

    THROTTLE            // Lock to a decimal value between 0 and 1.
    STEERING            // Lock to a direction, either a Vector or a Direction.
    WHEELTHROTTLE       // Separate throttle for wheels
    WHEELSTEERING       // Separate steering system for wheels

System Variables
----------------

Returns values about kOS and hardware

::

    PRINT VERSION.            // Returns operating system version number. e.g. 0.8.6
    PRINT VERSION:MAJOR.      // Returns major version number. e.g. 0
    PRINT VERSION:MINOR.      // Returns minor version number. e.g. 8
    PRINT VERSION:BUILD.      // Returns build version number. e.g. 6
    PRINT SESSIONTIME.        // Returns amount of time, in seconds, from vessel load.

NOTE the following important difference:

SESSIONTIME is the time since the last time this vessel was loaded from
on-rails into full physics.

TIME is the time since the entire saved game campaign started, in the
kerbal universe's time. i.e. TIME = 0 means a brand new campaign was
just started.

WARPING
~~~~~~~

Time warp can be controlled with WARP, and WARPMODE.  See
:ref:`WARP <warp>`

Config
------

CONFIG is a special variable name that refers to the configuration
settings for
the kOS mod, and can be used to set or get various options.

`CONFIG has its own page <structures/misc/config.html>`__ for further
details.

Game State
----------

Variables that have something to do with the state of the universe.

========= ====================================== ==============
Variable  Type                                   Meaning
========= ====================================== ==============
TIME      `Time <structures/misc/time.html>`__   Simulated amount of time that passed since the beginning of the game's universe epoch. (A brand new campaign that just started begins at TIME zero.)
MAPVIEW    boolean                               Both settable and gettable. If you query MAPVIEW, it's true if on the map screen, and false if on the flight view screen.  If you SET MAPVIEW, you can cause the game to switch between mapview and flight view or visa versa.
========= ====================================== ==============

TIME is a useful system variable for calculating the passage of time
between taking
physical measurements (i.e. to calculate how fast a phenomenon is
changing in a loop).
It returns the KSP *simulated* time, rather than the actual realtime
sitting in the
chair playing the game. If everything is running smoothly on a fast
computer, one
second of simulated time will match one second of real time, but if
anything is
causing the game to stutter or lag a bit, then the simulated time will
be a bit
slower than the real time. For any script program trying to calculate
physical
properties of the KSP universe, the time that matters is the simulated
time, which
is what TIME returns.

It's important to be aware of the `frozen update
nature <general/CPU_hardware.html#FROZEN>`__ of the kOS
computer when reading TIME.

.. |Resources| image:: /_images/reference/bindings/resources.png
