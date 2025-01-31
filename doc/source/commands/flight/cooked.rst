.. _cooked:

Cooked Control
==============

.. contents:: Contents
    :local:
    :depth: 1

.. note::

    It is possible for more than one processor part to control the cooked
    steering methods at any given time.  Whichever processor is the last to
    update the value of the given control will take priority.

    .. versionchanged:: v1.1.3
        kOS no longer throws an error if multiple parts attempt to set the value
        of the same control.

For more information, check out the documentation for the :struct:`SteeringManager` structure.

In this style of controlling the craft, you do not steer the craft directly, but instead select a goal direction and let kOS pick the way to steer toward that goal. This method of controlling the craft consists primarily of the following two commands:

CONFIG:SUPPRESSAUTOPILOT
------------------------

If :attr:`Config:SUPPRESSAUTOPILOT` is true, then none of the controls
on this page will have an effect.  That setting is there to provide
the player with an emergency way to quickly click a toggle on the
toolbar window to force kOS to stop taking control, letting the player
move the controls manually.

The special LOCK variables for cooked steering
----------------------------------------------

.. _LOCK THROTTLE:
.. object:: LOCK THROTTLE TO expression. // value range [0.0 .. 1.0]

    This sets the main throttle of the ship to *expression*. Where *expression* is a floating point number between 0.0 and 1.0. A value of 0.0 means the throttle is idle, and a value of 1.0 means the throttle is at maximum. A value of 0.5 means the throttle is at the halfway point, and so on.

    The expression used in this statement can be any formula and can
    call your own user functions.  Just make sure it returns a value
    in the range [0..1].

.. warning::

    Do not use both ``SAS`` and ``lock steering`` at the same time.
    See the :ref:`warning below<locksteeringsaswarning>`.

.. warning::

    It's a very bad idea to``WAIT`` during the execution of the expression in a
    LOCK THROTTLE.  See the note in the next section below.

.. warning::

    IF you have the *Breaking Ground DLC* for Kerbal Space Program, please
    be aware that even though you can set up control groups to make parts
    such as propellors and engines react to the throttle, they will not
    react to ``lock throttle``.  This is because the DLC ignores the
    autopilot API in using this feature.  It only pays attention to the
    actual pilot controls, not the autopilot controls overriding them.
    To affect a part that is defined to react to the throttle axis, you
    will have to use ``set ship:control:pilotmainthrottle`` as described
    by the :ref:`section on pilot controls<pilot>`.

.. _LOCK STEERING:
.. object:: LOCK STEERING TO expression.

    This sets the direction **kOS** should point the ship where *expression* is a :struct:`Vector` or a :ref:`Direction <direction>` created from a :ref:`Rotation <rotation>` or :ref:`Heading <heading>`:

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

    ``"kill"`` string

        Steering may also be locked to the special string value of ``"kill"``
        which tells the steering manager to attempt to stop any vessel rotation,
        much like the stock SAS's stability assist mode.


Like all ``LOCK`` expressions, the steering and throttle continually update on their own when using this style of control. If you lock your steering to velocity, then as your velocity changes, your steering will change to match it. Unlike with other ``LOCK`` expressions, the steering and throttle are special in that the lock expression gets executed automatically all the time in the background, while other ``LOCK`` expressions only get executed when you try to read the value of the variable. The reason is that the **kOS** computer is constantly querying the lock expression multiple times per second as it adjusts the steering and throttle in the background.

.. _locksteeringsaswarning:
.. warning::

    **About** ``lock steering`` **and** ``SAS`` **:**  While kOS had previously supported
    enabling SAS at the same time as locking steering, this functionality broke
    when the underlying KSP method was changed in a version upgrade.  It is our
    hope to evenentually restore this functionality.  Please check github issue
    `#2117 <https://github.com/KSP-KOS/KOS/issues/2117>`_ for updates.

.. _LOCK WHEELTHROTTLE:
.. object:: LOCK WHEELTHROTTLE TO expression. // value range [-1.0 .. 1.0]

    **(For Rovers)** This is used to control the throttle that is used when
    driving a wheeled vehicle on the ground.  It is an entirely independent
    control from the flight throttle used with ``LOCK THROTTLE`` above.
    It is analogous to holding the 'W' (value of +1) or 'S' (value of -1)
    key when driving a rover manually under default keybindings.

    ``WHEELTHROTTLE`` allows you to set
    a negative value, up to -1.0, while ``THROTTLE`` can't go below zero.
    A negative value means you are trying to accelerate in reverse.

    Unlike trying to drive manually, using ``WHEELTHROTTLE`` in kOS does
    not cause the torque wheels to engage as well.  In stock KSP using
    the 'W' or 'S' keys on a rover engages both the wheel driving AND the
    torque wheel rotational power.  In kOS those two features are
    done independently.

    The expression used in this statement can be any formula and can
    call your own user functions.  Just make sure it returns a value
    in the range [0..1].

.. warning::

    It's a very bad idea to ``WAIT`` during the execution of the expression in a
    LOCK WHEELTHROTTLE.  See the note in the next section below.

.. _LOCK WHEELSTEERING:
.. object:: LOCK WHEELSTEERING TO expression.

   **(For Rovers)** This is used to tell the rover's cooked steering
   where to go.  The rover's cooked steering doesn't use nearly as
   sophisticated a PID control system as the flight cooked steering
   does, but it does usually get the job done, as driving has more
   physical effects that help dampen the steering down automatically.

   There are 3 kinds of value understood by WHEELSTEERING:

   - :struct:`GeoCoordinates` - If you lock wheelsteering to a
     :struct:`GetCoordinates`, that will mean the rover will try to steer in
     whichever compass direction will aim at that location.

   - :struct:`Vessel` - If you try to lock wheelsteering to a vessel,
     that will mean the rover will try to steer in whichever compass
     direction will aim at that vessel.  The vessel being aimed at
     does not need to be landed.  If it is in the sky, the rover will
     attempt to aim at a location directly underneath it on the ground.

   - *Scalar Number* - If you try to lock wheelsteering to just a plain
     scalar number, that will mean the rover will try to aim at that
     compass heading.  For example ``lock wheelsteering to 45.`` will
     try to drive the rover northeast.

   For more precise control over steering, you can use raw steering to
   just directly tell the rover to yaw left and right as it drives and
   that will translate into wheel steering provided the vessel is landed
   and you have a probe core aiming the right way.

   **A warning about WHEELSTEERING and vertically mounted probe cores**:

   If you built your rover in such a way that the probe core controlling it
   is stack-mounted facing up at the sky when the rover is driving, that
   will confuse the ``lock WHEELSTEERING`` cooked control mechanism.  This
   is a common building pattern for KSP players and it seems to work okay
   when driving manually, but when driving by a kOS script, the fact that
   the vessel's facing is officially pointing up at the sky causes it to
   get confused.  If you notice that your rover tends to drive in the
   correct direction only when on a flat or slight downslope, but then
   turns around and around in circles when driving toward the target
   requires going up a slope, then this may be exactly what's happening.
   When it tilted back, the 'forward' vector aiming up at the sky started
   pointing behind it, and the cooked steering thought the rover was
   aimed in the opposite direction to the way it was really going.
   To fix this problem, either mount your rover probe core facing the
   front of the rover, or perform a "control from here" on some forward
   facing docking port or something like that to get it to stop thinking
   of the sky as "forward".

.. warning::

    It's a very bad idea to ``WAIT`` during the execution of the expression in a
    LOCK WHEELSTEERING.  See the note in the next section below.

.. global:: WHEELSTEERINGPID

   :access: Get/Set
   :type: :struct:`PIDLoop`

   The PIDLoop used to control wheelsteering. Can be used to optimize
   steering performance and eliminate steering oscillations on some vessels.

Don't 'WAIT' or run slow script code during cooked control calculation
----------------------------------------------------------------------

Be aware that because LOCK THROTTLE, LOCK STEERING, LOCK
WHEELTHROTTLE, and LOCK WHEELSTEERING are actually the
highest priority types of :ref:`triggers <triggers>` that
exist in kOS, they cause your expression to be calculated
every single physics update tick behind the scenes.  So you
should not execute a ``WAIT`` command in the code that
performs the evaluation of the value used in them, as that
will effectively cheat the entire script out of the full
execution speed it deserves.

For example, if you attempt this::

    function get_throttle {
        wait 0.001.  // this line is a bad idea.
        return 0.5.
    }
    lock throttle to get_throttle().

Then kOS will attempt to call the ``WAIT`` command *every single*
update, as the kOS system keeps trying to re-run the
``lock throttle`` expression to learn what you want the new
throttle value to be. This will starve your script of the
CPU time it deserves, having the effect of running the
lock function every-other-tick, and the rest of your code
every-other-tick on the ticks in-between.  (When the system
hits the wait inside the throttle expression, it will stop
there, not resuming until the next update, effectively meaning
it doesn't get around to running any of your main-line code
until the next tick.)

Again, note that the cooked steering LOCKS mentioned here are
the *highest* priority triggers there are in kOS.  That means they
can even interrupt other triggers like WHEN/THEN or GUI callbacks.
Do not make them call complex functions that take a lot of instructions
to return a value, or else you might find that there's not enough
instructions per update left to run the rest of your program effectively.

Normally when you use a LOCK command, the expression is only evaluated
when it needs to be by some other part of the script that is trying
to read the value.  But with these special cooked control locks,
remember that the kOS system *itself* will query the value repeatedly
in the background so it knows how to adjust the piloting.  Unlike
normal LOCKs, these LOCKs will be executed again and again even when
you're not explicitly trying to get their values.


Unlocking controls
------------------

If you ``LOCK`` the ``THROTTLE`` or ``STEERING``, be aware that this prevents the user from manually controlling them. Until they unlock, the manual controls are prevented from working. You can free up the controls by issuing these two commands::

    UNLOCK STEERING.
    UNLOCK THROTTLE.

When the program ends, these automatically unlock as well, which means that to control a craft you must make sure the program doesn't end. The moment it ends it lets go of the controls.

.. _cooked_tuning:

Tuning cooked steering
----------------------

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
settings that might help solve some problems, in the list below:

- **problem**: When rotating toward the target direction, ``lock steering``
  is wiggling the controls back and forth trying to keep the exact
  rotation rate even though it doesn't matter.  This is wasting RCS
  fuel.  (NOTE: This problem is different from the problem where it
  wiggles the controls *after* it arrives at the destination orientation.
  This is specifically for when it wiggles the controls *during* its
  rotation to the destination orientation.)

  - **solution**: Increase :attr:`STEERINGMANAGER:TORQUEPSILONMAX` to make
    it "not care" about the exact rotation rate until it gets closer to
    the target orientation.
    Increasing :attr:`STEERINGMANAGER:TORQUEPSILONMIN` can help also, but
    making it too high could prevent the steering from holding the nose on
    target once it does reach the desired direction.

- **problem**: On a vessel with very very slow rotational acceleration
  capabilities, kOS appears to be making no attempt at all to rotate
  the vessel.  (Note, not just slowly, but literally never moving the
  controls at all).

  - **solution**: You might have to either decrease
    :attr:`STEERINGMANAGER:TORQUEEPSILONMAX` or increase
    :attr:`STEERINGMANAGER:MAXSTOPPINGTIME`.

  - **explanation**: The problem may be that your vessel is so slow at
    rotating that the rotation rate the SteeringManager is attempting
    to achieve falls within its epsilon (null zone) that it ignores.
    Refer to :ref:`this formula <rotationepsilonmax_math>` to see if
    this is the problem.  kOS's has default values that attempt to be
    good enough for most designs, but it's impossible to guess every
    design that every player might try.  If you design a vessel that
    takes quite a few minutes to rotate around, it might fall outside
    the range of possibilities the default settings were made for.

- **problem**: A large vessel with low torque doesn't seem to be even trying to
  rotate very quickly.  It *does* turn, but very slowly.  Once it starts
  turning, the controls may be fluctuating around the zero point, letting it
  rotate slowly with momentum without trying to push its rotation any
  faster.

  - **solution**: Increase `STEERINGMANAGER:MAXSTOPPINGTIME` to about 5 or
    10 seconds or so.  Also, slightly increase `STEERINGMANAGER:PITCHPID:KD`
    and `STEERINGMANAGER:YAWPID:KD` to about 1 or 2 as well to go with it.

  - **explanation**: Once the steering manager gets such a ship rotating at
    a tiny rate, it stops trying to make it rotate any faster than that
    because it's designed to optimize for less expended thrust rather than
    for faster turning.  Every bit of angular momentum it builds up it's
    just going to have to stop again later.  The setting it uses to
    make this decision is :attr:`STEERINGMANAGER:MAXSTOPPINGTIME`.  It
    tries not to build up an angular velocity that would take it more than
    ``MAXSTOPPINGTIME`` seconds to stop again later.  Increasing this
    setting tells it you'd rather err on the side of faster rotations
    rather than err on the side of less expenditure of torque/RCS fuel.
    The reason for increasing the Kd terms as well is to help it deal
    with the need to be more proactive about the slowing down at the end
    of the turn.

- **problem**: A vessel seems to reasonably come to the desired direction
  sensibly, but once it's there the ship vibrates back and forth by about 1
  degree or less excessively around the setpoint.

  - **solution 1**: Increase `STEERINGMANAGER:PITCHTS` and
    `STEERINGMANAGER:YAWTS` to several seconds.

  - **solution 2**: If you don't care about the exact precision to
    point the correct direction to tiny fractions of a degree, then
    increase :attr:`SteeringManager:TORQUEEPSILONMIN` by a little bit.

  - **explanation**: Once it's
    at the desired orientation and it has mostly zeroed the rotational
    velocity, it's trying to hold it there with microadjustments to the
    controls, and those microadjustments are "too tight".

- **problem**: The vessel is having a hard time holding on to its
  ``lock steering`` direction during a burn when you have physics
  warp on.  It keeps veering off and having to correct the steering
  back again.  It may even show the rocket bending.

  - **solution:** If this is happening specifically under physics warp,
    and specifically during burns, chances are this is KSP's fault, not
    kOS, and it can be fixed by turning on KSP's "Advanced Tweakables" and
    autostrutting a few parts on the ship to root.
    
  - **explanation:** This happens because your
    vessel has some of those springy joints in it that go haywire under
    physics warp.  (You know the kind, where when you thrust the vessel
    visibly compresses like a spring?) When you have a springy joint on
    the ship, not only does it compress and stretch but it also flexes
    side to side.  This flexing side to side can cause kOS to get false
    information about which way the vessel is pointed. Because the
    vessel's official orientation is the orientation of the 'control from
    here' part, which is getting wiggled around by the physics warp,
    the official orientation information is always a few degrees off.
    kOS is believing that false information about which way the vessel
    is pointed and trying to "correct" it.)

- **problem**: The vessel's nose seems to be waving slowly back and forth
  across the set direction, taking too long to center on it, and you notice
  the control indicators are pushing all the way to the extremes as it does
  so.

  - **solution**: Increase `STEERINGMANAGER:PITCHPID:KD` and
    `STEERINGMANGER:YAWPID:KD`.

  - **explanation**: The ship is *trying* to
    push its rotation rate too high when almost at the setpoint.  It needs
    to anticipate the fact that it is going to reach the desired direction
    and start slowing down BEFORE it gets there.

- **problem**: The vessel's nose seems to be waving slowly back and forth across
  the set direction, taking too long to center on it, but you notice that the
  control indicators are NOT pushing all the way to the extremes as it does
  so.  Instead they seem to be staying low in magnitude, wavering around zero
  and may be getting smaller over time.

  - **solution**: Decrease `STEERINGMANAGER:PITCHTS` and/or
    `STEERINGMANAGER:YAWTS`

  - **explanation**: While larger values for the
    settling time on the Torque PID controller will help to smooth out
    spikes in the controls, it also results in a longer time period before
    the steering comes to a rest at the setpoint (also knows as settling).
    If you had previously increased the settling time to reduce
    oscillations, try picking a value half way between the default and the
    new value you previously selected.


But to understand how to tune the cooked steering in a more complex way than
just with that simple list, you first have to understand what a PID controller
is, at least a little bit, so you know what the settings you can tweak
actually do.

If you don't know what a PID controller is and want to learn more, you can
read numerous descriptions of the concept on the internet that can be found
in moments by a web search.  If you just want to know a two minute explanation
for the sake of tuning the cooked steering a bit, read on.

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
scope of this document, but you can check out :ref:`the PID Loop tutorial. <pidloops>`

Cooked Steering's use of PID controllers
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. highlight:: none

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

.. highlight:: kerboscript

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
the actual controls themselves (i.e. the WASDQE controls) to accelerate
toward that goal rotational velocity.

The suffixes to :ref:`SteeringManager <steeringmanager>` don't quite
allow direct manipulation of the torque PID tuning parameters Kp, Ki,
and Kd, because they are calculated indirectly from the ship's own
attributes.  However, there are several suffixes to
:ref:`SteeringManager <steeringmanager>` that allow you to make
indirect adjustments to them that are used in calculating the values
it uses for Kp, Ki, and Kd.

****

This technique of using two different PID controllers, the first one
telling the second one which seek value to use, and the second one
actually being connected to the control "lever", is one of many ways of dealing
with a phenomenon with two levels of indirection from the control.

Keeping the above two things separate, the rotational velocity PID
versus the Torque PID, is important in knowing which setting
you need to tweak in order to achieve the desired effect.

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

First, you can modify how kOS decides how fast the ship should turn::

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
change the settling time for the torque calculation along each axis::

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
center of mass (preferably both).  Adding struts to critical joints (like
decouplers) or installing a mod like Kerbal Joint Reinforcement will also help.

But because of the impossibility of finding one setting that is universally
correct for all possible vessels, sometimes the only way to make cooked
steering work well for you is to adjust the parameters as described above,
or to make your own steering control from scratch using raw steering.
