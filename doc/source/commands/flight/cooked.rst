Cooked Control
==============

In this style of controlling the craft, you do not steer the craft directly, but instead select a goal direction and let kOS pick the way to steer toward that goal. This method of controlling the craft consists primarily of the following two commands:

.. _LOCK THROTTLE:
.. object:: LOCK THROTTLE TO value.

    This sets the main throttle of the ship to *value*. Where *value* is a floating point number between 0.0 and 1.0. A value of 0.0 means the throttle is idle, and a value of 1.0 means the throttle is at maximum. A value of 0.5 means the throttle is at the halfway point, and so on.

.. _LOCK STEERING:
.. object:: LOCK STEERING TO value.

   This sets the direction **kOS** should point the ship where *value* is a :ref:`Vector <vector>` or a :ref:`Direction <direction>` created from a :ref:`Rotation <rotation>` or :ref:`Heading <heading>`:

    :ref:`Rotation <rotation>`

        A Rotation expressed as ``R(pitch,yaw,roll)``. Note that pitch, yaw and roll are not based on the horizon, but based on an internal coordinate system used by **KSP** that is hard to use. Thankfully, you can force the rotation into a sensible frame of reference by adding a rotation to a known direction first.

        To select a direction that is 20 degrees off from straight up::

            LOCK STEERING TO Up + R(20,0,0).

        To select a direction that is due east, aimed at the horizon::
        
            LOCK STEERING TO North + R(0,90,0).
           
        ``UP`` and ``NORTH`` are the only two predefined rotations.
            
    :ref:`Heading <heading>`
     
        A heading expressed as ``HEADING(compass, pitch)``. This will aim 30 degrees above the horizon, due south::

            LOCK STEERING TO HEADING(180, 30).

    :ref:`Vector <vector>`

        Any vector can also be used to lock steering::

            LOCK STEERING TO V(100,50,10).

        Note that the internal coordinate system for ``(X,Y,Z)`` is quite complex to explain. To aim in the opposite of the surface velocity direction::

            LOCK STEERING TO (-1) * SHIP:VELOCITY:SURFACE.

        The following aims at a vector which is the cross product of velocity and direction down to the SOI planet - in other words, it aims at the "normal" direction to the orbit::

            LOCK STEERING TO VCRS(SHIP:VELOCITY:ORBIT, BODY:POSITION).

Like all ``LOCK`` expressions, the steering and throttle continually update on their own when using this style of control. If you lock your steering to velocity, then as your velocity changes, your steering will change to match it. Unlike with other ``LOCK`` expressions, the steering and throttle are special in that the lock expression gets executed automatically all the time in the background, while other ``LOCK`` expressions only get executed when you try to read the value of the variable. The reason is that the **kOS** computer is constantly querying the lock expression multiple times per second as it adjusts the steering and throttle in the background.

Unlocking controls
------------------

If you ``LOCK`` the ``THROTTLE`` or ``STEERING``, be aware that this prevents the user from manually controlling them. Until they unlock, the manual controls are prevented from working. You can free up the controls by issuing these two commands::

    UNLOCK STEERING.
    UNLOCK THROTTLE.

When the program ends, these automatically unlock as well, which means that to control a craft you must make sure the program doesn't end. The moment it ends it lets go of the controls.

Advantages/Disadvantages
------------------------

The advantge of "Cooked" control is that it is simpler to write scripts for, but the disadvantage is that you have no control over the details of the motion. You can't dictate how fast or slow the craft rotates, or which axis it tries to rotate around first, and if your craft is wobbly, you can't dampen the wobbliness.
