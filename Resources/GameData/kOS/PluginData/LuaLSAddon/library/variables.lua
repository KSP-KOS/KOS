---Get/Set.
---:type: Action Group, `boolean`
---
---Turns the SAS **on** or **off**, like using ``T`` at the keybaord::
---
---    SAS ON. // same as SET SAS TO TRUE.
---    SAS OFF. // same as SET SAS TO FALSE.
---    PRINT SAS.  // prints either "True" or "False".
---
---.. warning::
---
---    Be aware that having KSP's ``SAS`` turned on *will* conflict
---    with using "cooked control" (the ``lock steering`` command).  You
---    should not use these two modes of steering control at the same time.
---    For further information see the
---    :ref:`warning in lock steering documentation<locksteeringsaswarning>`.
---@type boolean
sas = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Deploys or retracts the landing gear, like using the ``G`` key at the keyboard::
---
---    GEAR ON.
---@type boolean
gear = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Deploys or retracts all the landing legs (but not wheeled landing gear)::
---
---    LEGS ON.
---
---Returns true if all the legs are deployed.
---@type boolean
legs = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Deploys all the parachutes (only `ON` command has effect)::
---
---    CHUTES ON.
---
---Returns true if all the chutes are deployed.
---@type boolean
chutes = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Deploys all the parachutes than can be safely deployed in the current conditions (only `ON` command has effect)::
---
---    CHUTESSAFE ON.
---
---Returns false only if there are disarmed parachutes chutes which may be safely
---deployed, and true if all safe parachutes are already deployed including
---any time where there are no safe parachutes.
---
---The following code will gradually deploy all the chutes as the speed drops::
---
---    WHEN (NOT CHUTESSAFE) THEN {
---        CHUTESSAFE ON.
---        RETURN (NOT CHUTES).
---    }
---@type boolean
chutessafe = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Turns the lights **on** or **off**, like using the ``U`` key at the keyboard::
---
---    LIGHTS ON.
---@type boolean
lights = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Extends or retracts all the deployable solar panels::
---
---    PANELS ON.
---
---Returns true if all the panels are extended, including those inside of
---fairings or cargo bays.
---
---.. note::
---    Some solar panels can't be retracted once deployed.  Consult the part's
---    description for details.
---@type boolean
panels = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Extends or retracts all the deployable radiators and activates or deactivates all the fixed ones::
---
---    RADIATORS ON.
---
---Returns true if all the radiators are extended (if deployable) and active.
---@type boolean
radiators = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Extends or retracts all the extendable ladders::
---
---    LADDERS ON.
---
---Returns true if all the ladders are extended.
---@type boolean
ladders = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Opens or closes all the payload and service bays (including the cargo ramp)::
---
---    BAYS ON.
---
---Returns true if at least one bay is open.
---@type boolean
bays = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Deploys or retracts all the mining drills::
---
---    DEPLOYDRILLS ON.
---
---Returns true if all the drills are deployed.
---@type boolean
deploydrills = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Activates (has effect only on drills that are deployed and in contact with minable surface) or stops all the mining drills::
---
---    DRILLS ON.
---
---Returns true if at least one drill is actually mining.
---@type boolean
drills = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Activates or deactivates all the fuel cells (distingushed from other conveters by converter/action names)::
---
---    FUELCELLS ON.
---
---Returns true if at least one fuel cell is activated.
---@type boolean
fuelcells = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Activates or deactivates all the ISRU converters (distingushed from other conveters by converter/action names)::
---
---    ISRU ON.
---
---Returns true if at least one ISRU converter is activated.
---@type boolean
isru = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Opens or closes all the air intakes::
---
---    INTAKES ON.
---
---Returns true if all the intakes are open.
---@type boolean
intakes = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Turns the brakes **on** or **off**, like clicking the brakes button, though *not* like using the ``B`` key, because they stay on::
---
---    BRAKES ON.
---@type boolean
brakes = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Turns the RCS **on** or **off**, like using ``R`` at the keyboard::
---
---    RCS ON. // same as SET RCS TO TRUE.
---    RCS OFF. // same as SET RCS TO FALSE.
---    PRINT RCS.  // prints either "True" or "False".
---@type boolean
rcs = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---Abort action group (no actions are automatically assigned, configurable in the editor), like using the ``Backspace`` key at the keyboard::
---
---    ABORT ON.
---@type boolean
abort = nil

---Get/Set.
---:type: Action Group, `boolean`
---
---10 custom action groups (no actions are automatically assigned, configurable in the editor), like using the numeric keys at the keyboard::
---
---    AG1 ON.
---    AG4 OFF.
---    SET AG10 to AG3.
---@type boolean
ag1 = nil

---Get/Set.
---@type boolean
ag2 = nil

---Get/Set.
---@type boolean
ag3 = nil

---Get/Set.
---@type boolean
ag4 = nil

---Get/Set.
---@type boolean
ag5 = nil

---Get/Set.
---@type boolean
ag6 = nil

---Get/Set.
---@type boolean
ag7 = nil

---Get/Set.
---@type boolean
ag8 = nil

---Get/Set.
---@type boolean
ag9 = nil

---Get/Set.
---@type boolean
ag10 = nil

---Get only.
---@type RGBA
white = nil

---Get only.
---@type RGBA
black = nil

---Get only.
---@type RGBA
red = nil

---Get only.
---@type RGBA
green = nil

---Get only.
---@type RGBA
blue = nil

---Get only.
---@type RGBA
yellow = nil

---Get only.
---@type RGBA
cyan = nil

---Get only.
---@type RGBA
magenta = nil

---Get only.
---@type RGBA
purple = nil

---Get only.
---@type RGBA
grey = nil

---Get only.
---@type RGBA
gray = nil

---Get only.
---Returns amount of time, in seconds, from vessel load
---@type number
sessiontime = nil

---Get only.
---@type Terminal
terminal = nil

---Get only.
---@type Kuniverse
kuniverse = nil

---Get only.
---Returns a `Connection` representing the :ref:`CPU Vessel's<cpu vessel>`
---communication line to a network "home" node.  This home node may be the KSC
---ground station, or other ground stations added by the CommNet settings or
---RemoteTech.  Functionally, this connection may be used to determine if the
---archive volume is accessible.
---
---.. warning::
---
---    Attempting to send a message to the "home" connection will result in an
---    error message.  While this connection uses the same structure as when
---    sending inter-vessel and inter-processor messages, message support is
---    not included.
---@type Connection
homeconnection = nil

---Get only.
---Returns a `Connection` representing the :ref:`CPU Vessel's<cpu vessel>`
---communication line to a control source.  This may be the same as the
---:global:`HOMECONNECTION`, or it may represent a local crewed command pod,
---or it may represent a connection to a control station.  When using the
---``CommNetConnectivityManager`` this should show as connected whenever a vessel
---has partial manned control, or full control.  Functionally this may be used
---to determine if terminal input is available, and what the potential signal
---delay may be for this input.
---
---.. warning::
---
---    Attempting to send a message to the "control" connection will result in
---    an error message.  While this connection uses the same structure as when
---    sending inter-vessel and inter-processor messages, message support is
---    not included.
---@type Connection
controlconnection = nil

---Get/Set.
---This is identical to :attr:`MODE<TimeWarp:MODE>` above.
---::
---
---    // These two do the same thing:
---    SET WARPMODE TO "PHYSICS".
---    SET KUNIVERSE:TIMEWARP:MODE TO "PHYSICS".
---
---    // These two do the same thing:
---    SET WARPMODE TO "RAILS".
---    SET KUNIVERSE:TIMEWARP:MODE TO "RAILS".
---@type string | "physics" | "rails"
warpmode = nil

---Get/Set.
---This is identical to :attr:`WARP<TimeWarp:WARP>` above.
---::
---
---    // These do the same thing:
---    SET WARP TO 3.
---    SET KUNIVERSE:TIMEWARP:WARP to 3.
---@type number
warp = nil

---Get/Set.
---A variable that controls or queries whether or not the game is in map view::
---
---    IF MAPVIEW {
---        PRINT "You are looking at the map.".
---    } ELSE {
---        PRINT "You are looking at the flight view.".
---    }.
---
---You can switch between map and flight views by setting this variable::
---
---    SET MAPVIEW TO TRUE.  // to map view
---    SET MAPVIEW TO FALSE. // to flight view
---@type boolean
mapview = nil

---Get only.
---@type Constant
constant = nil

---Get only.
---Returns operating system version number. e.g. 0.1.2.3
---@type string
version = nil

---Get only.
---Gives the Prime Meridian `Vector` for the Solar System itself, in
---current Ship-Raw XYZ coordinates.
---
---Both the :attr:`Orbit:LONGITUDEOFASCENDINGNODE` orbit suffix and the
---:attr:`Body:ROTATIONANGLE` body suffix are expressed in terms of
---degree offsets from this *Prime Meridian Reference Vector*.
---@type Vector
solarprimevector = nil

---Get only.
---Get-only. ``archive`` returns a `Volume` structure referring to the archive.
---You can read more about what archive is on the :ref:`File & volumes <volumes>` page.
---@type Volume
archive = nil

---Get/Set.
---@type number
THROTTLE = nil

---Get/Set.
---@type Vector | Direction | Node | string | "kill"
STEERING = nil

---Get/Set.
---@type GeoCoordinates | Vessel | number
WHEELSTEERING = nil

---Get/Set.
---@type number
WHEELTHROTTLE = nil

---Get/Set.
---.. object:: SASMODE
---
---Getting this variable will return the currently selected SAS mode.  Where ``value`` is one of the valid strings listed below, this will set the stock SAS mode for the cpu vessel::
---
---    SET SASMODE TO value.
---
---It is the equivalent to clicking on the buttons next to the nav ball while manually piloting the craft, and will respect the current mode of the nav ball (orbital, surface, or target velocity - use NAVMODE to read or set it).  Valid strings for ``value`` are ``"PROGRADE"``, ``"RETROGRADE"``, ``"NORMAL"``, ``"ANTINORMAL"``, ``"RADIALOUT"``, ``"RADIALIN"``, ``"TARGET"``, ``"ANTITARGET"``, ``"MANEUVER"``, ``"STABILITYASSIST"``, and ``"STABILITY"``.  A null or empty string will default to stability assist mode, however any other invalid string will throw an exception.  This feature will respect career mode limitations, and will throw an exception if the current vessel is not able to use the mode passed to the command.  An exception is also thrown if ``"TARGET"`` or ``"ANTITARGET"`` are used when no target is set.
---
---.. note::
---    SAS mode is reset to stability assist when toggling SAS on, however it doesn't happen immediately.
---    Therefore, after activating SAS, you'll have to skip a frame before setting the SAS mode.
---    Velocity-related modes also reset back to stability assist when the velocity gets too low.
---
---.. warning:: SASMODE does not work with RemoteTech
---
---    Due to the way that RemoteTech disables flight control input, the built in SAS modes do not function properly when there is no connection to the KSC or a Command Center.  If you are writing scripts for use with RemoteTech, make sure to take this into account.
---
---.. warning:: SASMODE should not be used with LOCK STEERING
---
---    Be aware that having KSP's ``SAS`` turned on *will* conflict
---    with using "cooked control" (the ``lock steering`` command).  You
---    should not use these two modes of steering control at the same time.
---    For further information see the
---    :ref:`warning in lock steering documentation<locksteeringsaswarning>`.
---@type string | "maneuver" | "prograde" | "retrograde" | "normal" | "antinormal" | "radialin" | "radialout" | "target" | "antitarget" | "stability" | "stabilityassist"
sasmode = nil

---Get/Set.
---.. object:: NAVMODE
---
---Getting this variable will return the currently selected nav ball speed display mode.  Where ``value`` is one of the valid strings listed below, this will set the nav ball mode for the cpu vessel::
---
---    SET NAVMODE TO value.
---
---It is the equivalent to changing the nav ball mode by clicking on speed display on the nav ball while manually piloting the craft, and will change the current mode of the nav ball, affecting behavior of most SAS modes.  Valid strings for ``value`` are ``"ORBIT"``, ``"SURFACE"`` and ``"TARGET"``.  A null or empty string will default to orbit mode, however any other invalid string will throw an exception.  This feature is accessible only for the active vessel, and will throw an exception if the current vessel is not active.  An exception is also thrown if ``"TARGET"`` is used, but no target is selected.
---@type string | "orbit" | "surface" | "target"
navmode = nil

---Get/Set.
---The PIDLoop used to control wheelsteering. Can be used to optimize
---   steering performance and eliminate steering oscillations on some vessels.
---@type PIDLoop
wheelsteeringpid = nil

---Get only.
---@type VesselAltitude
alt = nil

---Get only.
---@type Vector
angularvelocity = nil

---Get only.
---The orbit patch describing the next encounter with a body the current
---vessel will enter. If there is no such encounter coming, it will return
---the special string "None".  If there is an encounter coming, it will
---return an object :ref:`of type Orbit <orbit>`.  (i.e. to obtain the name
---of the planet the encounter is with, you can do:
---``print ENCOUNTER:BODY:NAME.``, for example.).
---@type Orbit | string
encounter = nil

---Get only.
---@type OrbitEta
eta = nil

---Get only.
---You can obtain the number of seconds it has been since the current
---CPU vessel has been launched with the bound global variable
---``MISSIONTIME``.  In real space programs this is referred to usually
---as "MET" - Mission Elapsed Time, and it's what's being measured when
---you hear that familiar voice saying "T minus 10 seconds..."  Point "T"
---is the zero point of the mission elapsed time, and everything before that
---is a negative number and everything after it is a positive number.
---kOS is only capable of returning the "T+" times, not the "T-" times,
---because it doesn't read your mind to know ahead of time when you plan
---to launch.
---@type Struct
missiontime = nil

---Get only.
---The special variable :global:`TIME` is used to get the current time
---in the gameworld (not the real world where you're sitting in a chair
---playing Kerbal Space Program.)  It is the same thing as calling
---:func:`TIME` with empty parentheses.
---@type TimeStamp
time = nil

---Get only.
---@type Vessel
activeship = nil

---Get only.
---Vessel situation
---@type string
status = nil

---Get only.
---@type Stage
stageinfo = nil

---Get/Set.
---.. attribute:: Vessel:SHIPNAME
---
---The name of the vessel as it appears in the tracking station. When you set this, it cannot be empty.
---@type string
shipname = nil

---Get only.
---@type SteeringManager
steeringmanager = nil

---Get only.
---:global:`NEXTNODE` is a built-in variable that always refers to the next upcoming node that has been added to your flight plan::
---
---    SET MyNode to NEXTNODE.
---    PRINT NEXTNODE:PROGRADE.
---    REMOVE NEXTNODE.
---
---Currently, if you attempt to query :global:`NEXTNODE` and there is no node on your flight plan, it produces a run-time error. (This needs to be fixed in a future release so it is possible to query whether or not you have a next node).
---
---.. warning::
---    As per the warning above at the top of the section, NEXTNODE won't work on vessels that are not the active vessel.
---
---The special identifier :global:`NEXTNODE` is a euphemism for "whichever node is coming up soonest on my flight path". Therefore you can remove a node even if you no longer have the maneuver node variable around, by doing this::
---
---    REMOVE NEXTNODE.
---@type Node
nextnode = nil

---Get only.
---Returns true if there is a planned maneuver `ManeuverNode` in the
---:ref:`CPU vessel's <cpu vessel>` flight plan.  This will always return
---false for the non-active vessel, as access to maneuver nodes is limited to the active vessel.
---@type boolean
hasnode = nil

---Get only.
---:type: `List` of `ManeuverNode` elements
---
---Returns a list of all `ManeuverNode` objects currently on the
---:ref:`CPU vessel's <cpu vessel>` flight plan.  This list will be empty if
---no nodes are planned, or if the :ref:`CPU vessel <cpu vessel>` is currently
---unable to use maneuver nodes.
---
---.. note::
---    If you store a reference to this list in a variable, the variable's
---    instance will not be automatically updated if you :global:`ADD` or
---    :global:`REMOVE` maneuver nodes to the flight plan.
---
---.. note::
---    Adding a `ManeuverNode` to this list, or a reference to this
---    list **will not** add it to the flight plan.  Use the :global:`ADD`
---    command instead.
---@type List
allnodes = nil

---Get only.
---@type Orbit
obt = nil

---Get only.
---@type Orbit
orbit = nil

---Get only.
---This returns the amount of IPU (instructions per update) that are
---left in this physics tick. For example, if this gives you the value
---20, you can run 20 more instructions within this physics update
---before the game will let the rest of the game run and advance time.
---After this amount of instructions, other CPUs will run their
---instructions, and the game will do the rest of its work, and then
---`TIME:SECONDS` will increase and you'll get another physics update
---in which to run more of the program.
---
---Another way to think of this is "For the next ``OPCODESLEFT``
---instructions, the universe is still physically frozen, giving
---frozen values for time, position, velocity, etc.  After that
---it will be the next physics tick and those things will have moved
---ahead to the next physics tick."
---
---OPCODESLEFT can be used to try to make sure you run a block of code in one
---physics tick. This is useful when working with vectors or when interacting
---with shared message queues. 
---
---To use::
---
---   // Will always wait the first time, becomes more accurate the second time.
---   GLOBAL OPCODESNEEDED TO 1000.
---   IF OPCODESLEFT < OPCODESNEEDED
--- WAIT 0.
---   LOCAL STARTIPU TO OPCODESLEFT.
---   LOCAL STARTTIME TO TIME:SECONDS.
---   
---   // your code here, make sure to keep the instruction count lower than your CONFIG:IPU
---   
---   IF STARTTIME = TIME:SECONDS {
--- SET OPCODESNEEDED TO STARTIPU - OPCODESLEFT.
---   } ELSE {
--- PRINT "Code is taking too long to execute. Please make the code shorter or raise the IPU.".
---   }
---@type number
opcodesleft = nil

---Get only.
---There is a special keyword ``DONOTHING`` that refers to a special
---kind of `KosDelegate` called a `NoDelegate`.
---
---The type string returned by ``DONOTHING:TYPENAME`` is ``"NoDelegate"``.
---Otherwise an instance of `NoDelegate` has the same suffixes as one
---of `KOSDelegate`, although you're not usually
---expected to ever use them, except maybe ``TYPENAME`` to discover
---that it is a `NoDelegate`.
---
---``DONOTHING`` is used when you're in a situation where you had
---previously assigned a `KosDelegate` to some callback hook
---the kOS system provides, but now you want the kOS system to stop
---calling it.  To do so, you assign that callback hook to the value
---``DONOTHING``.
---
---``DONOTHING`` is similar to making a `KosDelegate` that
---consists of just ``{return.}``.  If you attempt to call it from
---your own code, that's how it will behave.  But the one extra
---feature it has is that it allows kOS to understand your intent
---that you wish to disable a callback hook.  kOS can detect when
---the ``KosDelegate`` you assign to something happens to be the
---``DONOTHING`` delegate.  When it is, kOS knows to not even
---bother calling the delegate at all anymore.
---@type NoDelegate
donothing = nil

---Get only.
---@type Config
config = nil

---Get only.
---@type Addons
addons = nil

---Get only.
---@type Core
core = nil

---Get only.
---@type Vessel
ship = nil

---Get/Set.
---:type: `string` (set); `Vessel` or `Body` or `Part` (get/set)
---
---Where ``name`` is the name of a target vessel or planet, this will set the current target::
---
---    SET TARGET TO name.
---
---For more information see :ref:`bindings`.
---
---NOTE, the way to de-select the target is to set it to an empty
---string like this::
---
---    SET TARGET TO "". // de-selects the target, setting it to nothing.
---
---(Trying to use :ref:`UNSET TARGET.<unset>` will have no effect because
---``UNSET`` means "get rid of the variable itself" which you're not
---allowed to do with built-in bound variables like ``TARGET``.)
---@type string | Vessel | Body | Part
target = nil

---Get only.
---@type boolean
hastarget = nil

---Get only.
---.. attribute:: Vessel:HEADING
---
---*absolute* compass heading (degrees) to this vessel from the :ref:`CPU Vessel <cpu vessel>`
---@type number
shipheading = nil

---Get only.
---.. attribute:: Vessel:AVAILABLETHRUST
---
---Sum of all the :ref:`engines' AVAILABLETHRUSTs <engine_AVAILABLETHRUST>` of all the currently active engines taking into account their throttlelimits. Result is in Kilonewtons.
---@type number
availablethrust = nil

---Get only.
---.. attribute:: Vessel:MAXTHRUST
---
---Sum of all the :ref:`engines' MAXTHRUSTs <engine_MAXTHRUST>` of all the currently active engines In Kilonewtons.
---@type number
maxthrust = nil

---Get only.
---.. attribute:: Vessel:FACING
---
---The way the vessel is pointed, which is also the rotation
---that would transform a vector from a coordinate space where the
---axes were oriented to match the vessel's orientation, to one
---where they're oriented to match the world's ship-raw coordinates.
---
---i.e. ``SHIP:FACING * V(0,0,1)`` gives the direction the
---ship is pointed (it's Z-axis) in absolute ship-raw coordinates
---@type Direction
facing = nil

---Get only.
---.. attribute:: Vessel:ANGULARMOMENTUM
---
---Given in :ref:`SHIP_RAW <ship-raw>` reference frame. The vector
---represents the axis of the rotation (in left-handed convention,
---not right handed as most physics textbooks show it), and its
---magnitude is the angular momentum of the rotation, which varies
---not only with the speed of the rotation, but also with the angular
---inertia of the vessel.
---
---Units are expressed in: (Megagrams * meters^2) / (seconds * radians)
---
---(Normal SI units would use kilograms, but in KSP all masses use a
---1000x scaling factor.)
---
---**Justification for radians here:**
---Unlike the trigonometry functions in kOS, this value uses radians
---rather than degrees.  The convention of always expressing angular
---momentum using a formula that assumes you're using radians is a very
---strongly adhered to universal convention, for... reasons.
---It's so common that it's often not even explicitly
---mentioned in information you may find when doing a web search on
---helpful formulae about angular momentum.  This is why kOS doesn't
---use degrees here.  (That an backward compatibility for old scripts.
---It's been like this for quite a while.).
---@type Vector
angularmomentum = nil

---Get only.
---.. attribute:: Vessel:ANGULARVEL
---
---Angular velocity of the body's rotation about its axis (its
---day) expressed as a vector.
---
---The direction the angular velocity points is in Ship-Raw orientation,
---and represents the axis of rotation.  Remember that everything in
---Kerbal Space Program uses a *left-handed coordinate system*, which
---affects which way the angular velocity vector will point.  If you
---curl the fingers of your **left** hand in the direction of the rotation,
---and stick out your thumb, the thumb's direction is the way the
---angular velocity vector will point.
---
---The magnitude of the vector is the speed of the rotation.
---
---Note, unlike many of the other parts of kOS, the rotation speed is
---expressed in radians rather than degrees.  This is to make it
---congruent with how VESSEL:ANGULARMOMENTUM is expressed, and for
---backward compatibility with older kOS scripts.
---@type Vector
angularvel = nil

---Get only.
---.. attribute:: Vessel:MASS
---
---:type: `number` (metric tons)
---
---The mass of the ship
---@type number
mass = nil

---Get only.
---.. attribute:: Vessel:VERTICALSPEED
---
---:type: `number` (m/s)
---
---How fast the ship is moving. in the "up" direction relative to the SOI Body's sea level surface.
---@type number
verticalspeed = nil

---Get only.
---.. attribute:: Vessel:GROUNDSPEED
---
---:type: `number` (m/s)
---
---How fast the ship is moving in the two dimensional plane horizontal
---to the SOI body's sea level surface.  The vertical component of the
---ship's velocity is ignored when calculating this.
---
---.. note::
---
---   .. versionadded:: 0.18
---       The old name for this value was SURFACESPEED.  The name was changed
---       because it was confusing before.  "surface speed" implied it's the
---       `number` magnitude of "surface velocity", but it wasn't, because of how
---       it ignores the vertical component.
---@type number
groundspeed = nil

---Get only.
---@type number
surfacespeed = nil

---Get only.
---.. attribute:: Vessel:AIRSPEED
---
---:type: `number` (m/s)
---
---How fast the ship is moving relative to the air. KSP models atmosphere as simply a solid block of air "glued" to the planet surface (the weather on Kerbin is boring and there's no wind). Therefore airspeed is generally the same thing as as the magnitude of the surface velocity.
---@type number
airspeed = nil

---Get only.
---@type number
latitude = nil

---Get only.
---@type number
longitude = nil

---Get only.
---@type number
altitude = nil

---Get only.
---.. attribute:: Orbitable:APOAPSIS
---
---:type: `number` (deg)
---
---.. deprecated:: 0.15
---
---   This is only kept here for backward compatibility.
---   in new scripts you write, use :attr:`OBT:APOAPSIS <Orbit:APOAPSIS>`.
---   (i.e. use ``SHIP:OBT:APOAPSIS`` instead of ``SHIP:APOAPSIS``,
---   or use ``MUN:OBT:APOAPSIS`` instead of ``MUN:APOAPSIS``, etc).
---@type number
apoapsis = nil

---Get only.
---.. attribute:: Orbitable:PERIAPSIS
---
---:type: `number` (deg)
---
---.. deprecated:: 0.15
---
---   This is only kept here for backward compatibility.
---   in new scripts you write, use :attr:`OBT:PERIAPSIS <Orbit:PERIAPSIS>`.
---   (i.e. use ``SHIP:OBT:PERIAPSIS`` instead of ``SHIP:PERIAPSIS``).
---   or use ``MUN:OBT:PERIAPSIS`` instead of ``MUN:PERIAPSIS``, etc).
---@type number
periapsis = nil

---Get only.
---.. attribute:: Orbitable:BODY
---
---The `Body` that this object is orbiting. I.e. ``Mun:BODY`` returns ``Kerbin``.
---@type Body
body = nil

---Get only.
---.. attribute:: Orbitable:UP
---
---pointing straight up away from the SOI body.
---@type Direction
up = nil

---Get only.
---.. attribute:: Orbitable:NORTH
---
---pointing straight north on the SOI body, parallel to the surface of the SOI body.
---@type Direction
north = nil

---Get only.
---.. attribute:: Orbitable:PROGRADE
---
---pointing in the direction of this object's **orbitable-frame** velocity
---@type Direction
prograde = nil

---Get only.
---.. attribute:: Orbitable:RETROGRADE
---
---pointing in the opposite of the direction of this object's **orbitable-frame** velocity
---@type Direction
retrograde = nil

---Get only.
---.. attribute:: Orbitable:SRFPROGRADE
---
---pointing in the direction of this object's **surface-frame** velocity. Note that if this Orbitable is itself a body, remember that this is relative to the surface of the SOI body, not this body.
---@type Direction
srfprograde = nil

---Get only.
---.. attribute:: Orbitable:SRFRETROGRADE
---
---pointing in the opposite of the direction of this object's **surface-frame** velocity. Note that this is relative to the surface of the SOI body.
---@type Direction
srfretrograde = nil

---Get only.
---.. attribute:: Orbitable:VELOCITY
---
---The :struct:`orbitable velocity <OrbitableVelocity>` of this object in the :ref:`SHIP-RAW reference frame <ship-raw>`
---@type OrbitableVelocity
velocity = nil

---Get only.
---.. attribute:: Orbitable:GEOPOSITION
---
---A combined structure of the latitude and longitude numbers.
---@type GeoCoordinates
geoposition = nil

---Get only.
---@type Body
sun = nil

---Get only.
---@type Body
kerbin = nil

---Get only.
---@type Body
mun = nil

---Get only.
---@type Body
minmus = nil

---Get only.
---@type Body
moho = nil

---Get only.
---@type Body
eve = nil

---Get only.
---@type Body
duna = nil

---Get only.
---@type Body
ike = nil

---Get only.
---@type Body
jool = nil

---Get only.
---@type Body
laythe = nil

---Get only.
---@type Body
vall = nil

---Get only.
---@type Body
bop = nil

---Get only.
---@type Body
tylo = nil

---Get only.
---@type Body
gilly = nil

---Get only.
---@type Body
pol = nil

---Get only.
---@type Body
dres = nil

---Get only.
---@type Body
eeloo = nil

