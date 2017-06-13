.. _designpatterns:

Design Patterns and Considerations with kOS
===========================================

There are many ways one can write a control program for a given scenario. The goal of this section is to help a novice kOS programmer, after having finished the :ref:`Quick Start Tutorial <quickstart>`, to develop a sense of elegance and capability when writing his or her own kOS scripts. All of the examples in this tutorial may be tested by the reader using a rocket design similar to the following. Notice it carries an `accelerometer`_ and the `negative gravioli detector`_ which are used in the second section. Don't forget the kOS module as well!

.. _accelerometer: http://wiki.kerbalspaceprogram.com/wiki/Double-C_Seismic_Accelerometer
.. _negative gravioli detector: http://wiki.kerbalspaceprogram.com/wiki/GRAVMAX_Negative_Gravioli_Detector

.. figure:: /_images/tutorials/designpatterns/designpatterns_rocket.png

.. contents:: Contents
    :local:
    :depth: 2

The Major Design Patterns of kOS Control Programs
-------------------------------------------------

The design of a program is usually determined by the flow-control statements used. I.e., the WHEN/THEN, ON, WAIT, UNTIL, IF and FOR constructs. Here is a list of the major styles of control programs that can be written in kOS:

1. Sequential
2. Loops with Condition Checking
3. Loops with Triggers

Of course, one style does not fit all scenarios and the programmer will typically want to use a combination of these all at once. Also, there may be other design patterns not listed here which can be perfectly valid, but this is a start.

1. Sequential Programs
^^^^^^^^^^^^^^^^^^^^^^

These are programs that rely almost exclusively on WAIT UNTIL statements to go from one phase to the next.

::

    LOCK STEERING TO HEADING(0,90).
    LOCK THROTTLE TO 1.
    STAGE.
    WAIT UNTIL SHIP:ALTITUDE > 10000.
    LOCK STEERING TO HEADING(0,90) + R(0,-45,0).
    WAIT UNTIL STAGE:LIQUIDFUEL < 0.1.
    STAGE.
    WAIT UNTIL SHIP:ALTITUDE > 20000.
    LOCK THROTTLE TO 0.
    WAIT UNTIL FALSE. // CTRL+C to break out

This example will take a two stage rocket up to 20km. The immediate thing to notice is that the programmer must have known that the first stage would cutoff between 10km and 20km. This is fine for a specific rocket but not too general and could end in disaster if the first stage cutoff occurs at say 5km. Certainly, one can write a program using this technique to take a specific rocket, put it into orbit and even perform a lot of fancy maneuvers, but adapting the code to different rockets may get complicated quickly.

2. Loops with Condition Checking
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Here, we introduce IF/ELSE logic into UNTIL loops:

::

    LOCK STEERING TO R(0,0,-90) + HEADING(90,90).
    LOCK THROTTLE TO 1.
    STAGE.
    UNTIL SHIP:ALTITUDE > 20000 {
        IF SHIP:ALTITUDE > 10000 {
            LOCK STEERING TO R(0,0,-90) + HEADING(90,45).
        }
        IF STAGE:LIQUIDFUEL < 0.1 {
            STAGE.
        }
    }
    LOCK THROTTLE TO 0.
    WAIT UNTIL FALSE.

This does the same thing as the previous example, but now it's checking for a staging condition from the launch pad all the way to 20km. More than that, it will stage as many times as needed.

One can imagine that these types of UNTIL loops can become very complex with many layers of IF/ELSE blocks. Once this happens it is usually good to reduce the frequency of the loop by adding a WAIT statement at the end of the loop. This wait could be anywhere from 0.001 (every physics tick), to 60 (every minute) or even longer for inter-planetary transfers if desired.

3. Loops with Triggers
^^^^^^^^^^^^^^^^^^^^^^

In the above example, once the rocket reaches 10km, the steering is constantly being re-locked to HEADING(90,45). This works, but it only needs to be locked once. A possible improvement is to set up a trigger using a WHEN/THEN statement:

::

    LOCK STEERING TO R(0,0,-90) + HEADING(90,90).
    LOCK THROTTLE TO 1.
    STAGE.
    WHEN SHIP:ALTITUDE > 10000 THEN {
        LOCK STEERING TO R(0,0,-90) + HEADING(90,45).
    }
    UNTIL SHIP:ALTITUDE > 20000 {
        IF STAGE:LIQUIDFUEL < 0.1 {
            STAGE.
        }
    }
    LOCK THROTTLE TO 0.
    WAIT UNTIL FALSE.

Now, when the rocket reaches 10km, the steering is set once and the trigger is removed from the active list of triggers. The staging condition can also be promoted to a trigger, keeping the trigger active after every stage using the PRESERVE keyword:

::

    WHEN STAGE:LIQUIDFUEL < 0.1 THEN {
        STAGE.
        PRESERVE.
    }
    LOCK STEERING TO R(0,0,-90) + HEADING(90,90).
    LOCK THROTTLE TO 1.
    STAGE.
    WHEN SHIP:ALTITUDE > 10000 THEN {
        LOCK STEERING TO R(0,0,-90) + HEADING(90,45).
    }
    WAIT UNTIL SHIP:ALTITUDE > 20000.
    LOCK THROTTLE TO 0.
    WAIT UNTIL FALSE.

Notice that the UNTIL loop was changed to a WAIT UNTIL statement since the program is small and all the logic of the triggers can be handled in a reasonable amount of time - there will be more on this topic later.

Bringing It All Together
^^^^^^^^^^^^^^^^^^^^^^^^

Typically, the programmer will find all of these constructs are useful at the same time and kOS scripts will naturally contain some sequential parts in combination with long-term and short-term triggers which can modify states in complex loops of varying frequency. If you didn't follow that bit of gobbledygook, don't worry. The next section will discuss a few recommendations for beginning kOS programmers to follow when setting up any program.

.. _general_guidlines:

General Guidelines for kOS Scripts
----------------------------------

This section discusses two general guidelines to follow when starting out with more complicated kOS scripts. These are not meant to be absolute and there will certainly be cases when they can be stretched, though one should never totally ignore them.

.. _minimize_trigger_bodies:

1. Minimize Time Spent in WHEN/THEN Blocks
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Remember that WAIT statements are ignored when inside WHEN/THEN blocks. It is OK to loop over small lists (engines for example), but don't let it get out of hand. The WHEN/THEN construct was designed to accommodate quick bits of code. Consider this bit of (non-working) code which tries to adjust the throttle based on the g-force as measured by a combination of the accelerometer and the negative gravioli detector:

::

    SET thrott TO 1.
    LOCK THROTTLE TO thrott.
    LOCK STEERING TO R(0,0,-90) + HEADING(90,90).
    STAGE.
    WHEN SHIP:ALTITUDE > 1000 THEN {
        SET g TO KERBIN:MU / KERBIN:RADIUS^2.
        LOCK accvec TO SHIP:SENSORS:ACC - SHIP:SENSORS:GRAV.
        LOCK gforce TO accvec:MAG / g.
        LOCK dthrott TO 0.05 * (1.2 - gforce).

        UNTIL SHIP:ALTITUDE > 40000 {
            WHEN STAGE:LIQUIDFUEL < 0.1 THEN {
                STAGE.
                PRESERVE.
            }
            SET thrott to thrott + dthrott.
            WAIT 0.1.
        }
    }

This looks reasonable. The throttle is set to maximum until 1km is reached at which point the throttle is adjusted every 0.1 seconds. If the gforce is off from the value of 1.2, then the throttle is either increased or decreased by a small amount. Running this on a test rocket merely produce the message "Program ended."

Understanding why this does not work is important. Everything in a WHEN/THEN block is expected to complete in the current physics tick, but here we have a loop that is supposed to last until the ship reaches 40km. This example can be reworked by separating the triggers from the loop. The staging trigger was separated from the UNTIL loop as well - not strictly necessary, but recommended form:

::

    WHEN STAGE:LIQUIDFUEL < 0.1 THEN {
        STAGE.
        PRESERVE.
    }
    SET thrott TO 1.
    SET dthrott TO 0.
    LOCK THROTTLE TO thrott.
    LOCK STEERING TO R(0,0,-90) + HEADING(90,90).
    STAGE.
    WHEN SHIP:ALTITUDE > 1000 THEN {
        SET g TO KERBIN:MU / KERBIN:RADIUS^2.
        LOCK accvec TO SHIP:SENSORS:ACC - SHIP:SENSORS:GRAV.
        LOCK gforce TO accvec:MAG / g.
        LOCK dthrott TO 0.05 * (1.2 - gforce).
    }
    UNTIL SHIP:ALTITUDE > 40000 {
        SET thrott to thrott + dthrott.
        WAIT 0.1.
    }

Now this program should work. The variable dthrott had to be set to 0 in the beginning so that the throttle is kept at maximum until 1km, the UNTIL loop operates every 0.1 seconds, and the WHEN/THEN triggers are run only once when the condition is met. The take-away from this example is to keep WHEN/THEN blocks separate from UNTIL loops. Specifically, never put an UNTIL loop inside a WHEN/THEN block and it should be extremely rare to put a WHEN/THEN statement inside an UNTIL loop.

Finally, as a bit of foreshadowing, this bit of code is actually a "`proportional feedback loop <http://en.wikipedia.org/wiki/PID_controller>`__." From an altitude of 1km up to 40km, the total g-force exerted on the ship is kept near 1.2 by constantly adjusting the throttle. The value of 1.2 is called the "setpoint," the measured g-force is called the "process variable," and the mystical 0.05 is called the "proportional gain." Please take a look at the `PID Loop Tutorial <pidloops.html>`__ which takes this script as a starting point and develops a full PID-loop in kOS.

.. _minimize_trigger_conditions:

2. Minimize Trigger Conditions
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

There is a lot of power in developing multi-level LOCK variables in combination with WHEN/THEN triggers. However, it can be easy to hit kOS's hard limit in the number of operations allowed for trigger checking. This will happen when several WHEN/THEN triggers are dependent on the same complex LOCK variable. This results in the LOCK variable being calculated multiple times every update. If the LOCK is deep enough, the calculations become too expensive to do and kOS stops executing and complains.

With this in mind, consider an extension of the example script in the previous section. This time, the g-force setpoint changes as the rocket climbs through 10km, 20km and 30km:

::

    WHEN STAGE:LIQUIDFUEL < 0.1 THEN {
        STAGE.
        PRESERVE.
    }
    SET thrott TO 1.
    SET dthrott TO 0.
    LOCK THROTTLE TO thrott.
    LOCK STEERING TO R(0,0,-90) + HEADING(90,90).
    STAGE.
    WHEN SHIP:ALTITUDE > 1000 THEN {
        SET g TO KERBIN:MU / KERBIN:RADIUS^2.
        LOCK accvec TO SHIP:SENSORS:ACC - SHIP:SENSORS:GRAV.
        LOCK gforce TO accvec:MAG / g.
        LOCK dthrott TO 0.05 * (1.2 - gforce).
    }
    WHEN SHIP:ALTITUDE > 10000 THEN {
        LOCK dthrott TO 0.05 * (2.0 - gforce).
    }
    WHEN SHIP:ALTITUDE > 20000 THEN {
        LOCK dthrott TO 0.05 * (4.0 - gforce).
    }
    WHEN SHIP:ALTITUDE > 30000 THEN {
        LOCK dthrott TO 0.05 * (5.0 - gforce).
    }
    UNTIL SHIP:ALTITUDE > 40000 {
        SET thrott to thrott + dthrott.
        WAIT 0.1.
    }

This example does what is expected of it without problems. But the ship's altitude is being checked at least five times for every update, including the UNTIL loop check. Certainly, the kOS CPU can keep up with this, however, one can imagine a whole series of WHEN/THEN statements which make use of complicated calculations based on atmospheric data or orbital mechanics. One way to minimize the trigger condition checking is to take strictly-sequential triggers and nest them:

::

    WHEN STAGE:LIQUIDFUEL < 0.1 THEN {
        STAGE.
        PRESERVE.
    }
    SET thrott TO 1.
    SET dthrott TO 0.
    LOCK THROTTLE TO thrott.
    LOCK STEERING TO R(0,0,-90) + HEADING(90,90).
    STAGE.
    WHEN SHIP:ALTITUDE > 1000 THEN {
        SET g TO KERBIN:MU / KERBIN:RADIUS^2.
        LOCK accvec TO SHIP:SENSORS:ACC - SHIP:SENSORS:GRAV.
        LOCK gforce TO accvec:MAG / g.
        LOCK dthrott TO 0.05 * (1.2 - gforce).

        WHEN SHIP:ALTITUDE > 10000 THEN {
            LOCK dthrott TO 0.05 * (2.0 - gforce).

            WHEN SHIP:ALTITUDE > 20000 THEN {
                LOCK dthrott TO 0.05 * (4.0 - gforce).

                WHEN SHIP:ALTITUDE > 30000 THEN {
                    LOCK dthrott TO 0.05 * (5.0 - gforce).
                }
            }
        }
    }
    UNTIL SHIP:ALTITUDE > 40000 {
        SET thrott to thrott + dthrott.
        WAIT 0.1.
    }

Now this is quite elegant! The number of triggers have been reduced to two per update for the entire running of this script. The trigger at 1km sets up the next trigger which will happen at 10km which sets up then next at 20km and so on. This can save a lot of processing time for triggers that will happen sequentially. As a general rule, one should try to nest WHEN/THEN statements whenever possible. Again, both examples above will work, but when scripts start to have deep and complicated triggers, this nested construct can save it from the dreaded kOS trigger limit.
