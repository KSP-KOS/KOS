Predictions of Flight Path
==========================

.. contents::
    :local:
    :depth: 1

.. note::

    **Manipulating the maneuver nodes**

    To alter the maneuver nodes on a vessel's flight plan, use the ADD and REMOVE commands as described on the :ref:`maneuver node manipulation page <maneuver node>`.

    Using the Add and Remove commands as described on that page, you may alter the flight plan of the CPU\_vessel, however kOS does not automatically execute the nodes. You still have to write the code to decide how to successfully execute a planned maneuver node.

.. warning::

    Be aware that a limitation of KSP makes it so that some vessels'
    maneuver node systems cannot be accessed.  KSP appears to limit the
    maneuver node system to only functioning on the current PLAYER
    vessel, under the presumption that its the only vessel that needs
    them, as ever other vessel cannot be maneuvered. kOS can maneuver a
    vessel that is not the player vessel, but it cannot overcome this
    limitation of the base game that unloads the maneuver node system
    for other vessels. 

    Be aware that the effect this has is that when you try to predict
    another vessel's position, it will sometimes give you answers that
    presume that other vessel will be purely drifting, and not following
    its maneuver nodes.


The following prediction functions do take into account the future maneuver nodes planned, and operate under the assumption that they will be executed as planned.

These return predicted information about the future position and velocity of an object.

.. function:: POSITIONAT(orbitable,time)

    :param orbitable: A :struct:`Vessel`, :struct:`Body` or other :struct:`Orbitable` object
    :type orbitable:  :struct:`Orbitable`
    :param time:    Time of prediction
    :type time:     :struct:`TimeStamp` or :struct:`Scalar` universal seconds
    :return:        A position :struct:`Vector` expressed as the coordinates in the :ref:`ship-center-raw-rotation <ship-raw>` frame

    Returns a prediction of where the :struct:`Orbitable` will be at some :ref:`universal Time <universal_time>`. If the :struct:`Orbitable` is a :struct:`Vessel`, and the :struct:`Vessel` has planned :ref:`maneuver nodes <maneuver node>`, the prediction assumes they will be executed exactly as planned.

    *Refrence Frame:* The reference frame that the future position
    gets returned in is the same reference frame as the current position
    vectors use.  In other words it's in ship:raw coords where the origin
    is the current ``SHIP``'s center of mass.

    *Prerequisite:*  If you are in a career mode game rather than a
    sandbox mode game, This function requires that you have your space
    center's buildings advanced to the point where you can make maneuver
    nodes on the map view, as described in :struct:`Career:CANMAKENODES`.

.. function:: VELOCITYAT(orbitable,time)

    :param orbitable: A :struct:`Vessel`, :struct:`Body` or other :struct:`Orbitable` object
    :type orbitable:  :struct:`Orbitable`
    :param time:    Time of prediction
    :type time:     :struct:`TimeStamp` or :struct:`Scalar` universal seconds
    :return: An :ref:`ObitalVelocity <orbitablevelocity>` structure.

    Returns a prediction of what the :ref:`Orbitable's <orbitable>` velocity will be at some :ref:`universal Time <universal_time>`. If the :struct:`Orbitable` is a :struct:`Vessel`, and the :struct:`Vessel` has planned :struct:`maneuver nodes <Node>`, the prediction assumes they will be executed exactly as planned.

    *Prerequisite:*  If you are in a career mode game rather than a
    sandbox mode game, This function requires that you have your space
    center's buildings advanced to the point where you can make manuever
    nodes on the map view, as described in :struct:`Career:CANMAKENODES`.

    *Refrence Frame:* The reference frame that the future velocity gets
    returned in is the same reference frame as the current velocity
    vectors use.  In other words it's relative to the ship's CURRENT
    body it's orbiting just like ``ship:velocity`` is.  For example,
    if the ship is currently in orbit of Kerbin, but will be in the Mun's
    SOI in the future, then the ``VELOCITYAT`` that future time will return
    is still returned relative to Kerbin, not the Mun, because that's the
    current reference for current velocities.  Here is an example
    illustrating that::

        // This example imagines you are on an orbit that is leaving
        // the current body and on the way to transfer to another orbit:

        // Later_time is 1 minute into the Mun orbit patch:
        local later_time is time:seconds + ship:obt:NEXTPATCHETA + 60.
        local later_ship_vel is VELOCITYAT(ship, later_time):ORBIT.
        local later_body_vel is VELOCITYAT(ship:obt:NEXTPATCH:body, later_time):ORBIT.

        local later_ship_vel_rel_to_later_body is later_ship_vel - later_body_vel.

        print "My later velocity relative to this body is: " + later_ship_vel.
        print "My later velocity relative to the body I will be around then is: " +
          later_ship_vel_rel_to_later_body.

.. function:: ORBITAT(orbitable,time)

    :param orbitable: A :Ref:`Vessel <vessel>`, :struct:`Body` or other :struct:`Orbitable` object
    :type orbitable:  :struct:`Orbitable`
    :param time:    Time of prediction
    :type time:     :struct:`TimeStamp` or :struct:`Scalar` universal seconds
    :return: An :struct:`Orbit` structure.

    Returns the :ref:`Orbit patch <orbit>` where the :struct:`Orbitable` object is predicted to be at some :ref:`universal Time <universal_time>`. If the :struct:`Orbitable` is a :struct:`Vessel`, and the :struct:`Vessel` has planned :ref:`maneuver nodes <maneuver node>`, the prediction assumes they will be executed exactly as planned.

    *Prerequisite:*  If you are in a career mode game rather than a
    sandbox mode game, This function requires that you have your space
    center's buildings advanced to the point where you can make maneuver
    nodes on the map view, as described in :struct:`Career:CANMAKENODES`.

Examples::

    //kOS
    // test the future position and velocity prediction.
    // Draws a position and velocity vector at a future predicted time.

    declare parameter item. // thing to predict for, i.e. SHIP.
    declare parameter offset. // how much time into the future to predict.
    declare parameter velScale. // how big to draw the velocity vectors.
                  // If they're far from the camera you should draw them bigger.


    set predictUT to time + offset.
    set stopProg to false.

    set futurePos to positionat( item, predictUT ).
    set futureVel to velocityat( item, predictUT ).

    set drawPos to vecdrawargs( v(0,0,0), futurePos, green, "future position", 1, true ).
    set drawVel to vecdrawargs( futurePos, velScale*futureVel:orbit, yellow, "future velocity", 1, true ).

Example Screenshot:

.. figure: /_images/commands/maneuver_nodes.png
    :width: 80 %
