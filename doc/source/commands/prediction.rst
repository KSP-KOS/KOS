Predictions of Flight Path
==========================

.. contents::
    :local:
    :depth: 1

.. note::

    **Manipulating the maneuver nodes**

    To alter the maneuver nodes on a vessel's flight plan, use the ADD and REMOVE commands as described on the :ref:`maneuver node manipulation page <node>`.

    Using the Add and Remove commands as described on that page, you may alter the flight plan of the CPU\_vessel, however kOS does not automatically execute the nodes. You still have to write the code to decide how to successfully execute a planned maneuver node.

The following prediction functions do take into account the future maneuver nodes planned, and operate under the assumption that they will be executed as planned.

These return predicted information about the future position and velocity of an object.

.. function:: POSITIONAT(orbitable,time)

    :param orbitable: A :Ref:`Vessel <vessel>`, :ref:`Body <body>` or other :ref:`Orbitable <orbitable>` object
    :type orbitable:  :ref:`Orbitable <orbitable>`
    :param time:    Time of prediction
    :type time:     :ref:`Timestamp <timestamp>`
    :return:        A position :ref:`Vector <vector>` expressed as the coordinates in the :ref:`ship-center-raw-rotation <ship-raw>` frame

    Returns a prediction of where the :ref:`Orbitable <orbitable>` will be at some :ref:`universal Timestamp <timestamp>`. If the :ref:`Orbitable <orbitable>` is a :ref:`Vessel <vessel>`, and the :ref:`Vessel <vessel>` has planned :ref:`maneuver nodes <node>`, the prediction assumes they will be executed exactly as planned.

.. function:: VELOCITYAT(orbitable,time)

    :param orbitable: A :Ref:`Vessel <vessel>`, :ref:`Body <body>` or other :ref:`Orbitable <orbitable>` object
    :type orbitable:  :ref:`Orbitable <orbitable>`
    :param time:    Time of prediction
    :type time:     :ref:`Timestamp <timestamp>`
    :return: An :ref:`ObitalVelocity <orbitablevelocity>` structure.

    Returns a prediction of what the :ref:`Orbitable's <orbitable>` velocity will be at some :ref:`universal Timestamp <timestamp>`. If the :ref:`Orbitable <orbitable>` is a :ref:`Vessel <vessel>`, and the :ref:`Vessel <vessel>` has planned :ref:`maneuver nodes <node>`, the prediction assumes they will be executed exactly as planned.

.. function:: ORBITAT(orbitable,time)

    :param orbitable: A :Ref:`Vessel <vessel>`, :ref:`Body <body>` or other :ref:`Orbitable <orbitable>` object
    :type orbitable:  :ref:`Orbitable <orbitable>`
    :param time:    Time of prediction
    :type time:     :ref:`Timestamp <timestamp>`
    :return: An :ref:`Orbit <orbit>` structure.
        
    Returns the :ref:`Orbit patch <orbit>` where the :ref:`Orbitable <orbitable>` object is predicted to be at some :ref:`universal Timestamp <timestamp>`. If the :ref:`Orbitable <orbitable>` is a :ref:`Vessel <vessel>`, and the :ref:`Vessel <vessel>` has planned :ref:`maneuver nodes <node>`, the prediction assumes they will be executed exactly as planned.

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
