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

.. _cooked_tuning:

Tuning cooked steering
----------------------

.. versionadded:: 0.18.0

    Version 0.18 of kOS completely gutted the internals of the old steering
    system and replaced them with the system described below.  Anything
    said below this point is pertinent to version 0.18 and higher only.

While cooked steering tries to come balanced to perform decently without user
interaction, there are some instances where you may need to help tune the
behavior.  There are a number of settings you can adjust to tweak the
behavior of the cooked steering if it's not performing exactly as you'd
like.  It may be the case that making your own control mechanism from
scratch, while entirely possible with kOS, might be unnecessary if all
you really want to do is just make the cooked steering behave slightly
differently.

The adjustments described below all come from the
:ref:`SteeringManager <steeringmanager>` structure, which
has its own detailed documentation page.

Some simple suggestions to try fixing common problems
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

If you don't want to understand the intricate details of the cooked
steering system, here's some quick suggestions for changes to the
settings that might help solve some problems, in the table below:

.. list-table::
    :header-rows: 1
    :widths: 1 1 1

    * - steering problem
      - possible solution(s)
      - reason

    * - A large vessel with low torque doesn't seem to be even trying to rotate at all.  The controls look like they're not even trying, staying near zero.  I understand that it will rotate slowly, but it should at least try to rotate.
      - Increase `STEERINGMANAGER:MAXSTOPPINGTIME` to about 5 or 10 seconds or so.  Also, slightly increase `STEERINGMANAGER:PITCHPID:KD` and `STEERINGMANAGER:YAWPID:KD` to about 1 or 2 as well to go with it.
      - Once the steering manager gets such a ship rotating at a tiny rate, it stops trying to make it rotate any faster than that because it's "afraid" of allowing it to obtain a larger momentum than it thinks it could quickly stop.  It needs to be told that in this case it's okay to build up more "seconds worth" of rotational velocity.  The reason for increasing the Kd term as well is to tell it to anticipate the need to starting slowing down rotation sooner than it normally would.
    * - A vessel seems to reasonably come to the desired direction sensibly, but once it's there the ship vibrates back and forth by about 1 degree or less excessively around the setpoint.
      - Increase `STEERINGMANAGER:PITCHTS` and `STEERINGMANAGER:YAWTS` to several seconds
      - Once it's at the desired orientation and it has mostly zeroed the rotational velocity, it's trying to hold it there with microadjustments to the controls, and those microadjustments are "too tight".
    * - The vessel's nose seems to be waving slowly back and forth across the set direction, taking too long to center on it, and you notice the control indicators are pushing all the way to the extremes as it does so.
      - Increase `STEERINGMANAGER:PITCHPID:KD` and `STEERINGMANGER:YAWPID:KD`.
      - The ship is *trying* to push its rotation rate too high when almost at the setpoint.  It needs to anticipate the fact that it is going to reach the desired direction and start slowing down BEFORE it gets there.
    * - The vessel's nose seems to be waving slowly back and forth across the set direction, taking too long to center on it, but you notice that the control indicators are NOT pushing all the way to the extremes as it does so.  Instead they seem to be staying low in magnitude, wavering around zero.
      - TODO - put the torque PID corrective measure here.  I'm not sure what it is, but I think the above would be caused by it *wanting* to reduce rotational velocity ahead of time like it should, but failing to do so because the controls are being moved too weakly to achieve the change fast enough.
      - TODO - explain the torque PID corrective measure here.


But to understand how to tune the cooked steering in a more complex way than just with that simple table, you first have to understand
what a PID controller is, at least a little bit, so you know what the settings
you can tweak actually do.

If you don't know what a PID controller is and want to learn more, you can
read numerous descriptions of the concept on the internet that can be found
in moments by a web search.  If you just want to know a two minute explanation
for the sake of tuing the cooked steering a bit, read on.

Quick and Dirty description of a PID controller
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

You can think of a PID controller as a magic mathematical black box that can
learn where to set a control lever in order to achieve a given goal.  A good
example of this is cruise control on a car.  You tell the cruise control
what speed you'd like it to maintain, and it attempts to move the accelerator
pedal to the necessary position that will maintain that constant speed.

That, in a nutshell is the goal of a PID controller - to perform tasks
like that.  You have control over a lever or dial of some sort, and it
indirectly affects a phenomenon you can measure, and you feed the
mathematical black box of the PID controller the measurement of the
phenomenon, and obey its instructions of where to set the control lever.
Over time, the PID controller, under the assumption that you are obeying
its instructions of where to set the control lever, learns how to fine 
tune its commands about how to set the lever to get the measurement to
settle on the value you asked for.

A more complex discussion of PID controllers than that is outside the
scope of this document.

Cooked Steering's use of PID controllers
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

kOS's cooked steering uses two nested PID controllers per axis of rotation::

    Seek direction   Current Direction Measurement
        |                |
        |                |
       \|/              \|/
    +-seek me---------cur val---+
    |                           |
    |  Rotational Velocity PID  |
    |                           |
    +-output--------------------+
      desired
      rotational
      velocity
      (i.e. "I'd like to be rotating at 3 degrees per second downward")
        |
        |
        |           Current Rotational Velocity measurement
        |                |
        |                |
       \|/              \|/
    +-seek me---------cur val---+
    |                           |
    |       Torque PID          |
    |                           |
    +-output--------------------+
      desired
      control
      setting
      (i.e. "ship:control:pitch should be -0.2")
        |
        |
        |
        |
       \|/
    Feed this control value to KSP.  (This is the value you can see
    on the control indicator meters in the lower-left of the screen).

.. _cooked_omega_pid:

The Rotational Velocity PID
:::::::::::::::::::::::::::

The first PID controller looks at the current direction the ship is pointed,
versus the direction the ship is meant to be pointed, and uses the offset
between the two to decide how to set the desired rotational velocity (rate
at which the angle is changing).

The suffixes to :ref:`SteeringManager <steeringmanager>` allow direct
manipulation of the rotational velocity's PID tuning parameters.

.. _cooked_torque_pid:

The Torque PID
::::::::::::::

But there is no such thing as a lever that directly controls the rotational
velocity.  What there is, is a lever that directly controls the rotational
*acceleration*.  When you pull on the yoke (i.e. hold down the "S" key),
you are telling the ship to either rotate *faster*  or *slower* than it
already is.  

So given a result from the Rotational Velocity PID, with a desired 
rotational velocity to seek, the second PID controller takes over,
the Torque PID, which uses that information to choose how to set
the actual controls themselves (i.e. the WASDQE controls) to acellerate
toward that goal roational velocity.

The suffixes to :ref:`SteeringManager <steeringmanager>` don't quite
allow direct manipulation of the torque PID tuning parameters Kp, Ki,
and Kd, because they are calculated indirectly from the ship's own
attributes.  However, there are several suffixes to
:ref:`SteeringManager <steeringmanager>` that allow you to make
indirect adjustments to them that are used in calculating the values 
it uses for Kp, Ki, and Kd.

****

This technique, of using two different PID controllers, the first one
telling the second one which seek value to use, and the second one
actually being connected to the control "lever", is one way of dealing
with a phenomenon with two levels of indirection from the control.
(Yes, there are other ways involving more sophisticated PID controllers.
This two-nested controller way is the way kOS's cooked steering ended up
doing it.)

Keeping the above two things separate, the rotational velocity PID
versus the Torque PID, is important in knowing which setting
you need to tweak in order to achive the desired effect.

One pair of PID's per axis of rotation
::::::::::::::::::::::::::::::::::::::

The above pair of controllers is replicated per each of the 3 axes of
rotation, for a total of 6 altogether.  Some of the settings you can
adjust affect all 3 axes together, while others are specific to just
one.  See the descriptions of each setting carefully to know which is
which.

Corrects 2 axes first, then the 3rd
:::::::::::::::::::::::::::::::::::

The cooked steering tries to correct first the pitch and yaw, to aim
the rocket at the desired pointing vector, then only after it's very
close to finishing that task does it allow the 3rd axis, the roll axis,
to correct itself.  This is because if you try correcting all three at
the same time, it causes the cooked steering to describe a curved arc
toward its destination orientation, rather than rotating straight
towards it.

This behavior is correct for rockets with radial symmetry, but is
probably a bit wrong for trying to steer an airplane to a new heading
while in atmosphere.  For flying an airplane to a new heading, it's 
still best to make your own control scheme from scratch with raw steering.


The settings to change
::::::::::::::::::::::

First, you can modify how kOS decides how fast the ship should turn.

    // MAXSTOPPINGTIME tells kOS how to calculate the maximum allowable
    // angular velocity the Rotational Velocity PID is allowed to output.
    // Increasing the value will result in the ship turning
    // faster, but it may introduce more overshoot.
    // Adjust this setting if you have a small amount of torque on a large mass,
    // or if your ship appears to oscillate back and forth rapidly without
    // moving towards the target direction.
    SET STEERINGMANAGER:MAXSTOPPINGTIME TO 10.

    // You can also modify the PID constants that calculate desired angular
    // velocity based on angular error, in the angular velocity PID controller.
    // Note that changes made directly to the PIDLoop's MINIMUM and MAXIMUM
    // suffixes will be overwritten based on the value MAXSTOPPINGTIME, the
    // ship's torque and moment of inertia.
    // These values will require precision and testing to ensure consistent
    // performance.
    // Beware of large KD values: Due to the way angular velocity and part
    // facing directions are calculated in KSP, it is normal to have small rapid
    // fluctuations which may introduce instability in the derivative component.
    SET STEERINGMANAGER:PITCHPID:KP TO 0.85.
    SET STEERINGMANAGER:PITCHPID:KI TO 0.5.
    SET STEERINGMANAGER:PITCHPID:KD TO 0.1.

Second, you can change how the controls are manipulated to achieve the desired
angular velocity.  This is for the Torque PID mentioned above.  Internally,
kOS uses the ship's available torque and moment of inertial to dynamically
calculate the PID constants.  Then the desired torque is calculated based on
the desired angular velocity.  The steering controls are then set based on
the the percentage the desired torque is of the available torque.  You can
change the settling time for the torque calculation along each axis:

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

The advantage of "Cooked" control is that it is simpler to write scripts
for, but the disadvantage is that you have only partial control over
the details of the motion.

Cooked controls perform best on ships that do not rely heavily on control
surfaces, have medium levels of torque, and are structurally stable.  You can
improve the control by placing the ship's root part or control part close to the
center of mass (preferablly both).  Adding struts to critical joints (like
decouplers) or installing a mod like Kerbal Joint Reinforcement will also help.

But because of the impossibility of finding one setting that is universally 
correct for all possible vessels, sometimes the only way to make cooked 
steering work well for you is to adjust the parameters as described above,
or to make your own steering control from scratch using raw steering.

