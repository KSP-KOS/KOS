.. _vessel:

Vessel
======

All vessels share a structure. To get a variable referring to any vessel you can do this::

    // Get a vessel by it's name.
    // The name is Case Sensitive.
    SET MY_VESS TO VESSEL("Some Ship Name").

    // Save the current vessel in a variable,
    // in case the current vessel changes.
    SET MY_VESS TO SHIP.

    // Save the target vessel in a variable,
    // in case the target vessel changes.
    SET MY_VESS TO TARGET.

Vessels are also :ref:`Orbitable<orbitable>`, and as such have all the associated suffixes as well as some additional suffixes.

.. structure:: Vessel

    ======================================== =============================== =============
    Suffix                                   Type                            Description
    ======================================== =============================== =============
                   Every suffix of :struct:`Orbitable`
    --------------------------------------------------------------------------------------
    :attr:`CONTROL`                          :struct:`Control`               Raw flight controls
    :attr:`BEARING`                          :struct:`Scalar` (deg)          relative heading to this vessel
    :attr:`HEADING`                          :struct:`Scalar` (deg)          Absolute heading to this vessel
    :attr:`THRUST`                           :struct:`Scalar`                Sum of active thrusts
    :attr:`MAXTHRUST`                        :struct:`Scalar`                Sum of active maximum thrusts
    :meth:`MAXTHRUSTAT(pressure)`            :struct:`Scalar`                Sum of active maximum thrusts at the given atmospheric pressure
    :attr:`AVAILABLETHRUST`                  :struct:`Scalar`                Sum of active limited maximum thrusts
    :meth:`AVAILABLETHRUSTAT(pressure)`      :struct:`Scalar`                Sum of active limited maximum thrusts at the given atmospheric pressure
    :attr:`FACING`                           :struct:`Direction`             The way the vessel is pointed
    :attr:`BOUNDS`                           :struct:`Bounds`                Construct bounding box information about the vessel
    :attr:`MASS`                             :struct:`Scalar` (metric tons)  Mass of the ship
    :attr:`WETMASS`                          :struct:`Scalar` (metric tons)  Mass of the ship fully fuelled
    :attr:`DRYMASS`                          :struct:`Scalar` (metric tons)  Mass of the ship with no resources
    :attr:`DYNAMICPRESSURE`                  :struct:`Scalar` (ATM's)        Air Pressure surrounding the vessel
    :attr:`Q`                                :struct:`Scalar` (ATM's)        Alias name for DYNAMICPRESSURE
    :attr:`VERTICALSPEED`                    :struct:`Scalar` (m/s)          How fast the ship is moving "up"
    :attr:`GROUNDSPEED`                      :struct:`Scalar` (m/s)          How fast the ship is moving "horizontally"
    :attr:`AIRSPEED`                         :struct:`Scalar` (m/s)          How fast the ship is moving relative to the air
    :attr:`TERMVELOCITY` (DEPRECATED)        :struct:`Scalar` (m/s)          terminal velocity of the vessel
    :attr:`SHIPNAME`                         :struct:`String`                The name of the vessel
    :attr:`NAME`                             :struct:`String`                Synonym for SHIPNAME
    :attr:`STATUS`                           :struct:`String`                Current ship status
    :attr:`DELTAV`                           :struct:`DeltaV`                Summed Delta-V info about the ship
    :meth:`STAGEDELTAV(num)`                 :struct:`DeltaV`                One stage's Delta-V info
    :attr:`STAGENUM`                         :struct:`Scalar`                Which stage number is current
    :attr:`TYPE`                             :struct:`String`                Ship type
    :meth:`STARTTRACKING`                    None                            Start tracking the asteroid "vessel" via the tracking station
    :meth:`STOPTRACKING`                     None                            Stop tracking the asteroid "vessel" via the tracking station
    :attr:`SIZECLASS`                        :struct:`String`                Return the size class for an asteroid-like object
    :attr:`ANGULARMOMENTUM`                  :struct:`Vector`                In :ref:`SHIP_RAW <ship-raw>`
    :attr:`ANGULARVEL`                       :struct:`Vector`                In :ref:`SHIP_RAW <ship-raw>`
    :attr:`SENSORS`                          :struct:`VesselSensors`         Sensor data
    :attr:`LOADED`                           :struct:`Boolean`               loaded into KSP physics engine or "on rails"
    :attr:`UNPACKED`                         :struct:`Boolean`               The ship has individual parts unpacked
    :attr:`LOADDISTANCE`                     :struct:`LoadDistance`          the :struct:`LoadDistance` object for this vessel
    :attr:`ISDEAD`                           :struct:`Boolean`               True if the vessel refers to a ship that has gone away.
    :attr:`PATCHES`                          :struct:`List`                  :struct:`Orbit` patches
    :attr:`ROOTPART`                         :struct:`Part`                  Root :struct:`Part` of this vessel
    :attr:`CONTROLPART`                      :struct:`Part`                  Control reference :struct:`Part` of this vessel
    :attr:`PARTS`                            :struct:`List`                  all :struct:`Parts <Part>`
    :attr:`ENGINES`                          :struct:`List`                  all :struct:`Engines <Engine>`
    :attr:`RCS`                              :struct:`List`                  all :struct:`RCS <RCS>`
    :attr:`DOCKINGPORTS`                     :struct:`List`                  all :struct:`DockingPorts <DockingPort>`
    :attr:`ELEMENTS`                         :struct:`List`                  all :struct:`Elements <Element>`
    :attr:`RESOURCES`                        :struct:`List`                  all :struct:`AggrgateResources <AggregateResource>`
    :meth:`PARTSNAMED(name)`                 :struct:`List`                  :struct:`Parts <Part>` by :attr:`NAME <Part:NAME>`
    :meth:`PARTSNAMEDPATTERN(namePattern)`   :struct:`List`                  :struct:`Parts <Part>` by :attr:`NAME <Part:NAME>` regex pattern
    :meth:`PARTSTITLED(title)`               :struct:`List`                  :struct:`Parts <Part>` by :attr:`TITLE <Part:TITLE>`
    :meth:`PARTSTITLEDPATTERN(titlePattern)` :struct:`List`                  :struct:`Parts <Part>` by :attr:`TITLE <Part:TITLE>` regex pattern
    :meth:`PARTSTAGGED(tag)`                 :struct:`List`                  :struct:`Parts <Part>` by :attr:`TAG <Part:TAG>`
    :meth:`PARTSTAGGEDPATTERN(tagPattern)`   :struct:`List`                  :struct:`Parts <Part>` by :attr:`TAG <Part:TAG>` regex pattern
    :meth:`PARTSDUBBED(name)`                :struct:`List`                  :struct:`Parts <Part>` by :attr:`NAME <Part:NAME>`, :attr:`TITLE <Part:TITLE>` or :attr:`TAG <Part:TAG>`
    :meth:`PARTSDUBBEDPATTERN(namePattern)`  :struct:`List`                  :struct:`Parts <Part>` by :attr:`NAME <Part:NAME>`, :attr:`TITLE <Part:TITLE>` or :attr:`TAG <Part:TAG>`  regex pattern
    :meth:`MODULESNAMED(name)`               :struct:`List`                  :struct:`PartModules <PartModule>` by :attr:`NAME <PartModule:NAME>`
    :meth:`PARTSINGROUP(group)`              :struct:`List`                  :struct:`Parts <Part>` by action group
    :meth:`MODULESINGROUP(group)`            :struct:`List`                  :struct:`PartModules <PartModule>` by action group
    :meth:`ALLTAGGEDPARTS()`                 :struct:`List`                  :struct:`Parts <Part>` that have non-blank nametags
    :attr:`CREWCAPACITY`                     :struct:`Scalar`                Crew capacity of this vessel
    :meth:`CREW()`                           :struct:`List`                  all :struct:`CrewMembers <CrewMember>`
    :attr:`CONNECTION`                       :struct:`Connection`            Returns your connection to this vessel
    :attr:`MESSAGES`                         :struct:`MessageQueue`          This vessel's message queue
    :attr:`DELTAV`                           :struct:`Scalar` (m/s)          The total delta-v of this vessel in its current situation
    :attr:`DELTAVASL`                        :struct:`Scalar` (m/s)          The total delta-v of this vessel if it were at sea level
    :attr:`DELTAVVACUUM`                     :struct:`Scalar` (m/s)          The total delta-v of this vessel if it were in a vacuum
    :attr:`BURNTIME`                         :struct:`Scalar` (s)            The total burn time of this vessel (or 5 if the vessel has 0 delta/v).
    ======================================== =============================== =============

.. note::

    This type is serializable.

.. attribute:: Vessel:CONTROL

    :type: :struct:`Control`
    :access: Get only

    The structure representing the raw flight controls for the vessel.

    WARNING: This suffix is only gettable for :ref:`CPU Vessel <cpu vessel>`

.. attribute:: Vessel:BEARING

    :type: :struct:`Scalar`
    :access: Get only

    *relative* compass heading (degrees) to this vessel from the :ref:`CPU Vessel <cpu vessel>`, taking into account the CPU Vessel's own heading.

.. attribute:: Vessel:HEADING

    :type: :struct:`Scalar`
    :access: Get only

    *absolute* compass heading (degrees) to this vessel from the :ref:`CPU Vessel <cpu vessel>`

.. attribute:: Vessel:THRUST

    :type: :struct:`Scalar`
    :access: Get only

    Sum of all the :ref:`engines' THRUSTs <engine_THRUST>` of all the currently active engines In Kilonewtons.

.. attribute:: Vessel:MAXTHRUST

    :type: :struct:`Scalar`
    :access: Get only

    Sum of all the :ref:`engines' MAXTHRUSTs <engine_MAXTHRUST>` of all the currently active engines In Kilonewtons.

.. method:: Vessel:MAXTHRUSTAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: :struct:`Scalar` (kN)

    Sum of all the :ref:`engines' MAXTHRUSTATs <engine_MAXTHRUSTAT>` of all the currently active engines In Kilonewtons at the given atmospheric pressure.  Use a pressure of 0 for vacuum, and 1 for sea level (on Kerbin).
    (Pressure must be greater than or equal to zero.  If you pass in a
    negative value, it will be treated as if you had given a zero instead.)

.. attribute:: Vessel:AVAILABLETHRUST

    :type: :struct:`Scalar`
    :access: Get only

    Sum of all the :ref:`engines' AVAILABLETHRUSTs <engine_AVAILABLETHRUST>` of all the currently active engines taking into account their throttlelimits. Result is in Kilonewtons.

.. method:: Vessel:AVAILABLETHRUSTAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: :struct:`Scalar` (kN)

    Sum of all the :ref:`engines' AVAILABLETHRUSTATs <engine_AVAILABLETHRUSTAT>` of all the currently active engines taking into account their throttlelimits at the given atmospheric pressure. Result is in Kilonewtons.  Use a pressure of 0 for vacuum, and 1 for sea level (on Kerbin).
    (Pressure must be greater than or equal to zero.  If you pass in a
    negative value, it will be treated as if you had given a zero instead.)

.. attribute:: Vessel:FACING

    :type: :struct:`Direction`
    :access: Get only

    The way the vessel is pointed, which is also the rotation
    that would transform a vector from a coordinate space where the
    axes were oriented to match the vessel's orientation, to one
    where they're oriented to match the world's ship-raw coordinates.
    
    i.e. ``SHIP:FACING * V(0,0,1)`` gives the direction the
    ship is pointed (it's Z-axis) in absolute ship-raw coordinates

.. attribute:: Vessel:BOUNDS

    :type: :struct:`Bounds`
    :access: Get only

    Constructs a "bounding box" structure that can be used to
    give your script some idea of the extents of the vessel's shape - how
    wide, long, and tall it is.

    It is rather expensive in terms of CPU time to call this suffix.
    (Calling :attr:`Part:BOUNDS` on ONE part on the ship is itself a
    *little* expensive, and this has to perform that same work on
    every part on the ship, finding the bounding box that would
    surround all the parts.) Because of that expense, kOS **forces**
    your script to give up its remaining instructions this update when
    you call this (It forces the equivalent of doing a ``WAIT 0.``
    right after you call it).  This is to discourage you from
    calling this suffix again and again in a fast loop.  The proper
    way to use this suffix is to call it once, storing the result in
    a variable, and then use that variable repeatedly, rather than
    using the suffix itself repeatedly.  Only call the suffix again
    when you have reason to expect the bounding box to change or
    become invalid, such as docking, staging, changing facing to a
    new control-from part, and so on.

    More detailed information about how to read the bounds box, and 
    what circumstances call for getting a re-generated copy of the
    bounds box, is found on the documentation page for :struct:`Bounds`.

.. attribute:: Vessel:MASS

    :type: :struct:`Scalar` (metric tons)
    :access: Get only

    The mass of the ship

.. attribute:: Vessel:WETMASS

    :type: :struct:`Scalar` (metric tons)
    :access: Get only

    The mass of the ship if all resources were full

.. attribute:: Vessel:DRYMASS

    :type: :struct:`Scalar` (metric tons)
    :access: Get only

    The mass of the ship if all resources were empty

.. attribute:: Vessel:DYNAMICPRESSURE

    :type: :struct:`Scalar` (ATM's)
    :access: Get only

    Returns what the air pressure is in the atmosphere surrounding the vessel.
    The value is returned in units of sea-level Kerbin atmospheres.  Many
    formulae expect you to plug in a value expressed in kiloPascals, or
    kPA.  You can convert this value into kPa by multiplying it by
    `constant:ATMtokPa`.

.. attribute:: Vessel:Q

    :type: :struct:`Scalar` (ATM's)
    :access: Get only

    Alias for DYNAMICPRESSURE

.. attribute:: Vessel:VERTICALSPEED

    :type: :struct:`Scalar` (m/s)
    :access: Get only

    How fast the ship is moving. in the "up" direction relative to the SOI Body's sea level surface.

.. attribute:: Vessel:GROUNDSPEED

    :type: :struct:`Scalar` (m/s)
    :access: Get only

    How fast the ship is moving in the two dimensional plane horizontal
    to the SOI body's sea level surface.  The vertical component of the
    ship's velocity is ignored when calculating this.

    .. note::

       .. versionadded:: 0.18
           The old name for this value was SURFACESPEED.  The name was changed
           because it was confusing before.  "surface speed" implied it's the
           :struct:`Scalar` magnitude of "surface velocity", but it wasn't, because of how
           it ignores the vertical component.

.. attribute:: Vessel:AIRSPEED

    :type: :struct:`Scalar` (m/s)
    :access: Get only

    How fast the ship is moving relative to the air. KSP models atmosphere as simply a solid block of air "glued" to the planet surface (the weather on Kerbin is boring and there's no wind). Therefore airspeed is generally the same thing as as the magnitude of the surface velocity.

.. attribute:: Vessel:SHIPNAME

    :type: :struct:`String`
    :access: Get/Set

    The name of the vessel as it appears in the tracking station. When you set this, it cannot be empty.

.. attribute:: Vessel:NAME

    Same as :attr:`Vessel:SHIPNAME`.

.. attribute:: Vessel:STATUS

    :type: :struct:`String`
    :access: get only

    The current status of the vessel possible results are: `LANDED`, `SPLASHED`, `PRELAUNCH`, `FLYING`, `SUB_ORBITAL`, `ORBITING`, `ESCAPING` and `DOCKED`.

.. attribute:: Vessel:DELTAV

    :type: :struct:`DeltaV`
    :access: get only

    Summed Delta-V info about the vessel.

.. method:: Vessel:STAGEDELTAV(num)

    :parameter num: :struct:`Scalar` the stage number to query for
    :return: :struct:`DeltaV`
    
    One stage's Delta-V info.  Pass in the stage number for which stage.  The
    curent stage can be found with ``:STAGENUM``, and they count down from
    there to stage 0 at the "top" of the staging list.

    If you pass in a number that is less than zero, it will return the info about
    stage 0.  If you pass in a number that is greater than the current stage, it
    will return the info about the current stage.  In other words, if there are
    currently stages 5, 4, 3, 2, 1, and 0, then passing in -99 gives you stage 0,
    and passing in stage 9999 gets you stage 5.

.. attribute:: Vessel:STAGENUM

    :type: :struct:`Scalar`
    :access: get only
    
    Tells you which stage number is current.  Stage numbers always count down, which
    is backward from how you might usually refer to stages in most space lingo, but
    in KSP, it's how it's done. (Stage 5 on bottom, Stage 0 on top, for example).

    e.g. if STAGENUM is 4, that tells you the vessel has 5 total stages remaining,
    numbered 4, 3, 2, 1, and 0.

.. attribute:: Vessel:TYPE

    :type: :struct:`String`
    :access: Get/Set

    The ship's type as described `on the KSP wiki <http://wiki.kerbalspaceprogram.com/wiki/Craft#Vessel_types>`_.

.. method:: Vessel:STARTTRACKING

    :return: None

    Call this method to start tracking the object.  This is functionally the
    same as clicking on the "Start Tracking" button in the Tracking Station
    interface.  The primary purpose is to change asteroids from being displayed
    in the tracking station or on the map as ``"Unknown"`` to being displayed as
    ``"SpaceObject"``.  By doing so, the asteroid will not be de-spawned by
    KSP's asteroid management system.

    .. note::
        This does not change the value returned by :attr:`Vessel:TYPE`.  KSP
        internally manages the "discovery information" for vessels, including
        assteroids, in a different system. As a result, the value kOS reads for
        ``TYPE`` may be different from that displayed on the map.

.. method:: Vessel:STOPTRACKING

    :return: None

    Call this method to stop tracking an asteroid or asteroid-like object.
    This is functionally the same as using the Tracking Station interface
    to tell KSP to forget the asteroid.  Doing so also tells the Tracking
    Station that it's okay to de-spawn the object if it feels the need
    to clean it up to avoid clutter.

.. attribute:: Vessel:SIZECLASS

    :type: :struct:`String`
    :access: Get only

    Returns the size class for an asteroid or asteroid-like object (which
    is modeled in the game as a vessel).  (i.e. class A, B, C, D, or E
    for varying size ranges of asteroid.) For objects that the tracking
    station is tracking but you have not yet rendezvous'ed with, sometimes
    all the game lets you know is the general class and not the specific
    dimensions or mass.

    If you are not tracking the object yet, the returned string can come
    back as "UNKNOWN" rather than one of the known class sizes.

.. attribute:: Vessel:ANGULARMOMENTUM

    :type: :struct:`Direction`
    :access: Get only

    Given in :ref:`SHIP_RAW <ship-raw>` reference frame. The vector
    represents the axis of the rotation (in left-handed convention,
    not right handed as most physics textbooks show it), and its
    magnitude is the angular momentum of the rotation, which varies
    not only with the speed of the rotation, but also with the angular
    inertia of the vessel.

    Units are expressed in: (Megagrams * meters^2) / (seconds * radians)

    (Normal SI units would use kilograms, but in KSP all masses use a
    1000x scaling factor.)

    **Justification for radians here:**
    Unlike the trigonometry functions in kOS, this value uses radians
    rather than degrees.  The convention of always expressing angular
    momentum using a formula that assumes you're using radians is a very
    strongly adhered to universal convention, for... reasons.
    It's so common that it's often not even explicitly
    mentioned in information you may find when doing a web search on
    helpful formulae about angular momentum.  This is why kOS doesn't
    use degrees here.  (That an backward compatibility for old scripts.
    It's been like this for quite a while.).

.. attribute:: Vessel:ANGULARVEL

    Angular velocity of the body's rotation about its axis (its
    day) expressed as a vector.

    The direction the angular velocity points is in Ship-Raw orientation,
    and represents the axis of rotation.  Remember that everything in
    Kerbal Space Program uses a *left-handed coordinate system*, which
    affects which way the angular velocity vector will point.  If you
    curl the fingers of your **left** hand in the direction of the rotation,
    and stick out your thumb, the thumb's direction is the way the
    angular velocity vector will point.

    The magnitude of the vector is the speed of the rotation.

    Note, unlike many of the other parts of kOS, the rotation speed is
    expressed in radians rather than degrees.  This is to make it
    congruent with how VESSEL:ANGULARMOMENTUM is expressed, and for
    backward compatibility with older kOS scripts.

.. attribute:: Vessel:SENSORS

    :type: :struct:`VesselSensors`
    :access: Get only

    Structure holding summary information of sensor data for the vessel

.. attribute:: Vessel:LOADED

    :type: :ref:`Boolean <boolean>`
    :access: Get only

    True if the vessel is fully loaded into the complete KSP physics engine (false if it's "on rails").
    See :struct:`LoadDistance` for details.

.. attribute:: Vessel:UNPACKED

    :type: :ref:`Boolean <boolean>`
    :access: Get only

    True if the vessel is fully unpacked.  That is to say that all of the individual parts are loaded
    and can be interacted with.  This allows docking ports to be targeted, and controls if some
    actions/events on parts will actually trigger.  See :struct:`LoadDistance` for details.


.. attribute:: Vessel:LOADDISTANCE

    :type: :struct:`LoadDistance`
    :access: Get only

    Returns the load distance object for this vessel.  The suffixes of this object may be adjusted to change the loading behavior of this vessel. Note: these settings are not persistent across flight instances, and will reset the next time you launch a craft from an editor or the tracking station.

.. attribute:: Vessel:ISDEAD

    :type: :ref:`Boolean <boolean>`
    :access: Get only

    It is possible to have a variable that refers to a vessel that
    doesn't exist in the Kerbal Space Program universe anymore, but
    did back when you first got it.  For example: you could do:
    SET VES TO VESSEL("OTHER"). WAIT 10. And in that intervening
    waiting time, the vessel might have crashed into the ground.
    Checking :ISDEAD lets you see if the vessel that was previously
    valid isn't valid anymore.

.. attribute:: Vessel:PATCHES

    :type: :struct:`List`
    :access: Get only

    The list of :ref:`orbit patches <orbit>` that describe this vessel's current travel path based on momentum alone with no thrusting changes. If the current path has no transitions to other bodies, then this will be a list of only one orbit. If the current path intersects other bodies, then this will be a list describing the transitions into and out of the intersecting body's sphere of influence. SHIP:PATCHES[0] is always exactly the same as SHIP:OBT, SHIP:PATCHES[1] is the same as SHIP:OBT:NEXTPATCH, SHIP:PATCHES[2] is the same as SHIP:OBT:NEXTPATCH:NEXTPATCH, and so on. Note that you will only see as far into the future as your KSP settings allow. (See the setting CONIC\_PATCH\_LIMIT in your settings.cfg file)

.. attribute:: Vessel:ROOTPART

    :type: :struct:`Part`
    :access: Get only

    The ROOTPART is usually the first :struct:`Part` that was used to begin the ship design - the command core. Vessels in KSP are built in a tree-structure, and the first part that was placed is the root of that tree. It is possible to change the root part in VAB/SPH by using Root tool, so ROOTPART does not always point to command core or command pod. Vessel:ROOTPART may change in flight as a result of docking/undocking or decoupling of some part of a Vessel.

.. attribute:: Vessel:CONTROLPART

    :type: :struct:`Part`
    :access: Get only

    Returns the :struct:`Part` serving as the control reference, relative to
    which the directions (as displayed on the navball and returned in
    :attr:`FACING`) are determined. A part may be set as the control reference
    part by "Control From Here" action or :meth:`PART:CONTROLFROM` command
    (available for parts of specific types).  **NOTE:** It is possible for this
    to return unexpected values if the root part of the vessel cannot serve as a
    control reference, and the control has not been directly selected.

.. attribute:: Vessel:PARTS

    :type: :struct:`List` of :struct:`Part` objects
    :access: Get only

    A List of all the :ref:`parts <part>` on the vessel. ``SET FOO TO SHIP:PARTS.`` has exactly the same effect as ``LIST PARTS IN FOO.``. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. attribute:: Vessel:ENGINES

    :type: :struct:`List` of :struct:`Engine` objects
    :access: Get only

    A List of all the :ref:`engines <Engine>` on the Vessel.
    
.. attribute:: Vessel:RCS

    :type: :struct:`List` of :struct:`RCS` objects
    :access: Get only

    A List of all the :ref:`RCS thrusters <RCS>` on the Vessel.
    
.. attribute:: Vessel:DOCKINGPORTS

    :type: :struct:`List` of :struct:`DockingPort` objects
    :access: Get only

    A List of all the :ref:`docking ports <DockingPort>` on the Vessel.

.. attribute:: Vessel:ELEMENTS

    :type: :struct:`List` of :struct:`Element` objects
    :access: Get only

    A List of all the :ref:`elements <Element>` on the Vessel.

.. attribute:: Vessel:RESOURCES

    :type: :struct:`List` of :struct:`AggregateResource` objects
    :access: Get only

    A List of all the :ref:`AggregateResources <AggregateResource>` on the vessel. ``SET FOO TO SHIP:RESOURCES.`` has exactly the same effect as ``LIST RESOURCES IN FOO.``.


.. method:: Vessel:PARTSNAMED(name)

    :parameter name: (:struct:`String`) Name of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Returns a list of all the parts that have this as their
    ``Part:NAME``. The matching is done case-insensitively. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSNAMEDPATTERN(namePattern)

    :parameter namePattern: (:struct:`String`) Pattern of the name of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Returns a list of all the parts that have this Regex pattern in their
    ``Part:NAME``. The matching is done identically as in :meth:`String:MATCHESPATTERN`\ . For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSTITLED(title)

    :parameter title: (:struct:`String`) Title of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Returns a list of all the parts that have this as their
    ``Part:TITLE``. The matching is done case-insensitively. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSTITLEDPATTERN(titlePattern)

    :parameter titlePattern: (:struct:`String`) Patern of the title of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Returns a list of all the parts that have this Regex pattern in their
    ``Part:TITLE``. The matching is done identically as in :meth:`String:MATCHESPATTERN`\ . For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSTAGGED(tag)

    :parameter tag: (:struct:`String`) Tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Returns a list of all the parts that have this name as their
    ``Part:TAG`` value. The matching is done case-insensitively. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSTAGGEDPATTERN(tagPattern)

    :parameter tagPattern: (:struct:`String`) Pattern of the tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Returns a list of all the parts that match this Regex pattern in their
    ``part:TAG`` value. The matching is done identically as in :meth:`String:MATCHESPATTERN`\ . For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSDUBBED(name)

    :parameter name: (:struct:`String`) name, title or tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Return a list of all the parts that match this
    name regardless of whether that name is the Part:Name, the Part:Tag, or the Part:Title. It is effectively the distinct union of :PARTSNAMED(val), :PARTSTITLED(val), :PARTSTAGGED(val). The matching is done case-insensitively. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSDUBBEDPATTERN(namePattern)

    :parameter namePattern: (:struct:`String`) Pattern of the name, title or tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Return a list of parts that match this Regex pattern
    regardless of whether that name is the Part:Name, the Part:Tag, or the Part:Title. It is effectively the distinct union of :PARTSNAMEDPATTERN(val), :PARTSTITLEDPATTERN(val), :PARTSTAGGEDPATTERN(val). The matching is done identically as in :meth:`String:MATCHESPATTERN`\ . For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:MODULESNAMED(name)

    :parameter name: (:struct:`String`) Name of the part modules
    :return: :struct:`List` of :struct:`PartModule` objects

    Return a list of all the :struct:`PartModule` objects that
    match the given name. The matching is done case-insensitively. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSINGROUP(group)

    :parameter group: (integer) the action group number
    :return: :struct:`List` of :struct:`Part` objects

    one action triggered by the given action group. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:MODULESINGROUP(group)

    :parameter group: (integer) the action group number
    :return: :struct:`List` of :struct:`PartModule` objects

    have at least one action triggered by the given action group. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:ALLTAGGEDPARTS()

    :return: :struct:`List` of :struct:`Part` objects

    Return all parts who's nametag isn't blank.
    For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. attribute:: Vessel:CREWCAPACITY

    :type: :struct:`Scalar`
    :access: Get only

    crew capacity of this vessel

.. method:: Vessel:CREW()

    :return: :struct:`List` of :struct:`CrewMember` objects

    list of all :struct:`kerbonauts <CrewMember>` aboard this vessel

.. attribute:: Vessel:CONNECTION

    :return: :struct:`Connection`

    Returns your connection to this vessel.

.. attribute:: Vessel:MESSAGES

    :return: :struct:`MessageQueue`

    Returns this vessel's message queue. You can only access this attribute for your current vessel (using for example `SHIP:MESSAGES`).

.. attribute:: Vessel:DELTAV

    :return: :struct:`Scalar`

    The total delta-v of this vessel in its current situation, using the stock
    calulations the KSP game shows in the staging list.  Note that this is only
    as accurate as the stock KSP game's numbers are.

.. attribute:: Vessel:DELTAVASL

    :return: :struct:`Scalar`

    The total delta-v of this vessel if it were at sea level, using the stock
    calulations the KSP game shows in the staging list.  Note that this is only
    as accurate as the stock KSP game's numbers are.

.. attribute:: Vessel:DELTAVVACUUM

    :return: :struct:`Scalar`

    The total delta-v of this vessel if it were at sea vacuum, using the stock
    calulations the KSP game shows in the staging list.  Note that this is only
    as accurate as the stock KSP game's numbers are.

.. attribute:: Vessel:BURNTIME

    :return: :struct:`Scalar`

    The total burn time, in seconds, of this vessel (or 5 if the vessel has 0 delta/v). Burn time is not affected by atmosphere.  This is using the stock
    calulations the KSP game shows in the staging list.  Note that this is only
    as accurate as the stock KSP game's numbers are.


Deprecated Suffix
-----------------

.. attribute:: Vessel:TERMVELOCITY

    :type: :struct:`Scalar` (m/s)
    :access: Get only

    (Deprecated with KSP 1.0 atmospheric model)
    
    Terminal velocity of the vessel in freefall through atmosphere, based on the vessel's current altitude above sea level, and its drag properties. Warning, can cause values of Infinity if used in a vacuum, and kOS sometimes does not let you store Infinity in a variable.

    .. note::

        .. deprecated:: 0.17.2

           Removed to account for significant changes to planetary atmosphere mechanics introduced in KSP 1.0
