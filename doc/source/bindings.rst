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

- **Variable name**: SHIP
- **Gettable**: yes
- **Settable**: no
- **Type**: `Vessel <structures/vessels/vessel.html>`__
- **Description**: Whichever vessel happens to be the one containing the
  CPU part that is running this Kerboscript code at the moment. This is
  the `CPU Vessel <general/cpu_vessel.html>`__.

.. _target:

TARGET:

- **Variable Name**: TARGET
- **Gettable**: yes
- **Settable**: yes
- **Type**: `Vessel <structures/vessels/vessel.html>`__ or
  `Body <structures/celestial_bodies/body.html>`__ or
  `Part <structures/vessels/part.html>`__

- **Description**: Whichever `Orbitable <structures/orbits/orbitable.html>`__
  object happens to be the one selected as the current KSP target. If a
  docking port is selected as the target, it will be the corresponding part.
  If set to a string, it will assume the string is the name of a vessel being
  targeted and set it to a vessel by that name. For best results set it
  to Body("some name") or Vessel("some name") explicitly.  This will
  throw an exception if called from a vessel other than the active vessel,
  as limitations in how KSP sets the target vessel limit the
  implementation to working with only the active vessel.

.. _hastarget:

HASTARGET:

- **Variable Name**: TARGET
- **Gettable**: yes
- **Settable**: no
- **Type**: boolean
- **Description**: Will return true if the ship has a target selected.
  This will always return false when not on the active vessel, due to
  limitations in how KSP sets the target vessel.

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
MASS             Same as SHIP:MASS
VERTICALSPEED    Same as SHIP:VERTICALSPEED
GROUNDSPEED      Same as SHIP:GROUNDSPEED
SURFACESPEED     This has been obsoleted as of kOS 0.18.0.  Replace it with GROUNDSPEED.
AIRSPEED         Same as SHIP:AIRSPEED
ALTITUDE         Same as SHIP:ALTITUDE
APOAPSIS         Same as SHIP:APOAPSIS
PERIAPSIS        Same as SHIP:PERIAPSIS
SENSORS          Same as SHIP:SENSORS
SRFPROGRADE      Same as SHIP:SRFPROGRADE
SRFRETROGRADE    Same as SHIP:SRFRETROGRADE
OBT              Same as SHIP:OBT
STATUS           Same as SHIP:STATUS
SHIPNAME         Same as SHIP:NAME
================ ==============================================================================

Constants (pi, e, etc)
----------------------

Get-only.

The variable ``constant`` provides a way to access a few
:ref:`basic math and physics constants <constants>`, such as Pi, Euler's
number, and so on.

Example::

    print "Kerbin's circumference: " + (2*constant:pi*Kerbin:radius) + "meters.".

The full list is here: :ref:`constants page <constants>`.

Terminal
--------

Get-only. ``terminal`` returns a :struct:`terminal` structure describing
the attributes of the current terminal screen associated with the
CPU this script is running on.

Core
----

Get-only. ``core`` returns a :struct:`core` structure referring to the CPU you
are running on.

Archive
-------

Get-only. ``archive`` returns a :struct:`Volume` structure referring to the archive.
You can read more about what archive is on the :ref:`File & volumes <volumes>` page.

Stage
-----

Get-only. ``stage`` returns a :struct:`stage` structure used to count resources
in the current stage.  Not to be confused with the COMMAND stage
which triggers the next stage.

NextNode
--------

See the :global:`NEXTNODE` documentation.

HasNode
-------

See the :global:`HASNODE` documentation.

AllNodes
--------

See the :global:`ALLNODES` documentation.

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

.. |Resources| image:: /_images/reference/bindings/resources.png

ALT ALIAS
---------

The special variable `ALT <structures/vessels/alt.html>`__ gives you
access to a few altitude predictions:

ALT:APOAPSIS

ALT:PERIAPSIS

ALT:RADAR

Further details are found on the `ALT page <structures/vessels/alt.html>`__ .


ETA ALIAS
---------

The special variable :ref:`ETA <eta>` gives you
access to a few time predictions:

ETA:APOAPSIS

ETA:PERIAPSIS

ETA:NEXTNODE

ETA:TRANSITION

Further details are found on the :ref:`ETA page <eta>`.

ENCOUNTER
---------

The orbit patch describing the next encounter with a body the current
vessel will enter. If there is no such encounter coming, it will return
the special string "None".  If there is an encounter coming, it will
return an object :ref:`of type Orbit <orbit>`.  (i.e. to obtain the name
of the planet the encounter is with, you can do:
``print ENCOUNTER:BODY:NAME.``, for example.).

BOOLEAN TOGGLE FLAGS:
---------------------

These are special :struct:`Boolean` variables that interact with ship systems.
They can be ``True`` or ``False``, and can be set or toggled using the ``ON``,
``OFF``, and ``TOGGLE`` :ref:`commands <toggle>`.  Many of these are for stock
action groups, while others are specific to kOS.

.. seealso::

    :ref:`stock-boolean-flags`
        Stock action groups are independent of actual part state and must be
        toggled to have an effect.

    :ref:`kos-boolean-flags`
        Pseudo-action groups added by kOS which are dependent on actual part
        state and may still affect parts if set to the current value.


=============================  ==========   =========   ========= =============================================
Variable Name                   Can Read     Can Set     Source    What it manages
=============================  ==========   =========   ========= =============================================
:global:`SAS`                   yes          yes          stock     SAS action group
:global:`RCS`                   yes          yes          stock     RCS thrusters action group
:global:`GEAR`                  yes          yes          stock     Landing gear action group
:global:`LIGHTS`                yes          yes          stock     Lights action group
:global:`BRAKES`                yes          yes          stock     Brakes action group
:global:`ABORT`                 yes          yes          stock     Abort action group
:global:`LEGS`                  yes          yes          kOS       The extended state of all landing legs
:global:`CHUTES`                yes          yes          kOS       The armed state of all parachutes
:global:`CHUTESSAFE`            yes          yes          kOS       The armed state of all "safe" parachutes
:global:`PANELS`                yes          yes          kOS       The state of retractable solar panels
:global:`RADIATORS`             yes          yes          kOS       The deployed state of radiators
:global:`LADDERS`               yes          yes          kOS       The extended state of ladders
:global:`BAYS`                  yes          yes          kOS       The opened state of payload/service bays
:global:`INTAKES`               yes          yes          kOS       The opened state of all  intakes
:global:`DEPLOYDRILLS`          yes          yes          kOS       The deployment state of all drills
:global:`DRILLS`                yes          yes          kOS       The running state of all drills
:global:`FUELCELLS`             yes          yes          kOS       The running state of all fuel cells
:global:`ISRU`                  yes          yes          kOS       The running state of all resource converters
:any:`AG1 <AG1 ... AG10>`       yes          yes          stock     Action Group 1.
:any:`AG2 <AG1 ... AG10>`       yes          yes          stock     Action Group 2.
:any:`AG3 <AG1 ... AG10>`       yes          yes          stock     Action Group 3.
:any:`AG4 <AG1 ... AG10>`       yes          yes          stock     Action Group 4.
:any:`AG5 <AG1 ... AG10>`       yes          yes          stock     Action Group 5.
:any:`AG6 <AG1 ... AG10>`       yes          yes          stock     Action Group 6.
:any:`AG7 <AG1 ... AG10>`       yes          yes          stock     Action Group 7.
:any:`AG8 <AG1 ... AG10>`       yes          yes          stock     Action Group 8.
:any:`AG9 <AG1 ... AG10>`       yes          yes          stock     Action Group 9.
:any:`AG10 <AG1 ... AG10>`      yes          yes          stock     Action Group 10.
:ref:`AGn <AGX>`                yes          yes          AGX       ActionGroupsExtended action groups
=============================  ==========   =========   ========= =============================================

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

Time
----

MISSIONTIME
~~~~~~~~~~~~~~~~~~~

You can obtain the number of seconds it has been since the current
CPU vessel has been launched with the bound global variable
``MISSIONTIME``.  In real space programs this is referred to usually
as "MET" - Mission Elapsed Time, and it's what's being measured when
you hear that familiar voice saying "T minus 10 seconds..."  Point "T"
is the zero point of the mission elapsed time, and everything before that
is a negative number and everything after it is a positive number.
kOS is only capable of returning the "T+" times, not the "T-" times,
because it doesn't read your mind to know ahead of time when you plan
to launch.

Time Structure
~~~~~~~~~~~~~~

`Time <structures/misc/time.html>`__ is the simulated amount of time that passed since the beginning of the game's universe epoch. (A brand new campaign that just started begins at TIME zero.)

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

It's important to be aware of the
:ref:`frozen update nature <frozen>` of the kOS
computer when reading TIME.

System Variables
----------------

This section is about variables that describe the things that are slightly
outside the simulated universe of the game and are more about
the game's user interface or the kOS mod itself.  They represent things
that slightly "break the fourth wall" and let your script access
something entirely outside the in-character experience.

::

    PRINT VERSION.            // Returns operating system version number. e.g. 0.1.2.3
    PRINT VERSION:MAJOR.      // Returns major version number. e.g. 0 if version is 0.1.2.3
    PRINT VERSION:MINOR.      // Returns minor version number. e.g. 1 if version is 0.1.2.3
    PRINT VERSION:PATCH.      // Returns patch version number. e.g. 2 if version is 0.1.2.3
    PRINT VERSION:BUILD.      // Returns build version number. e.g. 3 if version is 0.1.2.3
    PRINT SESSIONTIME.        // Returns amount of time, in seconds, from vessel load.

NOTE the following important difference:

SESSIONTIME is the time since the last time this vessel was loaded from
on-rails into full physics.

TIME is the time since the entire saved game campaign started, in the
kerbal universe's time. i.e. TIME = 0 means a brand new campaign was
just started.

.. object:: HOMECONNECTION

    .. seealso::

        :global:`HOMECONNECTION`
            Globally bound variable for the connection to "home".

.. object:: CONTROLCONNECTION

    .. seealso::

        :global:`CONTROLCONNECTION`
            Globally bound variable for the connection to a control source.

KUNIVERSE
~~~~~~~~~

:ref:`Kuniverse <kuniverse>` is a structure that contains many settings that
break the fourth wall a little bit and control the game simulation directly.
The eventual goal is probably to move many of the variables you see listed
below into ``kuniverse``.

Config
~~~~~~

CONFIG is a special variable name that refers to the configuration
settings for the kOS mod, and can be used to set or get various
options.

`CONFIG has its own page <structures/misc/config.html>`__ for further
details.

WARP and WARPMODE
~~~~~~~~~~~~~~~~~

Time warp can be controlled with the variables
WARP and WARPMODE.  See :ref:`WARP <warp>`

MAPVIEW
~~~~~~~

A boolean that is both gettable and settable.

If you query MAPVIEW, it's true if on the map screen, and false if on the flight view screen.  If you SET MAPVIEW, you can cause the game to switch between mapview and flight view or visa versa.

LOADDISTANCE
~~~~~~~~~~~~

LOADDISTANCE sets the distance from the active vessel at
which vessels get removed from the full physics engine and put
on-rails, or visa versa.  Note that as of KSP 1.0 the stock game
supports multiple different load distance settings for different
situations such that the value changes depending on where you are.
But kOS does not support this at the moment so in kOS if you set
the LOADDISTANCE, you are setting it to the same value
universally for all situations.

.. _profileresult:

PROFILERESULT()
---------------

If you have the runtime statistics configuration option
:attr:`Config:STAT` set to ``True``, then in addition to
the summary statistics after the program run, you can also
see a detailed report of the "profiling" result of your
most recent program run, by calling the built-in function
``ProfileResult()``.  *"Profiling"* is a programmer's term
that means gathering data about how long the program is
spending doing each piece of the program.  If you are trying
to figure out whether your program spent more milliseconds
printing numbers to the screen, or more milliseconds
calculating a complex formula, or more milliseconds activating
actions on a PartModule, and so on, then this feature may
help.  The ProfileResult() was meant mainly for kOS developers
trying to internally determine which parts of the system could
use the most optomizing.  However, as long as it was implemented
for that purpose, it may as well be made available to all
the users of kOS as well.

To use::

   SET CONFIG:STAT TO TRUE.
   RUN MYPROGRAM.
   PRINT PROFILERESULT().
   // <or>
   LOG PROFILERESULT() TO SOMEFIELNAME.csv.

The function ``ProfileResult()`` returns a string containing
a formatted dump of your whole program, broken down into
the more low-level instructions that make it up, with data
values describing how long was spent in total on each
instruction, how many times that instruction was executed,
and the average time spent on a single execution of that
instruction (by dividing the total time by the count of how
many executions it had).

The format of ``ProfileResult()`` is designed to be suitable
for importing into a spreadsheet program if you like, because
it is formatted as a "comma separated values" file, or CSV
for short.

.. _solarprimevector:

SOLARPRIMEVECTOR
----------------

Gives the Prime Meridian :struct:`Vector` for the Solar System itself, in
current Ship-Raw XYZ coordinates.

Both the :attr:`Orbit:LONGITUDEOFASCENDINGNODE` orbit suffix and the
:attr:`Body:ROTATIONANGLE` body suffix are expressed in terms of
degree offsets from this *Prime Meridian Reference Vector*.

What is the Solar Prime Reference Vector?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The solar prime vector is an arbitrary vector in space used to measure
some orbital parameters that are supposed to remain fixed to space
regardless of how the planets underneath the orbit rotate, or where the
Sun is.  In a sense it can be thought of as the celestial "prime
meridian" of the entire solar system, rather than the "prime meridian" of
any one particular rotating planet or moon.

In a hypothetical Earthling's solar system our Kerbal scientists have
hypothesized may exist in a galaxy far away, Earthbound astronomers use
a reference they called the
`First Point of Aries <https://en.wikipedia.org/wiki/First_Point_of_Aries>`__,
for this purpose.

For Kerbals, it refers to a more arbitrary line in space, pointing at a fixed
point in the firmament, also known as the "skybox".

OPCODESLEFT
-----------

This returns the amount of IPU that are left in this physics tick. This means
that if you receive the value 20, you can run 20 more instructions. After this
amount of instructions, other CPUs will run their instructions and then
`TIME:SECONDS` will increase.

OPCODESLEFT can be used to try to make sure you run a block of code in one
physics tick. This is useful when working with vectors or when interacting
with shared message queues. 

To use::

   // Will always wait the first time, becomes more accurate the second time.
   GLOBAL OPCODESNEEDED TO 1000.
   IF OPCODESLEFT < OPCODESNEEDED
     WAIT 0.
   LOCAL STARTIPU TO OPCODESLEFT.
   LOCAL STARTTIME TO TIME:SECONDS.
   
   // your code here, make sure to keep the instruction count lower than your CONFIG:IPU
   
   IF STARTTIME = TIME:SECONDS {
     SET OPCODESNEEDED TO STARTIPU - OPCODESLEFT.
   } ELSE {
     PRINT "Code is taking too long to execute. Please make the code shorter or raise the IPU.".
   }

Addons
------

Get-only.  ``addons`` is a special variable used to access various extensions
to kOS that are designed to support the features introduced by some other mods.  More info can be found on the :ref:`addons <addons>` page.

Colors
------

There are several bound variables associated with :ref:`hardcoded colors <colors>` such as WHITE, BLACK, RED, etc.  See the linked page for the full list.
