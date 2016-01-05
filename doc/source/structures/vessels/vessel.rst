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

.. note::

    .. versionadded:: 0.13

        A vessel is now a type of :struct:`Orbitable`. Much of what a Vessel can do can now by done by any orbitable object. The documentation for those abilities has been moved to the :ref:`orbitable page <orbitable>`.


.. structure:: Vessel

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
                    Every suffix of :struct:`Orbitable`
    -----------------------------------------------------------------------------
     :attr:`CONTROL`                       :struct:`Control`         Raw flight controls
     :attr:`BEARING`                       scalar (deg)              relative heading to this vessel
     :attr:`HEADING`                       scalar (deg)              Absolute heading to this vessel
     :attr:`MAXTHRUST`                     scalar                    Sum of active maximum thrusts
     :meth:`MAXTHRUSTAT(pressure)`         scalar                    Sum of active maximum thrusts at the given atmospheric pressure
     :attr:`AVAILABLETHRUST`               scalar                    Sum of active limited maximum thrusts
     :meth:`AVAILABLETHRUSTAT(pressure)`   scalar                    Sum of active limited maximum thrusts at the given atmospheric pressure
     :attr:`FACING`                        :struct:`Direction`       The way the vessel is pointed
     :attr:`MASS`                          scalar (metric tons)      Mass of the ship
     :attr:`WETMASS`                       scalar (metric tons)      Mass of the ship fully fuelled
     :attr:`DRYMASS`                       scalar (metric tons)      Mass of the ship with no resources
     :attr:`DYNAMICPRESSURE`               scalar (ATM's)            Air Pressure surrounding the vessel
     :attr:`Q`                             scalar (ATM's)            Alias name for DYNAMICPRESSURE
     :attr:`VERTICALSPEED`                 scalar (m/s)              How fast the ship is moving "up"
     :attr:`GROUNDSPEED`                   scalar (m/s)              How fast the ship is moving "horizontally"
     :attr:`AIRSPEED`                      scalar (m/s)              How fast the ship is moving relative to the air
     :attr:`TERMVELOCITY`                  scalar (m/s)              terminal velocity of the vessel
     :attr:`SHIPNAME`                      string                    The name of the vessel
     :attr:`NAME`                          string                    Synonym for SHIPNAME
     :attr:`STATUS`                        string                    Current ship status
     :attr:`TYPE`                          string                    Ship type
     :attr:`ANGULARMOMENTUM`               :struct:`Vector`          In :ref:`SHIP_RAW <ship-raw>`
     :attr:`ANGULARVEL`                    :struct:`Vector`          In :ref:`SHIP_RAW <ship-raw>`
     :attr:`SENSORS`                       :struct:`VesselSensors`   Sensor data
     :attr:`LOADED`                        Boolean                   loaded into KSP physics engine or "on rails"
     :attr:`LOADDISTANCE`                  :struct:`LoadDistance`    the :struct:`LoadDistance` object for this vessel
     :attr:`ISDEAD`                        Boolean                   True if the vessel refers to a ship that has gone away.
     :attr:`PATCHES`                       :struct:`List`            :struct:`Orbit` patches
     :attr:`ROOTPART`                      :struct:`Part`            Root :struct:`Part` of this vessel
     :attr:`PARTS`                         :struct:`List`            all :struct:`Parts <Part>`
     :attr:`DOCKINGPORTS`                  :struct:`List`            all :struct:`DockingPorts <DockingPort>`
     :attr:`ELEMENTS`                      :struct:`List`            all :struct:`Elements <Element>`
     :attr:`RESOURCES`                     :struct:`List`            all :struct:`AggrgateResources <AggregateResource>`
     :meth:`PARTSNAMED(name)`              :struct:`List`            :struct:`Parts <Part>` by :attr:`NAME <Part:NAME>`
     :meth:`PARTSTITLED(title)`            :struct:`List`            :struct:`Parts <Part>` by :attr:`TITLE <Part:TITLE>`
     :meth:`PARTSTAGGED(tag)`              :struct:`List`            :struct:`Parts <Part>` by :attr:`TAG <Part:TAG>`
     :meth:`PARTSDUBBED(name)`             :struct:`List`            :struct:`Parts <Part>` by :attr:`NAME <Part:NAME>`, :attr:`TITLE <Part:TITLE>` or :attr:`TAG <Part:TAG>`
     :meth:`MODULESNAMED(name)`            :struct:`List`            :struct:`PartModules <PartModule>` by :attr:`NAME <PartModule:NAME>`
     :meth:`PARTSINGROUP(group)`           :struct:`List`            :struct:`Parts <Part>` by action group
     :meth:`MODULESINGROUP(group)`         :struct:`List`            :struct:`PartModules <PartModule>` by action group
     :meth:`ALLPARTSTAGGED()`              :struct:`List`            :struct:`Parts <Part>` that have non-blank nametags
     :attr:`CREWCAPACITY`                  scalar                    Crew capacity of this vessel
     :meth:`CREW()`                        :struct:`List`            all :struct:`CrewMembers <CrewMember>`
    ===================================== ========================= =============

.. note::

    This type is serializable.

.. attribute:: Vessel:CONTROL

    :type: :struct:`Control`
    :access: Get only

    The structure representing the raw flight controls for the vessel.

    WARNING: This suffix is only gettable for :ref:`CPU Vessel <cpu vessel>`

.. attribute:: Vessel:BEARING

    :type: scalar
    :access: Get only

    *relative* compass heading (degrees) to this vessel from the :ref:`CPU Vessel <cpu vessel>`, taking into account the CPU Vessel's own heading.

.. attribute:: Vessel:HEADING

    :type: scalar
    :access: Get only

    *absolute* compass heading (degrees) to this vessel from the :ref:`CPU Vessel <cpu vessel>`

.. attribute:: Vessel:MAXTHRUST

    :type: scalar
    :access: Get only

    Sum of all the :ref:`engines' MAXTHRUSTs <engine_MAXTHRUST>` of all the currently active engines In Kilonewtons.

.. method:: Vessel:MAXTHRUSTAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: scalar (kN)

    Sum of all the :ref:`engines' MAXTHRUSTATs <engine_MAXTHRUSTAT>` of all the currently active engines In Kilonewtons at the given atmospheric pressure.  Use a pressure of 0 for vacuum, and 1 for sea level (on Kerbin).

.. attribute:: Vessel:AVAILABLETHRUST

    :type: scalar
    :access: Get only

    Sum of all the :ref:`engines' AVAILABLETHRUSTs <engine_AVAILABLETHRUST>` of all the currently active engines taking into account their throttlelimits. Result is in Kilonewtons.

.. method:: Vessel:AVAILABLETHRUSTAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: scalar (kN)

    Sum of all the :ref:`engines' AVAILABLETHRUSTATs <engine_AVAILABLETHRUSTAT>` of all the currently active engines taking into account their throttlelimits at the given atmospheric pressure. Result is in Kilonewtons.  Use a pressure of 0 for vacuum, and 1 for sea level (on Kerbin).

.. attribute:: Vessel:FACING

    :type: :struct:`Direction`
    :access: Get only

    The way the vessel is pointed.

.. attribute:: Vessel:MASS

    :type: scalar (metric tons)
    :access: Get only

    The mass of the ship

.. attribute:: Vessel:WETMASS

    :type: scalar (metric tons)
    :access: Get only

    The mass of the ship if all resources were full

.. attribute:: Vessel:DRYMASS

    :type: scalar (metric tons)
    :access: Get only

    The mass of the ship if all resources were empty

.. attribute:: Vessel:DYNAMICPRESSURE

    :type: scalar (ATM's)
    :access: Get only

    Returns what the air pressure is in the atmosphere surrounding the vessel.
    The value is returned in units of sea-level Kerbin atmospheres.  Many
    formulae expect you to plug in a value expressed in kiloPascals, or
    kPA.  You can convert this value into kPa by multiplying it by
    `constant:ATMtokPa`.

.. attribute:: Vessel:Q

    :type: scalar (ATM's)
    :access: Get only

    Alias for DYNAMICPRESSURE

.. attribute:: Vessel:VERTICALSPEED

    :type: scalar (m/s)
    :access: Get only

    How fast the ship is moving. in the "up" direction relative to the SOI Body's sea level surface.

.. attribute:: Vessel:GROUNDSPEED

    :type: scalar (m/s)
    :access: Get only

    How fast the ship is moving in the two dimensional plane horizontal
    to the SOI body's sea level surface.  The vertical component of the
    ship's velocity is ignored when calculating this.

    .. note::

        .. versionadded:: 0.18

        The old name for this value was SURFACESPEED.  The name was changed
        because it was confusing before.  "surface speed" implied it's the
        scalar magnitude of "surface velocity", but it wasn't, because of how
        it ignores the vertical component.


.. attribute:: Vessel:AIRSPEED

    :type: scalar (m/s)
    :access: Get only

    How fast the ship is moving relative to the air. KSP models atmosphere as simply a solid block of air "glued" to the planet surface (the weather on Kerbin is boring and there's no wind). Therefore airspeed is generally the same thing as as the magnitude of the surface velocity.

.. attribute:: Vessel:TERMVELOCITY

    :type: scalar (m/s)
    :access: Get only

    terminal velocity of the vessel in freefall through atmosphere, based on the vessel's current altitude above sea level, and its drag properties. Warning, can cause values of Infinity if used in a vacuum, and kOS sometimes does not let you store Infinity in a variable.

.. attribute:: Vessel:SHIPNAME

    :type: string
    :access: Get/Set

    The name of the vessel as it appears in the tracking station. When you set this, it cannot be empty.

.. attribute:: Vessel:NAME

    Same as :attr:`Vessel:SHIPNAME`.

.. attribute:: Vessel:STATUS

    :type: string
    :access: get only

    The current status of the vessel possible results are: `LANDED`, `SPLASHED`, `PRELAUNCH`, `FLYING`, `SUB_ORBITAL`, `ORBITING`, `ESCAPING` and `DOCKED`.

.. attribute:: Vessel:TYPE

    :type: string
    :access: Get/Set

    The ship's type as described `on the KSP wiki <http://wiki.kerbalspaceprogram.com/wiki/Craft#Vessel_types>`_.

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
    
    .. note::

        .. versionchanged:: 0.15.4

            This has been changed to a vector, as it should have been all along.

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

    :type: Boolean
    :access: Get only

    true if the vessel is fully loaded into the complete KSP physics engine (false if it's "on rails").

.. attribute:: Vessel:LOADDISTANCE

    :type: :struct:`LoadDistance`
    :access: Get only

    Returns the load distance object for this vessel.  The suffixes of this object may be adjusted to change the loading behavior of this vessel. Note: these settings are not persistent across flight instances, and will reset the next time you launch a craft from an editor or the tracking station.

.. attribute:: Vessel:ISDEAD

    :type: Boolean
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

.. attribute:: Vessel:PARTS

    :type: :struct:`List` of :struct:`Part` objects
    :access: Get only

    A List of all the :ref:`parts <part>` on the vessel. ``SET FOO TO SHIP:PARTS.`` has exactly the same effect as ``LIST PARTS IN FOO.``. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

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

    :parameter name: (string) Name of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Part:NAME. The matching is done case-insensitively. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSTITLED(title)

    :parameter title: (string) Title of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Part:TITLE. The matching is done case-insensitively. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSTAGGED(tag)

    :parameter tag: (string) Tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Part:TAG value. The matching is done case-insensitively. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSDUBBED(name)

    :parameter name: (string) name, title or tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    name regardless of whether that name is the Part:Name, the Part:Tag, or the Part:Title. It is effectively the distinct union of :PARTSNAMED(val), :PARTSTITLED(val), :PARTSTAGGED(val). The matching is done case-insensitively. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:MODULESNAMED(name)

    :parameter name: (string) Name of the part modules
    :return: :struct:`List` of :struct:`PartModule` objects

    match the given name. The matching is done case-insensitively. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:PARTSINGROUP(group)

    :parameter group: (integer) the action group number
    :return: :struct:`List` of :struct:`Part` objects

    one action triggered by the given action group. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:MODULESINGROUP(group)

    :parameter group: (integer) the action group number
    :return: :struct:`List` of :struct:`PartModule` objects

    have at least one action triggered by the given action group. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. method:: Vessel:ALLPARTSTAGGED()

    :return: :struct:`List` of :struct:`Part` objects

    nametag on them of any sort that is nonblank. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. attribute:: Vessel:CREWCAPACITY

    :type: scalar
    :access: Get only

    crew capacity of this vessel

.. method:: Vessel:CREW()

    :return: :struct:`List` of :struct:`CrewMember` objects

    list of all :struct:`kerbonauts <CrewMember>` aboard this vessel
