.. _direction:

Directions
==========

.. contents:: Contents
    :local:
    :depth: 1

:struct:`Direction` objects represent a rotation starting from an initial point in **KSP**'s coordinate system where the initial state was looking down the :math:`+z` axis, with the camera "up" being the :math:`+y` axis. This exists primarily to enable automated steering.

Creation
--------

=============================================== ===================================
 Method                                          Description
=============================================== ===================================
 :func:`R(pitch,yaw,roll)`                       Euler rotation
 :func:`Q(x,y,z,rot)`                            Quaternion
 :func:`HEADING(dir,pitch)`                      Compass heading
 :ref:`FACING         <Direction from suffix>`   From SHIP or TARGET
 :ref:`UP             <Direction from suffix>`   From SHIP
 :ref:`PROGRADE, etc. <Direction from suffix>`   From SHIP, TARGET or BODY
=============================================== ===================================

.. function:: R(pitch,yaw,roll)

    A :struct:`Direction` can be created out of a Euler Rotation, indicated with the :func:`R()` function, as shown below where the ``pitch``, ``yaw`` and ``roll`` values are in degrees::

        SET myDir TO R( a, b, c ).

.. function:: Q(x,y,z,rot)

    A :struct:`Direction` can also be created out of a *Quaternion* tuple, indicated with the :func:`Q()` function, as shown below where ``x``, ``y``, and ``z`` are a :struct:`Vector` to rotate around, and ``rot`` is how many degrees to rotate::

        SET myDir TO Q( x, y, z, rot ).


.. function:: HEADING(dir,pitch)

    A :struct:`Direction` can be created out of a :func:`HEADING()` function. The first parameter is the compass heading, and the second parameter is the pitch above the horizon::

        SET myDir TO HEADING(degreesFromNorth, pitchAboveHorizon).

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

    ================ ================== ================================
     Suffix           Type               Description
    ================ ================== ================================
     :attr:`PITCH`    scalar (deg)       Rotation around :math:`x` axis
     :attr:`YAW`      scalar (deg)       Rotation around :math:`y` axis
     :attr:`ROLL`     scalar (deg)       Rotation around :math:`z` axis
     :attr:`VECTOR`   :struct:`Vector`   Vector of length 1
    ================ ================== ================================
    
    The :struct:`Direction` object exists primarily to enable automated steering. You can initialize a :struct:`Direction` using a :struct:`Vector` or a ``Rotation``. :struct:`Direction` objects represent a rotation starting from an initial point in **KSP**'s coordinate system where the initial state was looking down the :math:`+z` axis, with the camera "up" being the :math:`+y` axis. So for example, a :struct:`Direction` pointing along the :math:`x` axis might be represented as ``R(0,90,0)``, meaning the initial :math:`z`-axis direction was rotated *90 degrees* around the :math:`y` axis.

    If you are going to manipulate directions a lot, it's important to note that the order in which the rotations occur is:

    1. First rotate around :math:`z` axis.
    2. Then rotate around :math:`x` axis.
    3. Then rotate around :math:`y` axis.

    What this means is that if you try to ``ROLL`` and ``YAW`` in the same tuple, like so: ``R(0,45,45)``, you'll end up **rolling first and then yawing**, which might not be what you expected. There is little that can be done to change this as it's the native way things are represented in the underlying **Unity engine**.

    Also, if you are going to manipulate directions a lot, it's important to note how **KSP**'s `native coord system works <ref_frame>`_.

.. attribute:: Direction:PITCH

    :type: scalar (deg)
    :access: Get only


    Rotation around the :math:`x` axis.
    
.. attribute:: Direction:YAW

    :type: scalar (deg)
    :access: Get only

    Rotation around the :math:`y` axis.
    
.. attribute:: Direction:ROLL

    :type: scalar (deg)
    :access: Get only


    Rotation around the :math:`z` axis.
    
.. attribute:: Direction:VECTOR

    :type: :struct:`Vector`
    :access: Get only

    :struct:`Vector` of length 1 that is in the same direction.


.. note:: **The difference between a :struct:`Direction` and a ``Vector``**

    ``Vector`` and a :struct:`Direction` can be represented with the exact same amount of information: a tuple of 3 floating point numbers. So you might wonder why it is that a ``Vector`` can hold information about the magnitude of the line segment, while a :struct:`Direction` cannot, given that both have the same amount of information. The answer is that a :struct:`Direction` does contain one thing a ``Vector`` does not. A :struct:`Direction` knows which way is "up", while a ``Vector`` does not. If you tell **kOS** to ``LOCK STEERING`` to a ``Vector``, it will be able to point the nose of the vessel in the correct direction, but won't know which way you want the roof of the craft rotated to. This works fine for axial symmetrical rockets but can be a problem for airplanes.


Operations and Methods
----------------------

You can use math operations on :struct:`Direction` objects as well. The next example uses a rotation of "UP" which is a system variable describing a vector directly away from the celestial body you are under the influence of::

    // Set direction 45 degrees west of "UP".
    SET Direction TO SHIP:UP + R(0,-45,0). 

For vector operations, you may use the ``:VECTOR`` suffix in combination with the regular vector methods::

    SET dir TO SHIP:UP.
    SET newdir TO VCRS(SHIP:PROGRADE:VECTOR, dir:VECTOR)

Vectors and Directions
----------------------

There are some consequences when converting from a :struct:`Direction` to a ``Vector`` and vice versa which should not be overlooked.

A ``Vector`` and a :struct:`Direction` can be represented with the exact same amount of information: a tuple of 3 floating point numbers. So you might wonder why it is that a ``Vector`` can hold information about the magnitude of the line segment, while a :struct:`Direction` cannot, given that both have the same amount of information. The answer is that a :struct:`Direction` does contain one thing a ``Vector`` does not. A :struct:`Direction` knows which way is "up", while a ``Vector`` does not. If you tell **kOS** to ``LOCK STEERING`` to a ``Vector``, it will be able to point the nose of the vessel in the correct direction, but won't know which way you want the roof of the craft rotated to. This works fine for axial symmetrical rockets but can be a problem for airplanes.

Therefore if you do this::

    SET MyVec to V(100,200,300).
    SET MyDir to MyVec:DIRECTION.

Then ``MyDir`` will be a :struct:`Direction`, but it will be a :struct:`Direction` where you have no control over which way is "up" for it.
