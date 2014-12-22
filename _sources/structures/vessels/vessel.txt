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
     :attr:`FACING`                        :struct:`Direction`       The way the vessel is pointed
     :attr:`MASS`                          scalar (metric tons)      Mass of the ship
     :attr:`VERTICALSPEED`                 scalar (m/s)              How fast the ship is moving "up"
     :attr:`SURFACESPEED`                  scalar (m/s)              How fast the ship is moving "horizontally"
     :attr:`AIRSPEED`                      scalar (m/s)              How fast the ship is moving relative to the air
     :attr:`TERMVELOCITY`                  scalar (m/s)              terminal velocity of the vessel
     :attr:`VESSELNAME`                    string                    The name of the vessel
     :attr:`ANGULARMOMENTUM`               :struct:`Direction`       In :ref:`SHIP_RAW <ship-raw>`
     :attr:`ANGULARVEL`                    :struct:`Direction`       In :ref:`SHIP_RAW <ship-raw>`
     :attr:`SENSORS`                       :struct:`VesselSensors`   Sensor data
     :attr:`LOADED`                        Boolean                   loaded into KSP physics engine or "on rails"
     :attr:`PATCHES`                       :struct:`List`            :struct:`Orbit` patches
     :attr:`ROOTPART`                      :struct:`Part`            Root :struct:`Part` of this vessel
     :attr:`PARTS`                         :struct:`List`            all :struct:`Parts <Part>`
     :meth:`PARTSNAMED(name)`              :struct:`List`            :struct:`Parts <Part>` by :attr:`NAME <Part:NAME>`
     :meth:`PARTSTITLED(title)`            :struct:`List`            :struct:`Parts <Part>` by :attr:`TITLE <Part:TITLE>`
     :meth:`PARTSTAGGED(tag)`              :struct:`List`            :struct:`Parts <Part>` by :attr:`TAG <Part:TAG>`
     :meth:`PARTSDUBBED(name)`             :struct:`List`            :struct:`Parts <Part>` by :attr:`NAME <Part:NAME>`, :attr:`TITLE <Part:TITLE>` or :attr:`TAG <Part:TAG>`
     :meth:`MODULESNAMED(name)`            :struct:`List`            :struct:`PartModules <PartModule>` by :attr:`NAME <PartModule:NAME>`
     :meth:`PARTSINGROUP(group)`           :struct:`List`            :struct:`Parts <Part>` by action group
     :meth:`MODULESINGROUP(group)`         :struct:`List`            :struct:`PartModules <PartModule>` by action group
     :meth:`ALLPARTSTAGGED()`              :struct:`List`            :struct:`Parts <Part>` that have non-blank nametags
    ===================================== ========================= =============

.. attribute:: Vessel:CONTROL

    :type: :struct:`Control`
    :access: Get only

    The structure representing the raw flight controls for the vessel.

.. attribute:: Vessel:BEARING

    :type: scalar
    :access: Get only

    *relative* compass heading (degrees) to this vessel from the `CPU Vessel <../../summary_topics/CPU_Vessel/index.html>`__, taking into account the CPU Vessel's own heading.

.. attribute:: Vessel:HEADING

    :type: scalar
    :access: Get only

    *absolute* compass heading (degrees) to this vessel from the `CPU Vessel <../../summary_topics/CPU_Vessel/index.html>`__

.. attribute:: Vessel:MAXTHRUST

    :type: scalar
    :access: Get only

    Sum of all the Max thrust of all the currently active engines In Kilonewtons.

.. attribute:: Vessel:FACING

    :type: :struct:`Direction`
    :access: Get only

    The way the vessel is pointed.

.. attribute:: Vessel:MASS

    :type: scalar (metric tons)
    :access: Get only

    The mass of the ship

.. attribute:: Vessel:VERTICALSPEED

    :type: scalar (m/s)
    :access: Get only

    How fast the ship is moving. in the "up" direction relative to the SOI Body's sea level surface.

.. attribute:: Vessel:SURFACESPEED

    :type: scalar (m/s)
    :access: Get only

    How fast the ship is moving in the plane horizontal to the SOI body's sea level surface.

.. attribute:: Vessel:AIRSPEED

    :type: scalar (m/s)
    :access: Get only

    How fast the ship is moving relative to the air. KSP models atmosphere as simply a solid block of air "glued" to the planet surface (the weather on Kerbin is boring and there's no wind). Therefore airspeed is generally the same thing as as the magnitude of the surface velocity.

.. attribute:: Vessel:TERMVELOCITY

    :type: scalar (m/s)
    :access: Get only

    terminal velocity of the vessel in freefall through atmosphere, based on the vessel's current altitude above sea level, and its drag properties. Warning, can cause values of Infinity if used in a vacuum, and kOS sometimes does not let you store Infinity in a variable.

.. attribute:: Vessel:VESSELNAME

    :type: string
    :access: Get only

    The name of the vessel as it appears in the tracking station.

.. attribute:: Vessel:ANGULARMOMENTUM

    :type: :struct:`Direction`
    :access: Get only

    Given in [SHIP-RAW reference frame]](../../ref\_frame/index.html). Despite the name, this is technically not momentum information because it has no magnitude.

.. attribute:: Vessel:ANGULARVEL

    :type: :struct:`Direction`
    :access: Get only

    Given in [SHIP-RAW reference frame]](../../ref\_frame/index.html). Despite the name, this is technically not a velocity. It only tells you the axis of rotation, not the speed of rotation around that axis.

.. attribute:: Vessel:SENSORS

    :type: :struct:`VesselSensors`
    :access: Get only

    Structure holding summary information of sensor data for the vessel

.. attribute:: Vessel:LOADED

    :type: Boolean
    :access: Get only

    true if the vessel is fully loaded into the complete KSP physics engine (false if it's "on rails").

.. attribute:: Vessel:PATCHES

    :type: :struct:`List`
    :access: Get only

    The list of `orbit patches <../orbit/index.html>`__ that describe this vessel's current travel path based on momentum alone with no thrusting changes. If the current path has no transitions to other bodies, then this will be a list of only one orbit. If the current path intersects other bodies, then this will be a list describing the transitions into and out of the intersecting body's sphere of influence. SHIP:PATCHES[0] is always exactly the same as SHIP:OBT, SHIP:PATCHES[1] is the same as SHIP:OBT:NEXTPATCH, SHIP:PATCHES[2] is the same as SHIP:OBT:NEXTPATCH:NEXTPATCH, and so on. Note that you will only see as far into the future as your KSP settings allow. (See the setting CONIC\_PATCH\_LIMIT in your settings.cfg file)

.. attribute:: Vessel:ROOTPART

    :type: :struct:`Part`
    :access: Get only

    The first `part <../part/index.html>`__ that was used to begin the ship design - the command core. Vessels in KSP are built in a tree-structure, and the first part that was placed is the root of that tree.

.. attribute:: Vessel:PARTS

    :type: :struct:`List` of :struct:`Part` objects
    :access: Get only

    A List of all the `parts <../part/index.html>`__ on the vessel. SET FOO TO SHIP:PARTS has exactly the same effect as LIST PARTS IN FOO. For more information, see `ship parts and modules <../../summary_topics/ship_parts_and_modules/index.html>`__


.. method:: Vessel:PARTSNAMED(name)

    :parameter name: (string) Name of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Part:NAME. The matching is done case-insensitively. For more information, see `ship parts and modules <../../summary_topics/ship_parts_and_modules/index.html>`__

.. method:: Vessel:PARTSTITLED(title)

    :parameter title: (string) Title of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Part:TITLE. The matching is done case-insensitively. For more information, see `ship parts and modules <../../summary_topics/ship_parts_and_modules/index.html>`__

.. method:: Vessel:PARTSTAGGED(tag)

    :parameter tag: (string) Tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Part:TAG value. The matching is done case-insensitively. For more information, see `ship parts and modules <../../summary_topics/ship_parts_and_modules/index.html>`__

.. method:: Vessel:PARTSDUBBED(name)

    :parameter name: (string) name, title or tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    name regardless of whether that name is the Part:Name, the Part:Tag, or the Part:Title. It is effectively the distinct union of :PARTSNAMED(val), :PARTSTITLED(val), :PARTSTAGGED(val). The matching is done case-insensitively. For more information, see `ship parts and modules <../../summary_topics/ship_parts_and_modules/index.html>`__

.. method:: Vessel:MODULESNAMED(name)

    :parameter name: (string) Name of the part modules
    :return: :struct:`List` of :struct:`PartModule` objects

    match the given name. The matching is done case-insensitively. For more information, see `ship parts and modules <../../summary_topics/ship_parts_and_modules/index.html>`__

.. method:: Vessel:PARTSINGROUP(group)

    :parameter group: (integer) the action group number
    :return: :struct:`List` of :struct:`Part` objects

    one action triggered by the given action group. For more information, see `ship parts and modules <../../summary_topics/ship_parts_and_modules/index.html>`__

.. method:: Vessel:MODULESINGROUP(group)

    :parameter group: (integer) the action group number
    :return: :struct:`List` of :struct:`PartModule` objects

    have at least one action triggered by the given action group. For more information, see `ship parts and modules <../../summary_topics/ship_parts_and_modules/index.html>`__

.. method:: Vessel:ALLPARTSTAGGED()

    :return: :struct:`List` of :struct:`Part` objects

    nametag on them of any sort that is nonblank. For more information, see `ship parts and modules <../../summary_topics/ship_parts_and_modules/index.html>`__
