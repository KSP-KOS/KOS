.. _maneuver node:

Maneuver Node
=============

*Contents*

    - :func:`NODE()`
    - :global:`ADD`
    - :global:`REMOVE`
    - :global:`NEXTNODE`
    - :global:`HASNODE`
    - :struct:`ManeuverNode`

A planned velocity change along an orbit. These are the nodes that you can set in the KSP user interface. Setting one through kOS will make it appear on the in-game map view, and creating one manually on the in-game map view will cause it to be visible to kOS.

.. warning::
    Be aware that a limitation of KSP makes it so that some vessels'
    maneuver node systems cannot be accessed.  KSP appears to limit the
    maneuver node system to only functioning on the current PLAYER
    vessel, under the presumption that its the only vessel that needs
    them, as ever other vessel cannot be maneuvered. kOS can maneuver a
    vessel that is not the player vessel, but it cannot overcome this
    limitation of the base game that unloads the maneuver node system
    for other vessels.

    Be aware that the effect this has is that when you try to use some of
    these commands on some vessels, they won't work because those vessels
    do not have their maneuver node system in play.  This is mostly only
    going to happen when you try to run a script on a vessel that is not
    the current player active vessel.


Creation
--------

.. function:: NODE(time, radial, normal, prograde)

    :parameter time: :ref:`TimeSpan` (ETA), :ref:`TimeStamp` (UT), or :ref:`Scalar` (UT)
    :parameter radial: (m/s) Delta-V in radial-out direction
    :parameter normal: (m/s) Delta-V normal to orbital plane
    :parameter prograde: (m/s) Delta-V in prograde direction
    :returns: :struct:`ManeuverNode`

    You can make a maneuver node in a variable using the :func:`NODE` function.
    The radial, normal, and prograde parameters represent the 3 axes you can
    adjust on the manuever node.  The time parameter represents when the node
    is along a vessel's path.  The time parameter has two different possible
    meanings depending on what kind of value you pass in for it.  It's either
    an absolute time since the game started, or it's a relative time (ETA)
    from now, according to the following rule:

    Using a TimeSpan for time means it's an ETA time offset
    relative to right now at the moment you called this function::

        // Example: This makes a node 2 minutes and 30 seconds from now:
        SET myNode to NODE( TimeSpan(0, 0, 0, 2, 30), 0, 50, 10 ).
        // Example: This also makes a node 2 minutes and 30 seconds from now,
        // but does it by total seconds (2*60 + 30 = 150):
        SET myNode to NODE( TimeSpan(150), 0, 50, 10 ).

     Using a TimeStamp, or a Scalar number of seconds for time means
     it's a time expressed in absolute universal time since game
     start::

        // Example: A node at: year 5, day 23, hour 1, minute 30, second zero:
        SET myNode to NODE( TimeStamp(5,23,1,30,0), 0, 50, 10 ).

        // Using a Scalar number of seconds for time also means it's
        // a time expressed in absolute universal time (seconds since
        // epoch):
        // Example: A node exactly one hour (3600 seconds) after the
        // campaign started:
        SET myNode to NODE( 3600, 0, 50, 10 ).

     Either way, once you have a maneuver node in a variable, you use the :global:`ADD` and :global:`REMOVE` commands to attach it to your vessel's flight plan. A kOS CPU can only manipulate the flight plan of its :ref:`CPU vessel <cpu vessel>`.

    Once you have created a node, it's just a hypothetical node that hasn't
    been attached to anything yet. To attach a node to the flight path, you must use the command :global:`ADD` to attach it to the ship.

.. global:: ADD

    To put a maneuver node into the flight plan of the current :ref:`CPU vessel <cpu vessel>` (i.e. ``SHIP``), just :global:`ADD` it like so::

        SET myNode to NODE( TIME:SECONDS+200, 0, 50, 10 ).
        ADD myNode.

    You should immediately see it appear on the map view when you do this. The :global:`ADD` command can add nodes anywhere within the flight plan. To insert a node earlier in the flight than an existing node, simply give it a smaller :attr:`ETA <ManeuverNode:ETA>` time and then :global:`ADD` it.

    .. warning::
        As per the warning above at the top of the section, ADD won't work on vessels that are not the active vessel.

.. global:: REMOVE

    To remove a maneuver node from the flight path of the current :ref:`CPU vessel <cpu vessel>` (i.e. ``SHIP``), just :global:`REMOVE` it like so::

        REMOVE myNode.

    .. warning::
        As per the warning above at the top of the section, REMOVE won't work on vessels that are not the active vessel.

.. global:: NEXTNODE

    :global:`NEXTNODE` is a built-in variable that always refers to the next upcoming node that has been added to your flight plan::

        SET MyNode to NEXTNODE.
        PRINT NEXTNODE:PROGRADE.
        REMOVE NEXTNODE.

    Currently, if you attempt to query :global:`NEXTNODE` and there is no node on your flight plan, it produces a run-time error. (This needs to be fixed in a future release so it is possible to query whether or not you have a next node).

    .. warning::
        As per the warning above at the top of the section, NEXTNODE won't work on vessels that are not the active vessel.

    The special identifier :global:`NEXTNODE` is a euphemism for "whichever node is coming up soonest on my flight path". Therefore you can remove a node even if you no longer have the maneuver node variable around, by doing this::

        REMOVE NEXTNODE.

.. global:: HASNODE

    :type: :struct:`Boolean`
    :access: Get only

    Returns true if there is a planned maneuver :struct:`ManeuverNode` in the
    :ref:`CPU vessel's <cpu vessel>` flight plan.  This will always return
    false for the non-active vessel, as access to maneuver nodes is limited to the active vessel.

.. global:: ALLNODES

    :type: :struct:`List` of :struct:`ManeuverNode` elements
    :access: Get only

    Returns a list of all :struct:`ManeuverNode` objects currently on the
    :ref:`CPU vessel's <cpu vessel>` flight plan.  This list will be empty if
    no nodes are planned, or if the :ref:`CPU vessel <cpu vessel>` is currently
    unable to use maneuver nodes.

    .. note::
        If you store a reference to this list in a variable, the variable's
        instance will not be automatically updated if you :global:`ADD` or
        :global:`REMOVE` maneuver nodes to the flight plan.

    .. note::
        Adding a :struct:`ManeuverNode` to this list, or a reference to this
        list **will not** add it to the flight plan.  Use the :global:`ADD`
        command instead.

Structure
---------

.. structure:: ManeuverNode


    Here are some examples of accessing the suffixes of a :struct:`ManeuverNode`::

        // creates a node 60 seconds from now with
        // prograde = 100 m/s
        SET X TO NODE(TIME:SECONDS+60, 0, 0, 100).

        ADD X.            // adds maneuver to flight plan

        PRINT X:PROGRADE. // prints 100.
        PRINT X:ETA.      // prints seconds till maneuver
        PRINT X:TIME.     // prints exact UT time of manuever
        PRINT X:DELTAV    // prints delta-v vector

        REMOVE X.         // remove node from flight plan

        // Create a blank node
        SET X TO NODE(0, 0, 0, 0).

        ADD X.                 // add Node to flight plan
        SET X:PROGRADE to 500. // set prograde dV to 500 m/s
        SET X:ETA to 30.       // Set to 30 sec from now

        PRINT X:ORBIT:APOAPSIS.  // apoapsis after maneuver
        PRINT X:ORBIT:PERIAPSIS. // periapsis after maneuver


    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 1 2

        * - Suffix
          - Type (units)
          - Access
          - Description

        * - :attr:`DELTAV`
          - :struct:`Vector` (m/s)
          - Get only
          - The burn vector with magnitude equal to delta-V
        * - :attr:`BURNVECTOR`
          - :struct:`Vector` (m/s)
          - Get only
          - Alias for :attr:`DELTAV`
        * - :attr:`ETA`
          - :ref:`scalar <scalar>` (s)
          - Get/Set
          - Time until this maneuver
        * - :attr:`TIME`
          - :ref:`scalar <scalar>` (s)
          - Get/Set
          - Universal Time of this maneuver
        * - :attr:`PROGRADE`
          - :ref:`scalar <scalar>` (m/s)
          - Get/Set
          - Delta-V along prograde
        * - :attr:`RADIALOUT`
          - :ref:`scalar <scalar>` (m/s)
          - Get/Set
          - Delta-V along radial to orbited :struct:`Body`
        * - :attr:`NORMAL`
          - :ref:`scalar <scalar>` (m/s)
          - Get/Set
          - Delta-V along normal to the :struct:`Vessel`'s :struct:`Orbit`
        * - :attr:`ORBIT`
          - :struct:`Orbit`
          - Get only
          - Expected :struct:`Orbit` after this maneuver


.. attribute:: ManeuverNode:DELTAV

    :access: Get only
    :type: :struct:`Vector`

    The vector giving the total burn of the node. The vector can be used to steer with, and its magnitude is the delta V of the burn.

.. attribute:: ManeuverNode:BURNVECTOR

    Alias for :attr:`ManeuverNode:DELTAV`.

.. attribute:: ManeuverNode:ETA

    :access: Get/Set
    :type: :ref:`scalar <scalar>`

    The number of seconds until the expected burn time. If you SET this, it will actually move the maneuver node along the path in the map view, identically to grabbing the maneuver node and dragging it.

.. attribute:: ManeuverNode:TIME

    :access: Get/Set
    :type: :ref:`scalar <scalar>`

    The time of the node in universal time, rather than ETA relative to the current
    time.  This should be the same as adding :attr:`ManeuverNode:ETA` to ``TIME:SECONDS``.

.. attribute:: ManeuverNode:PROGRADE

    :access: Get/Set
    :type: :ref:`scalar <scalar>`

    The delta V in (meters/s) along just the prograde direction (the yellow and green 'knobs' of the maneuver node). A positive value is a prograde burn and a negative value is a retrograde burn.

.. attribute:: ManeuverNode:RADIALOUT

    :access: Get/Set
    :type: :ref:`scalar <scalar>`

    The delta V in (meters/s) along just the radial direction (the cyan knobs' of the maneuver node). A positive value is a radial out burn and a negative value is a radial in burn.

.. attribute:: ManeuverNode:NORMAL

    :access: Get/Set
    :type: :ref:`scalar <scalar>`

    The delta V in (meters/s) along just the normal direction (the purple knobs' of the maneuver node). A positive value is a normal burn and a negative value is an anti-normal burn.

.. attribute:: ManeuverNode:ORBIT

    :access: Get only
    :type: :struct:`Orbit`

    The new orbit patch that will begin starting with the burn of this node, under the assumption that the burn will occur exactly as planned.
