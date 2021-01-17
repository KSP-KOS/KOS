.. _pidloops:

PID Loops in kOS
================

.. versionadded:: 0.18.1
    Note, this is an older tutorial.  As of
    kOS version 0.18.1 and up, a new :struct:`pidloop`
    feature was added to kOS to allow you to use a built-in PID
    controller that executes very quickly in the kOS "hardware"
    rather than in your script code.  You can use it to perform
    the work described in detail on this page.  However, this
    tutorial is still quite important because it walks you through
    how a PID controller works and what it's really doing under the
    hood.  It's probably a good idea to use the built-in
    :struct:`pidloop` instead of the program shown here, once you
    understand the topic this page describes.  However, it's also
    a good idea to have a read through this page to get an
    understanding of what that built-in feature is really doing.

This tutorial covers how one can implement a `PID loop`_ using kOS. A P-loop, or "proportional feedback loop" was already introduced in the second section of the :ref:`Design Patterns Tutorial <designpatterns>`, and that will serve as our starting point. After some code rearrangement, the integral and derivative terms will be added and discussed in turn. Next, a couple extra features will be added to the full PID-loop. Lastly, we'll show a case-study in tuning a full PID loop using the Ziegler-Nichols method. We'll use the LOG method to dump telemetry from KSP into a file and our favorite graphing software to visualize the data.

.. _PID loop: http://en.wikipedia.org/wiki/PID_controller

The code examples in this tutorial can be tested with a similar rocket design as shown. Do not forget the accelerometer, gravioli detector or the kOS CPU module. The engine is purposefully overpowered to demonstrate the feedback in action.

.. figure:: /_images/tutorials/pidloops/pidtune_rocket_design_maxtwr8.png

Those fuel-tank adapters are from the `Modular Rocket Systems (MRS) addon`_, but stock tanks will work just fine. The design goal of this rocket is to have a TWR of 8 on the launchpad and enough fuel to make it past 30km when throttled for optimal atmospheric efficiency.

.. _Modular Rocket Systems (MRS) addon: https://kerbalstuff.com/mod/148/Modular%20Rocket%20Systems%20-%20Parts%20Pack

.. contents:: Contents
    :local:
    :depth: 2

Proportional Feedback Loop (P-loop)
-----------------------------------

The example code from the :ref:`Design Patterns Tutorial <designpatterns>`, with some slight modifications looks like the following:

::

    // staging, throttle, steering, go
    WHEN STAGE:LIQUIDFUEL < 0.1 THEN {
        STAGE.
        PRESERVE.
    }
    LOCK THROTTLE TO 1.
    LOCK STEERING TO R(0,0,-90) + HEADING(90,90).
    STAGE.
    WAIT UNTIL SHIP:ALTITUDE > 1000.

    // P-loop setup
    SET g TO KERBIN:MU / KERBIN:RADIUS^2.
    LOCK accvec TO SHIP:SENSORS:ACC - SHIP:SENSORS:GRAV.
    LOCK gforce TO accvec:MAG / g.
    LOCK dthrott TO 0.05 * (1.2 - gforce).

    SET thrott TO 1.
    LOCK THROTTLE to thrott.

    UNTIL SHIP:ALTITUDE > 40000 {
        SET thrott to thrott + dthrott.
        WAIT 0.1.
    }

The first several lines sets up a simple staging condition, puts the throttle to maximum, steers the rocket straight up and launches. The rocket is assumed to use only liquid fuel engines. After the rocket hits 1km, the script sets up the LOCK used in the P-loop which is updated every 0.1 seconds in the UNTIL loop. The use of LOCK variables makes this code fairly clean. When the script comes up to the first line in the UNTIL loop, i.e. "SET thrott TO thrott + dthrott.", the variable dthrott is evaluated which causes the LOCK on gforce to be evaluated which in-turn causes accvec to be evaluated.

The input to this feedback loop is the acceleration experienced by the ship (gforce) in terms of Kerbin's gravitational acceleration at sea level (g). The variable accvec is the total acceleration vector and is obtained by the accelerometer and gravioli detectors, both of which must be on the ship for this to work. The variable dthrott is the change in throttle that should be applied in a single iteration of the feedback loop.

In terms of a PID loop, the factor 1.2 is called the setpoint, gforce is the process variable and 0.05 is called the proportional gain. The setpoint and gain factors can be promoted to their own variables with names. Also, the code up to and including the "WAIT UNTIL SHIP:ALTITUDE > 1000." will be implied for the next few examples of code:

::

    // P-loop
    SET g TO KERBIN:MU / KERBIN:RADIUS^2.
    LOCK accvec TO SHIP:SENSORS:ACC - SHIP:SENSORS:GRAV.
    LOCK gforce TO accvec:MAG / g.

    SET gforce_setpoint TO 1.2.
    SET Kp TO 0.05.
    LOCK dthrott TO Kp * (gforce_setpoint - gforce).

    SET thrott TO 1.
    LOCK THROTTLE to thrott.

    UNTIL SHIP:ALTITUDE > 40000 {
        SET thrott to thrott + dthrott.
        WAIT 0.1.
    }

This is not a big change, but it will set us up to include the integral and derivative terms in the next section.

Proportional-Integral Feedback Loop (PI-loop)
---------------------------------------------

Adding the integral term requires us to keep track of time. This is done by introducing a variable (t0) to store the time of the last iteration. Now, the throttle is changed only on iterations where some time has elapsed so the WAIT time in the UNTIL can be brought to 0.001. The offset of the gforce has been set to the variable P, and the integral gain to Ki.

::

    // PI-loop
    SET g TO KERBIN:MU / KERBIN:RADIUS^2.
    LOCK accvec TO SHIP:SENSORS:ACC - SHIP:SENSORS:GRAV.
    LOCK gforce TO accvec:MAG / g.

    SET gforce_setpoint TO 1.2.

    LOCK P TO gforce_setpoint - gforce.
    SET I TO 0.

    SET Kp TO 0.01.
    SET Ki TO 0.006.

    LOCK dthrott TO Kp * P + Ki * I.

    SET thrott TO 1.
    LOCK THROTTLE to thrott.

    SET t0 TO TIME:SECONDS.
    UNTIL SHIP:ALTITUDE > 40000 {
        SET dt TO TIME:SECONDS - t0.
        IF dt > 0 {
            SET I TO I + P * dt.
            SET thrott to thrott + dthrott.
            SET t0 TO TIME:SECONDS.
        }
        WAIT 0.001.
    }

Adding the integral term has the general effect of stabilizing the feedback loop, making it less prone to oscillating due to rapid changes in the process variable (gforce, in this case). This is usually at the expense of a longer settling time.

Proportional-Integral-Derivative Feedback Loop (PID-loop)
---------------------------------------------------------

Incorporating the derivative term (D) and derivative gain (Kd) requires an additional variable (P0) to keep track of the previous value of the proportional term (P).

::

    // PID-loop
    SET g TO KERBIN:MU / KERBIN:RADIUS^2.
    LOCK accvec TO SHIP:SENSORS:ACC - SHIP:SENSORS:GRAV.
    LOCK gforce TO accvec:MAG / g.

    SET gforce_setpoint TO 1.2.

    LOCK P TO gforce_setpoint - gforce.
    SET I TO 0.
    SET D TO 0.
    SET P0 TO P.

    SET Kp TO 0.01.
    SET Ki TO 0.006.
    SET Kd TO 0.006.

    LOCK dthrott TO Kp * P + Ki * I + Kd * D.

    SET thrott TO 1.
    LOCK THROTTLE to thrott.

    SET t0 TO TIME:SECONDS.
    UNTIL SHIP:ALTITUDE > 40000 {
        SET dt TO TIME:SECONDS - t0.
        IF dt > 0 {
            SET I TO I + P * dt.
            SET D TO (P - P0) / dt.
            SET thrott to thrott + dthrott.
            SET P0 TO P.
            SET t0 TO TIME:SECONDS.
        }
        WAIT 0.001.
    }

When tuned properly, the derivative term will cause the PID-loop to act quickly without causing problematic oscillations. Later in this tutorial, we will cover a way to tune a PID-loop using only the proportional term called the Zieger-Nichols method.

.. _struct_pidloop_in_tutorial:

Using :struct:`pidloop`
-----------------------

As mentioned earlier, kOS 0.18.1 introduced a new structure called :struct:`pidloop` that can take the place of much of the previous code.  Here is the previous script, converted to use :struct:`pidloop`.

::

    // pidloop
    SET g TO KERBIN:MU / KERBIN:RADIUS^2.
    LOCK accvec TO SHIP:SENSORS:ACC - SHIP:SENSORS:GRAV.
    LOCK gforce TO accvec:MAG / g.

    SET Kp TO 0.01.
    SET Ki TO 0.006.
    SET Kd TO 0.006.
    SET PID TO PIDLOOP(Kp, Ki, Kd).
    SET PID:SETPOINT TO 1.2.

    SET thrott TO 1.
    LOCK THROTTLE TO thrott.

    UNTIL SHIP:ALTITUDE > 40000 {
        SET thrott TO thrott + PID:UPDATE(TIME:SECONDS, gforce).
        // pid:update() is given the input time and input and returns the output. gforce is the input.
        WAIT 0.001.
    }

The primary advantage to using :struct:`pidloop` is the reduction in the number of instructions per update (see :attr:`Config:IPU`).  For example, this :struct:`pidloop` script requires approximately one-third the number of instructions needed by the script shown in the previous section.  Since the number of instructions executed has a direct bearing on :ref:`electrical drain <electricdrain>` as of 0.19.0, this can be a great help with power conservation.

Note that :struct:`pidloop` offers a great deal more options than were presented here, but nevertheless, this should provide a decent introduction to using :struct:`pidloop`.


Final Touches
-------------

There are a few modifications that can make PID loops very robust. The following code example adds three range limits:

#. bounds on the Integral term which addresses possible `integral windup`_
#. bounds on the throttle since it must stay in the range 0 to 1
#. a `deadband`_ to avoid changing the throttle due to small fluctuations

.. _integral windup: http://en.wikipedia.org/wiki/Integral_windup
.. _deadband: http://en.wikipedia.org/wiki/Deadband

Of course, KSP is a simulator and small fluctuations are not observed in this particular loop. Indeed, the P-loop is sufficient in this example, but all these features are included here for illustration purposes and they could become useful for unstable aircraft or untested scenarios.

::

    // PID-loop
    SET g TO KERBIN:MU / KERBIN:RADIUS^2.
    LOCK accvec TO SHIP:SENSORS:ACC - SHIP:SENSORS:GRAV.
    LOCK gforce TO accvec:MAG / g.

    SET gforce_setpoint TO 1.2.

    LOCK P TO gforce_setpoint - gforce.
    SET I TO 0.
    SET D TO 0.
    SET P0 TO P.

    LOCK in_deadband TO ABS(P) < 0.01.

    SET Kp TO 0.01.
    SET Ki TO 0.006.
    SET Kd TO 0.006.

    LOCK dthrott TO Kp * P + Ki * I + Kd * D.

    SET thrott TO 1.
    LOCK THROTTLE to thrott.

    SET t0 TO TIME:SECONDS.
    UNTIL SHIP:ALTITUDE > 40000 {
        SET dt TO TIME:SECONDS - t0.
        IF dt > 0 {
            IF NOT in_deadband {
                SET I TO I + P * dt.
                SET D TO (P - P0) / dt.

                // If Ki is non-zero, then limit Ki*I to [-1,1]
                IF Ki > 0 {
                    SET I TO MIN(1.0/Ki, MAX(-1.0/Ki, I)).
                }

                // set throttle but keep in range [0,1]
                SET thrott to MIN(1, MAX(0, thrott + dthrott)).

                SET P0 TO P.
                SET t0 TO TIME:SECONDS.
            }
        }
        WAIT 0.001.
    }

Tuning a PID-loop
-----------------

.. warning::

    **Obsolete Atmospheric Model Assumed Here.**

    The following section was written prior to Kerbal Space Program 1.0, when
    the game's atmospheric model was drastically different than it is today.
    Much of what it says is no longer applicable.  Most importantly, it is
    built on the assumption that the most efficient speed for a rocket to
    travel at is exactly at the edge of terminal velocity.  This isn't true
    in the real world, and it's no longer true in Kerbal Space Program either
    (although it was when this section was first written).

    Please take that under advisement when reading this section.  Although
    what it says is good for teaching PID loop tuning, the goal it is trying
    to seek, of holding the rocket at terminal velocity, is no longer a
    good goal to try to achieve.  A new replacement for this section should
    be written, but has not been yet.

We are going to start with the same rocket design we have been using so far and actually tune the PID-loop using the Ziegler-Nichols method. This is where we turn off the integral and derivative terms in the loop and bring the proportional gain (Kp) up from zero to the point where the loop causes a steady oscillation with a measured period (Tu). At this point, the proportional gain is called the "ultimate gain" (Ku) and the actual gains (Kp, Ki and Kd) are set according to this table `taken from wikipedia`_:

.. _taken from Wikipedia: http://en.wikipedia.org/wiki/Ziegler%E2%80%93Nichols_method

+------------------------+-----------+---------------+--------------+
| Control Type           | Kp        | Ki            | Kd           |
+========================+===========+===============+==============+
| P                      | 0.5 Ku    |               |              |
+------------------------+-----------+---------------+--------------+
| PI                     | 0.45 Ku   | 1.2 Kp / Tu   |              |
+------------------------+-----------+---------------+--------------+
| PD                     | 0.8 Ku    |               | Kp Tu / 8    |
+------------------------+-----------+---------------+--------------+
| classic PID            | 0.6 Ku    | 2 Kp / Tu     | Kp Tu / 8    |
+------------------------+-----------+---------------+--------------+
| Pessen Integral Rule   | 0.7 Ku    | 0.4 Kp / Tu   | 0.15 Kp Tu   |
+------------------------+-----------+---------------+--------------+
| some overshoot         | 0.33 Ku   | 2 Kp / Tu     | Kp Tu / 3    |
+------------------------+-----------+---------------+--------------+
| no overshoot           | 0.2 Ku    | 2 Kp / Tu     | Kp Tu / 3    |
+------------------------+-----------+---------------+--------------+

An immediate problem to overcome with this method is that it assumes a steady state can be achieved. With rockets, there is never a steady state: fuel is being consumed, altitude and therefore gravity and atmosphere is changing, staging can cause major upsets in the feedback loop. So, this tuning method will be some approximation which should come as no surprise since it will come from experimental observation. All we need is enough of a steady state that we can measure the oscillations - both the change in amplitude and the period.

.. sidebar:: Obsolete, deprecated

    This example assumes Kerbal Space Program's old
    atmospheric model which has now been obsoleted,
    and this example won't quite work as described if
    you try using it today.

    Please note the warning at the start of this section.

The script we'll use to tune the highly overpowered rocket shown will launch the rocket straight up (using SAS) and will log data to an output file until it reaches 30km at which point the log file will be copied to the archive and the program will terminate. Also, this time the feedback loop will be based on the more realistic "atmospheric efficiency." The log file will contain three columns: time since launch, offset of atmospheric efficiency from the ideal (in this case, 1.0) and the ship's maximum thrust. The maximum thrust will increase monotonically with time (this rocket has only one stage) and we'll use both as the x-axis when plotting the offset on the y-axis.

.. sidebar:: Obsolete, deprecated

    The example program here uses the suffix ``SHIP:TERMVELOCITY``,
    which no longer exists in kOS because of changes to the base
    game's atmospheric model.

    Please note the warning at the start of this section.

::

    DECLARE PARAMETER Kp.

    SWITCH TO 1. // This is the default usually, but just to be sure.

    LOCK g TO SHIP:BODY:MU / (SHIP:BODY:RADIUS + SHIP:ALTITUDE)^2.
    LOCK maxtwr TO SHIP:MAXTHRUST / (g * SHIP:MASS).

    // feedback based on atmospheric efficiency
    LOCK surfspeed TO SHIP:VELOCITY:SURFACE:MAG.
    LOCK atmoeff TO surfspeed / SHIP:TERMVELOCITY. // OBSOLETED EXAMPLE! SHIP:TERMVELOCITY No longer exists
    LOCK P TO 1.0 - atmoeff.

    SET t0 TO TIME:SECONDS.
    LOCK dthrott TO Kp*P.
    SET start_time TO t0.

    LOG "# Throttle PID Tuning" TO throttle_log.
    LOG "# Kp: " + Kp TO throttle_log.
    LOG "# t P maxtwr" TO throttle_log.

    LOCK logline TO (TIME:SECONDS - start_time)
	    + " " + P
	    + " " + maxtwr.

    SET thrott TO 1.
    LOCK THROTTLE TO thrott.
    SAS ON.
    STAGE.
    WAIT 3.

    UNTIL SHIP:ALTITUDE > 30000 {
	SET dt TO TIME:SECONDS - t0.
	IF dt > 0 {
	    SET thrott TO MIN(1,MAX(0,thrott + dthrott)).
	    SET t0 TO TIME:SECONDS.
	    LOG logline TO throttle_log.
	}
	WAIT 0.001.
    }
    COPYPATH("throttle_log", "0:/").

Give this script a short name, something like "tune.txt" so that running is simple:

::

    copypath("0:/tune", "").
    run tune(0.5).

.. sidebar:: Obsolete, deprecated

    These data values wouldn't quite come out this
    way if you tried using this example today.

    Please note the warning at the start of this section.

After every launch completes, you'll have to go into the archive directory and rename the output logfile. Something like "throttle\_log.txt" --> "throttle.01.log" will help if you increment the index number each time. To analyze the data, plot the offset (P) as a function of time (t). Here, we show the results for three values of Kp: 0.002, 0.016 and 0.160, including the maximum TWR when Kp = 0.002 as the top x-axis. The maximum TWR dependence on time is different for the three values of Kp, but not by a lot.

.. figure:: /_images/tutorials/pidloops/pidtune1.png

The value of 0.002 is obviously too low. The settling time is well over 20 seconds and the loop can't keep up with the increase in terminal velocity at the higher altitudes reached after one minute. When Kp = 0.016, the behavior is far more well behaved, and though some oscillation exists, it's damped and slow with a period of about 10 seconds. At Kp = 0.160, the oscillations are prominent and we can start to measure the change in amplitude along with the period of the oscillations. This plot shows the data for Kp = 0.160 from 20 to 40 seconds after ignition. The peaks are found and are fit to a line.

.. sidebar:: Obsolete, deprecated

    These data values wouldn't quite come out this
    way if you tried using this example today.

    Please note the warning at the start of this section.

This is done for each value of Kp and the slopes of the fitted lines are plotted as a function of Kp in the following plot:

.. figure:: /_images/tutorials/pidloops/pidtune3.png

.. sidebar:: Obsolete, deprecated

    These data values wouldn't quite come out this
    way if you tried using this example today.

    Please note the warning at the start of this section.

The period of oscillation was averaged over the interval and plotted on top of the amplitude change over time. Notice the turn over that occurs when Kp reaches approximately 0.26. This will mark the "ultimate gain" and 3.1 seconds will be used as the associated period of oscillation. It is left as an exercise for the reader to implement a full PID-loop using the classic PID values (see table above): Kp = 0.156, Ki = 0.101, Kd = 0.060, producing this behavior:

.. figure:: /_images/tutorials/pidloops/pidtune4.png

As soon as the PID-loop was activated at 3 seconds after ignition, the throttle was cut. At approximately 7 seconds, the atmospheric efficiency dropped below 100% and the integral term started to climb back to zero. At 11 seconds, the engine was reignited and the feedback loop settled after about 20 seconds. The inset plot has the same axes as the parent and shows the long-term stability of the final PID-loop.

Final Thoughts
--------------

The classic PID values used above are fairly aggressive and there is some overshoot at the beginning. This can be dealt with in many ways and is discussed on the `wikipedia page about PID controllers`_. For example, one might consider trying to implement a switch to a PD-loop when the integral term hits some limit, switching back once P crosses zero. The PID behavior should look like the following:

.. _wikipedia page about PID controllers: `PID loop`_

.. figure:: /_images/tutorials/pidloops/pidtune5.png

Finally, Controlling the throttle of a rocket is perhaps the easiest thing to implement as a PID loop in KSP using kOS. The steering was largely ignored and the orientation was always up. When writing an autopilot for horizontal atmospheric flight, one will have to deal with the direction the ship is traveling using SHIP:HEADING as well as it's orientation with SHIP:FACING. Additionally, there are the SHIP:ROTATION and SHIP:TRANSLATION vectors which can tell you the rate of change of the ship's facing and heading respectively. The controls in this case are six-dimensional using SHIP:CONTROL with YAW, PITCH, ROLL, FORE, STARBOARD, TOP and MAINTHROTTLE.

The PID gain parameters are dependent on the characteristics of the ship being controlled. The size, shape, turning capability and maximum TWR should be considered when tuning a PID loop. Turning RCS on can also have an effect and you might consider changing the PID loop's gain parameters every time to switch them on or off.
