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
    :type time:     :struct:`TimeSpan`
    :return:        A position :struct:`Vector` expressed as the coordinates in the :ref:`ship-center-raw-rotation <ship-raw>` frame

    Returns a prediction of where the :struct:`Orbitable` will be at some :ref:`universal Timestamp <timestamp>`. If the :struct:`Orbitable` is a :struct:`Vessel`, and the :struct:`Vessel` has planned :ref:`maneuver nodes <maneuver node>`, the prediction assumes they will be executed exactly as planned.

    *Prerequisite:*  If you are in a career mode game rather than a
    sandbox mode game, This function requires that you have your space
    center's buildings advanced to the point where you can make maneuver
    nodes on the map view, as described in :struct:`Career:CANMAKENODES`.

.. function:: VELOCITYAT(orbitable,time)

    :param orbitable: A :struct:`Vessel`, :struct:`Body` or other :struct:`Orbitable` object
    :type orbitable:  :struct:`Orbitable`
    :param time:    Time of prediction
    :type time:     :struct:`TimeSpan`
    :return: An :ref:`ObitalVelocity <orbitablevelocity>` structure.

    Returns a prediction of what the :ref:`Orbitable's <orbitable>` velocity will be at some :ref:`universal Timestamp <timestamp>`. If the :struct:`Orbitable` is a :struct:`Vessel`, and the :struct:`Vessel` has planned :struct:`maneuver nodes <Node>`, the prediction assumes they will be executed exactly as planned.

    *Prerequisite:*  If you are in a career mode game rather than a
    sandbox mode game, This function requires that you have your space
    center's buildings advanced to the point where you can make manuever
    nodes on the map view, as described in :struct:`Career:CANMAKENODES`.

.. function:: ORBITAT(orbitable,time)

    :param orbitable: A :Ref:`Vessel <vessel>`, :struct:`Body` or other :struct:`Orbitable` object
    :type orbitable:  :struct:`Orbitable`
    :param time:    Time of prediction
    :type time:     :struct:`TimeSpan`
    :return: An :struct:`Orbit` structure.

    Returns the :ref:`Orbit patch <orbit>` where the :struct:`Orbitable` object is predicted to be at some :ref:`universal Timestamp <timestamp>`. If the :struct:`Orbitable` is a :struct:`Vessel`, and the :struct:`Vessel` has planned :ref:`maneuver nodes <maneuver node>`, the prediction assumes they will be executed exactly as planned.

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
