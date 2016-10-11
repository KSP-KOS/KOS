.. _ref frame:

Reference Frames
================

This page describes the :math:`(x,y,z)` reference frame used for most
of **kOS**'s vectors. kOS inherits its reference frame mostly from the
base Kerbal Space Program game itself.  The coordinate system of Kerbal
Space Program does some strange things that don't make a lot of sense
at first.

.. For nomenclature, the following terms are used in this documentation:

.. _left-handed:

.. note::
    Be aware that Kerbal Space program (and in fact many of the games
    based on the Unity game engine) uses a **LEFT-handed** coordinate
    system.  kOS inherits this behavior from KSP.

In all the reference frames mentioned below, the orientation of the axes is
**left-handed**. What does that mean? If you open your **left** palm and point
your fingers along the x-axis, then curl your fingers in the direction of the
y-axis and stick out your thumb, your thumb will be pointing along the z-axis.
(If you do those steps with your right hand, you will get a z-axis in the
opposite direction and that is known as a **right handed** coordinate system).

This is an important thing to keep in mind, as most mathematics
and physics textbooks tend to draw examples using a right handed
coordinate system, and most students become familiar with that
convention first.  But for a variety of reasons, many computer
graphics systems have a tradition of using left-handed systems
instead, and Kerbal Space Program is one of them.


.. _ship-raw:
.. index:: SHIP-RAW (Reference Frame)
.. object:: SHIP-RAW

    The name of the reference frame in which the origin point is :ref:`CPU Vessel`, and the rotation is identical to **KSP**'s native raw coordinate grid.

.. _soi-raw:
.. index:: SOI-RAW (Reference Frame)
.. object:: SOI-RAW

    The name of the reference frame in which the origin point is the center of the *SOI body*, and the rotation is identical to **KSP**'s native raw coordinate grid.

.. _raw-raw:
.. index:: RAW-RAW (Reference Frame)
.. object:: RAW-RAW

    The name of the reference frame in which both the origin point and the rotation of the axes is identical to **KSP**'s native raw coordinate grid. This is never exposed to the **KerbalScript** program, because the origin point is meaningless to work with.

.. note::
    It is hoped that this may be expanded in the future, and conversion routines provided to let people pick a reference frame that makes sense depending on what the script is trying to do. At the moment the only reference frames used are SHIP-RAW and SOI-RAW, as they match closely to what **KSP** is using internally.

Raw Orientation
----------------

.. figure:: /_images/reference/math/KSP_body_coords.png

The Y axis of **KSP** is the only consistent thing. Imagine a ray starting in the center of the SOI body and pointing upward out through the north pole. That is the direction of the Y axis. (If you move to the SOI of a body with an inclined spin, presumably it will also change the angle of the Y axis to point in the new direction of the body's spin axis).

The X and Z axes of the coordinate grid are then consequently aligned with the equator plane of the SOI body, 90 degrees to each other. **KSP** uses a left-handed coordinate system, so the Z axis will always be rotated 90 degrees to the east of the X axis.

.. figure:: /_images/reference/math/KSP_body_latlong.png

However, it's hard to predict exactly where the X and Z axes will be. They keep moving depending on where you are, to the point where it's impossible to get a fix on just which direction they'll point.

Origin Position
---------------

The origin position of the :math:`(x,y,z)` coordinate grid in **KSP** is also a bit messy. It's usually *near* but not exactly *on* the current ship. **kOS** performs some conversions for you to make this a bit simpler and keep everything consistent.

Regardless of where the origin of the underlying **KSP** system is, in **kOS**, whenever a POSITION is reported, it will always be reported in a frame of reference where the origin is located at the :ref:`CPU Vessel <cpu vessel>`.

However, for the sake of VELOCITY, the origin point of all vectors is usually not SHIP, but the SOI body's center. This is because if the origin point was the SHIP, then the ship's velocity would always be zero in that frame of reference, and that would not be useful.

(The makers of **kOS** are aware that this is not technically a proper frame of reference, because the origin point varies depending on if you're getting POSITION or getting VELOCITY. Fixing it at this point would break a lot of existing scripts, however.)

So the rule of thumb is:

-  For POSITION returned by **KSP**, the SHIP-RAW reference frame is used: centered on SHIP, with raw axes rotation.
-  For VELOCITY returned by **KSP**, the SOI-RAW reference frame is used: centered on SOI Body, with raw axes rotation.

Converting
----------

Converting between SHIP-RAW and SOI-RAW reference frames is a simple matter of moving the origin point by adding or subtracting the ``SHIP:BODY:POSITION`` vector from the coordinate. This works because both frames are using the same axes rotation.

-  Any SHIP-RAW position vector *minus* ``SHIP:BODY:POSITION`` Gives the vector in SOI-RAW coordinates.
-  Any SOI-RAW position vector *plus* ``SHIP:BODY:POSITION`` Gives the vector in SHIP-RAW coordinates.
