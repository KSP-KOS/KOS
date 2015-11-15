.. _cooked:

Cooked Control
==============

.. contents:: Contents
    :local:
    :depth: 1

For more information, check out the documentation for the :struct:`SteeringManager` structure.

In this style of controlling the craft, you do not steer the craft directly, but instead select a goal direction and let kOS pick the way to steer toward that goal. This method of controlling the craft consists primarily of the following two commands:

.. _LOCK THROTTLE:
.. object:: LOCK THROTTLE TO value.

    This sets the main throttle of the ship to *value*. Where *value* is a floating point number between 0.0 and 1.0. A value of 0.0 means the throttle is idle, and a value of 1.0 means the throttle is at maximum. A value of 0.5 means the throttle is at the halfway point, and so on.

.. _LOCK STEERING:
.. object:: LOCK STEERING TO value.

   This sets the direction **kOS** should point the ship where *value* is a :struct:`Vector` or a :ref:`Direction <direction>` created from a :ref:`Rotation <rotation>` or :ref:`Heading <heading>`:

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

    :struct:`Vector`

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

Tuning cooked steering
----------------------

While cooked steering tries to come balanced to perform decently without user
interaction, there are some instances where you may need to help tune the
behavior.  There are two parts of the steering calculation that can be tuned.
First, you can modify how kOS decides how fast the ship should turn.

    // MAXSTOPPINGTIME tells kOS how to calculate the maximum allowable
    // angular velocity.  Increasing the value will result in the ship turning
    // faster, but it may introduce more overshoot.
    // Adjust this setting if you have a small amount of torque on a large mass,
    // or if your ship appears to oscillate back and forth rapidly without
    // moving towards the target direction.
    SET STEERINGMANAGER:MAXSTOPPINGTIME TO 10.

    // You can also modify the PID constants for that calculate desired angular
    // velocity based on angular error.  Note that changes made directly to
    // the PIDLoop's MINIMUM and MAXIMUM suffixes will be overwritten based on
    // the value MAXSTOPPINGTIME, the ship's torque and moment of inertia.
    // These values will require precision and testing to ensure consistent performance.
    // Beware of large KD values: Due to the way angular velocity and part
    // facing directions are calculated in KSP, it is normal to have small rapid
    // fluctuations which may introduce instability in the derivative component.
    SET STEERINGMANAGER:PITCHPID:KP TO 0.85.
    SET STEERINGMANAGER:PITCHPID:KI TO 0.5.
    SET STEERINGMANAGER:PITCHPID:KD TO 0.1.

Second, you can change how the controls are manipulated to achieve the desired
angular velocity.  Internally, kOS uses the ship's available torque and
moment of inertial to dynamically calculate the PID constants.  Then the desired
torque is calculated based on the desired angular velocity.  The steering
controls are then set based on the the percentage the desired torque is of the
available torque.  You can change the settling time for the torque calculation
along each axis:

    // Increase the settling time to slow down control reaction time and
    // reduce control spikes.  This is helpful in vessels that wobble enough to
    // cause fluctuations in the measured angular velocity.
    // This is recommended if your ship turns towards the target direction well
    // but then oscillates when close to the target direction.
    SET STEERINGMANAGER:PITCHTS TO 10.
    SET STEERINGMANAGER:ROLLTS TO 5.

If you find that kOS is regularly miscalculating the available torque, you can
also define an adjust bias, or factor.  Check out these :struct:`SteeringManager`
suffixes for more details: PITCHTORQUEADJUST, YAWTORQUEADJUST, ROLLTORQUEADJUST,
PITCHTORQUEFACTOR, YAWTORQUEFACTOR, ROLLTORQUEFACTOR

Advantages/Disadvantages
------------------------

The advantage of "Cooked" control is that it is simpler to write scripts for, but the disadvantage is that you have no direct control over the details of the motion. You can't dictate how fast or slow the craft rotates, or which axis it tries to rotate around first, and if your craft is wobbly, you can't dampen the wobbliness.

Cooked controls perform best on ships that do not rely heavily on control
surfaces, have medium levels of torque, and are structurally stable.  You can
improve the control by placing the ship's root part or control part close to the
center of mass (preferablly both).  Adding struts to critical joints (like
decouplers) or installing a mod like Kerbal Joint Reinforcement will also help.
