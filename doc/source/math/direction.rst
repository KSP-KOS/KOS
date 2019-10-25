.. _direction:

Directions
==========

.. contents:: Contents
    :local:
    :depth: 1

:struct:`Direction` objects represent a rotation starting from an initial point in **KSP**'s coordinate system where the initial state was looking down the :math:`+z` axis, with the camera "up" being the :math:`+y` axis. This exists primarily to enable automated steering.

In your thinking, you can largely think of Directions as being Rotations and Rotations as being Directions.  The two concepts can be used interchangeably.  Used on its own to steer by, a rotation from the default XYZ axes of the universe into a new rotation does in fact provide an absolute direction, thus the name Direction for these objects even though in reality they are just Rotations.  It's important to know that Directions are just rotations because you can use them to modify other directions or vectors.

.. note::
    When dealing with Directions (which are Rotations) in kOS, it is
    important to remember that KSP uses a :ref:`left-handed <left-handed>`
    coordinate system.  This affects the convention of which rotation
    direction is positive when calculating angles.

Creation
--------

=============================================== ===================================
 Method                                          Description
=============================================== ===================================
 :func:`R(pitch,yaw,roll)`                       Euler rotation
 :func:`Q(x,y,z,rot)`                            Quaternion
 :func:`HEADING(dir,pitch,roll)`                 Compass heading
 :func:`LOOKDIRUP(lookAt,lookUp)`                Looking along vector *lookAt*, rolled so that *lookUp* is upward.
 :func:`ANGLEAXIS(degrees,axisVector)`           A rotation that would rotate the universe around an axis
 :func:`ROTATEFROMTO(fromVec,toVec)`             A rotation that would go from vectors fromVec to toVec
 :ref:`FACING         <Direction from suffix>`   From SHIP or TARGET
 :ref:`UP             <Direction from suffix>`   From SHIP
 :ref:`PROGRADE, etc. <Direction from suffix>`   From SHIP, TARGET or BODY
=============================================== ===================================

.. _rotation:
.. function:: R(pitch,yaw,roll)

    A :struct:`Direction` can be created out of a Euler Rotation, indicated with the :func:`R()` function, as shown below where the ``pitch``, ``yaw`` and ``roll`` values are in degrees::

        SET myDir TO R( a, b, c ).

.. function:: Q(x,y,z,rot)

    A :struct:`Direction` can also be created out of a *Quaternion* tuple,
    indicated with the :func:`Q()` function, passing it the x, y, z, w
    values of the Quaternion.
    `The concept of a Quaternion <https://en.wikipedia.org/wiki/Quaternions_and_spatial_rotation>`__
    uses complex numbers and is beyond the scope of the kOS
    documentation, which is meant to be simple to understand.  It is
    best to not use the Q() function unless Quaternions are something
    you already understand.

    ::

        SET myDir TO Q( x, y, z, w ).

.. _heading:
.. function:: HEADING(dir,pitch,roll)

    A :struct:`Direction` can be created out of a :func:`HEADING()` function. The first parameter is the compass heading, and the second parameter is the pitch above the horizon::

        SET myDir TO HEADING(degreesFromNorth, pitchAboveHorizon).

    The third parameter, *roll*, is optional. Roll indicates rotation about the longitudinal axis.

.. function:: LOOKDIRUP(lookAt,lookUp)

    A :struct:`Direction` can be created with the LOOKDIRUP function by using two vectors.   This is like converting a vector to a direction directly, except that it also provides roll information, which a single vector lacks.   *lookAt* is a vector describing the Direction's FORE orientation (its local Z axis), and *lookUp* is a vector describing the direction's TOP orientation (its local Y axis).  Note that *lookAt* and *lookUp* need not actually be perpendicualr to each other - they just need to be non-parallel in some way.  When they are not perpendicular, then a vector resulting from projecting *lookUp* into the plane that is normal to *lookAt* will be used as the effective *lookUp* instead::

        // Aim up the SOI's north axis (V(0,1,0)), rolling the roof to point to the sun.
        LOCK STEERING TO LOOKDIRUP( V(0,1,0), SUN:POSITION ).
        //
        // A direction that aims normal to orbit, with the roof pointed down toward the planet:
        LOCK normVec to VCRS(SHIP:BODY:POSITION,SHIP:VELOCITY:ORBIT).  // Cross-product these for a normal vector
        LOCK STEERING TO LOOKDIRUP( normVec, SHIP:BODY:POSITION).

.. function:: ANGLEAXIS(degrees,axisVector)

    A :struct:`Direction` can be created with the ANGLEAXIS function.  It represents a rotation of *degrees* around an axis of *axisVector*.  To know which way a positive or negative number of degrees rotates, remember this is a left-handed coordinate system::

        // Pick a new rotation that is pitched 30 degrees from the current one, taking into account
        // the ship's current orientation to decide which direction is the 'pitch' rotation:
        //
        SET pitchUp30 to ANGLEAXIS(-30,SHIP:STARFACING).
        SET newDir to pitchUp30*SHIP:FACING.
        LOCK STEERING TO newDir.

.. note::
    The fact that KSP is using a :ref:`left-handed <left-handed>`
    coordinate system is important to keep in mind when visualizing
    the meaning of an ANGLEAXIS function call.  It affects which
    direction is positive when calculating angles.

.. function:: ROTATEFROMTO(fromVec,toVec)

    A :struct:`Direction` can be created with the ``ROTATEFROMTO`` function.  It is *one of the infinite number of* rotations that could rotate vector *fromVec* to become vector *toVec* (or at least pointing in the same direction as toVec, since fromVec and toVec need not be the same magnitude).  Note the use of the phrase "**infinite number of**".  Because there's no guarantee about the roll information, there are an infinite number of rotations that could qualify as getting you from one vector to another, because there's an infinite number of roll angles that could result and all still fit the requirement::

        SET myDir to ROTATEFROMTO( v1, v2 ).

.. _Direction from suffix:
.. object:: Suffix terms from other structures

    A :struct:`Direction` can be made from many suffix terms of other structures, as shown below::

        SET myDir TO SHIP:FACING.
        SET myDir TO TARGET:FACING.
        SET myDir TO SHIP:UP.

Whenever a :struct:`Direction` is printed, it always comes out showing its Euler Rotation, regardless of how it was created::

    // Initializes a direction to prograde
    // plus a relative pitch of 90
    SET X TO SHIP:PROGRADE + R(90,0,0).

    // Steer the vessel in the direction
    // suggested by direction X.
    LOCK STEERING TO X.

    // Create a rotation facing northeast,
    // 10 degrees above horizon
    SET Y TO HEADING(45, 10).

    // Steer the vessel in the direction
    // suggested by direction X.
    LOCK STEERING TO Y.

    // Set by a rotation in degrees
    SET Direction TO R(0,90,0).

Structure
---------

.. structure:: Direction

    The suffixes of a :struct:`Direction` cannot be altered, so to get a new :struct:`Direction` you must construct a new one.

    ========================= ======================= ================================
     Suffix                   Type                    Description
    ========================= ======================= ================================
     :attr:`PITCH`            :struct:`scalar` (deg)  Rotation around :math:`x` axis
     :attr:`YAW`              :struct:`scalar` (deg)  Rotation around :math:`y` axis
     :attr:`ROLL`             :struct:`scalar` (deg)  Rotation around :math:`z` axis
     :attr:`FOREVECTOR`       :struct:`Vector`        This Direction's forward vector (z axis after rotation).
     ``VECTOR``               :struct:`Vector`        Alias synonym for :attr:`FOREVECTOR`
     :attr:`TOPVECTOR`        :struct:`Vector`        This Direction's top vector (y axis after rotation).
     ``UPVECTOR``             :struct:`Vector`        Alias synonym for :attr:`TOPVECTOR`
     :attr:`STARVECTOR`       :struct:`Vector`        This Direction's starboard vector (z axis after rotation).
     ``RIGHTVECTOR``          :struct:`Vector`        Alias synonym for :attr:`STARVECTOR`
     :attr:`INVERSE`          :struct:`Direction`     The inverse of this direction.
     ``-`` (unary minus)      :struct:`Direction`     Using the negation operator ``-`` on a Direction does the same thing as using the :INVERSE suffix on it.
    ========================= ======================= ================================

    The :struct:`Direction` object exists primarily to enable automated steering. You can initialize a :struct:`Direction` using a :struct:`Vector` or a ``Rotation``. :struct:`Direction` objects represent a rotation starting from an initial point in **KSP**'s coordinate system where the initial state was looking down the :math:`+z` axis, with the camera "up" being the :math:`+y` axis. So for example, a :struct:`Direction` pointing along the :math:`x` axis might be represented as ``R(0,90,0)``, meaning the initial :math:`z`-axis direction was rotated *90 degrees* around the :math:`y` axis.

    If you are going to manipulate directions a lot, it's important to note that the order in which the rotations occur is:

    1. First rotate around :math:`z` axis.
    2. Then rotate around :math:`x` axis.
    3. Then rotate around :math:`y` axis.

    What this means is that if you try to ``ROLL`` and ``YAW`` in the same tuple, like so: ``R(0,45,45)``, you'll end up **rolling first and then yawing**, which might not be what you expected. There is little that can be done to change this as it's the native way things are represented in the underlying **Unity engine**.

    Also, if you are going to manipulate directions a lot, it's important to note how **KSP**'s `native coordinate system works <ref_frame>`_.

.. attribute:: Direction:PITCH

    :type: :struct:`Scalar` (deg)
    :access: Get only


    Rotation around the :math:`x` axis.

.. attribute:: Direction:YAW

    :type: :struct:`Scalar` (deg)
    :access: Get only

    Rotation around the :math:`y` axis.

.. attribute:: Direction:ROLL

    :type: :struct:`Scalar` (deg)
    :access: Get only


    Rotation around the :math:`z` axis.

.. attribute:: Direction:FOREVECTOR

    :type: :struct:`Vector`
    :access: Get only

    :struct:`Vector` of length 1 that is in the same direction as the "look-at" of this Direction.  Note that it is the same meaning as "what the Z axis of the universe would be rotated to if this rotation was applied to the basis axes of the universe".  When you LOCK STEERING to a direction, that direction's FOREVECTOR is the vector the nose of the ship will orient to.  SHIP:FACING:FOREVECTOR is the way the ship's nose is aimed right now.

.. attribute:: Direction:TOPVECTOR

    :type: :struct:`Vector`
    :access: Get only

    :struct:`Vector` of length 1 that is in the same direction as the "look-up" of this Direction.  Note that it is the same meaning as "what the Y axis of the universe would be rotated to if this rotation was applied to the basis axes of the universe". When you LOCK STEERING to a direction, that direction's TOPVECTOR is the vector the roof of the ship will orient to.  SHIP:FACING:TOPVECTOR is the way the ship's roof is aimed right now.

.. attribute:: Direction:STARVECTOR

    :type: :struct:`Vector`
    :access: Get only

    :struct:`Vector` of length 1 that is in the same direction as the "starboard side" of this Direction.  Note that it is the same meaning as "what the X axis of the universe would be rotated to if this rotation was applied to the basis axes of the universe". When you LOCK STEERING to a direction, that direction's STARVECTOR is the vector the right wing of the ship will orient to.  SHIP:FACING:STARVECTOR is the way the ship's right wing is aimed right now.

.. attribute:: Direction:INVERSE

    :type: :struct:`Direction`
    :access: Get only

    :struct: Gives a `Direction` with the opposite rotation around its axes.

Operations and Methods
----------------------

You can use math operations on :struct:`Direction` objects as well. The next example uses a rotation of "UP" which is a system variable describing a vector directly away from the celestial body you are under the influence of:

Supported Direction Operators:

:Direction Multiplied by Direction:
    ``Dir1 * Dir2`` - This operator returns the result of rotating Dir2 by the rotation of Dir1.  Note that the order of operations matters here.  ``Dir1*Dir2`` is not the same as ``Dir2*Dir1``.  Example::

        // A direction pointing along compass heading 330, by rotating NORTH by 30 degrees around UP axis:
        SET newDir TO ANGLEAXIS(30,SHIP:UP) * NORTH.

:Direction Multiplied by Vector:
    ``Dir * Vec`` - This operator returns the result of rotating the vector by Dir::

        // What would the velocity of your ship be if it was angled 20 degrees to your left?
        SET Vel to ANGLEAXIS(-20,SHIP:TOPVECTOR) * SHIP:VELOCITY:ORBIT.
        // At this point Vel:MAG and SHIP:VELOCITY:MAG should be the same, but they don't point the same way

:Direction Added to Direction:
    ``Dir1 + Dir2`` - This operator is less reliable because its exact behavior depends on the order of operations of the UnityEngine's X Y and Z axis rotations, and it can result in gimbal lock.

    It's supposed to perform a Euler rotation of one direction by another, but it's preferred to use ``Dir*Dir`` instead, as that doesn't experience gimbal lock, and does not require that you know the exact transformation order of Unity.

For vector operations, you may use the ``:VECTOR`` suffix in combination with the regular vector methods::

    SET dir TO SHIP:UP.
    SET newdir TO VCRS(SHIP:PROGRADE:VECTOR, dir:VECTOR)

.. _vectors_vs_directions:

The Difference Between Vectors and Directions
---------------------------------------------

There are some consequences when converting from a :struct:`Direction` to a :struct:`Vector` and vice versa which should not be overlooked.

    A :struct:`Vector` and a :struct:`Direction` can be represented with the exact same amount of information: a tuple of 3 floating point numbers. So you might wonder why it is that a :struct:`Vector` can hold information about the magnitude of the line segment, while a :struct:`Direction` cannot, given that both have the same amount of information. The answer is that a :struct:`Direction` does contain one thing a :struct:`Vector` does not. A :struct:`Direction` knows which way is "up", while a :struct:`Vector` does not. If you tell **kOS** to ``LOCK STEERING`` to a :struct:`Vector`, it will be able to point the nose of the vessel in the correct direction, but won't know which way you want the roof of the craft rotated to. This works fine for axial symmetrical rockets but can be a problem for airplanes.

Therefore if you do this::

    SET MyVec to V(100,200,300).
    SET MyDir to MyVec:DIRECTION.

Then ``MyDir`` will be a :struct:`Direction`, but it will be a :struct:`Direction` where you have no control over which way is "up" for it.
