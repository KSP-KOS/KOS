.. _pidloop:

PIDLoop
=======

A PID loop is a standard control mechanism taught in many control
theory textbooks.  It's a simple way to deal with a scenario where
you want a program to move a control lever of some sort in order
to cause a measured phenomenon to settle on a target value.  That
base description actually covers many scenarios:

  * A car's cruise control moving the accelerator
    pedal to seek a target speed.
  * A ship at sea moving the rudder to seek a target
    compass heading.
  * An electric oven changing the amount of current to
    the heating element to seek a target temperature.
  * .. etc ..

It should be clear why such a thing would come up a lot in kOS,
which is all about having a computer move the controls to seek
some kind of target result.

PID stands for "Proportional, Integral, Derivative".  It tracks
the thing you are measuring and how it changes over time, and
uses that to decide how you should set the control.

The kOS documenation has a 
:ref:`tutorial about what PID loops are <pidloops>` that goes over the
general principles and shows code examples of how to create your
own PID loop with your own script code.

But, you don't actually need to do that (make your own PID loop code)
because kOS comes with a built-in PID loop type you can use, which is
what this page describes.  However, to make sense out of what these
terms mean and what they are for, it's still recommended that you
read the :ref:`tutorial about what PID loops are <pidloops>` first.

Constructors
------------

The `PIDLoop` constructor function has a number of parametres, and
they can be left off, leaving optional defaults.

The full version is this::

  SET MYPID TO PIDLOOP(Kp, Ki, Kd, min_output, max_output, epsilon).

With these defaults:

  .. list-table::
    :header-rows: 1
    :widths: 1 2 3

    * - Parameter
      - Default if left off
      - Meaning
    
    * - Kp
      - 1.0
      - Proportional gain
    * - Ki
      - 0.0
      - Integral gain
    * - Kd
      - 0.0
      - Derivative gain
    * - min_output
      - Very Big negative number. about -1.7e308
      - The minimum value this PIDLoop will output.
    * - max_output
      - Very Big positive number. about -1.7e308
      - The maximum value this PIDLoop will output.
    * - epsilon
      - 0.0
      - The PIDLoop will ignore any input error smaller than this.

Examples
~~~~~~~~

::

    // With zero parameters, the PIDLoop has default parameters:
    // kp = 1, ki = 0, kd = 0
    // maxoutput = maximum number value
    // minoutput = minimum number value
    SET PID TO PIDLOOP().

    // Create a PIDLoop with a few stated paremeters:
    SET PID TO PIDLOOP(2.5).
    SET PID TO PIDLOOP(2.0, 0.05, 0.1).
    SET PID TO PIDLOOP(2.0, 0.05, 0.1, -1, 1).

Example of using it:
~~~~~~~~~~~~~~~~~~~~

::

    // Throttle up when below target altitude, throttle down when above
    // target altitude, trying to hover:
    local target_alt is 100.
    set HOVERPID to PIDLOOP(
        50,  // adjust throttle 0.1 per 5m in error from desired altitude.
        0.1, // adjust throttle 0.1 per second spent at 1m error in altitude.
        30,  // adjust throttle 0.1 per 3 m/s speed toward desired altitude.
        0,   // min possible throttle is zero.
        1    // max possible throttle is one.
      ).
    set HOVERPID:SETPOINT to target_alt.
    set mythrot to 0.
    lock throttle to mythrot.
    until false {
      set mythrot to HOVERPID:UPDATE(TIME:SECONDS, alt:radar).
      wait 0.
    }
    
Please see the bottom of this page for information on the derivation of the loop's output.

.. _basic_pidloop_example:

Basic PIDloop Example
---------------------

Here is a simple example that would move a throttle on a hovering
rocket to get it to settle in at an ``alt:radar`` of 100 meters::

    // Example PIDLoop usage to hover a rocket at 100 meters off the ground:
    // Please use it with a rocket that has lots of fuel to test it,
    // and a TWR between about 1.75 and 2.0.

    lock steering to up.

    print "Setting up PID structure:".
    set hoverPID to PIDLoop(0.02, 0.0015, 0.02, 0, 1).
    set hoverPID:SETPOINT to 100.

    set wanted_throttle to 0. // for now.
    lock throttle to wanted_throttle.

    print "Now starting loop:".
    print "Make sure you stage until the engine is active.".
    print "You will have to kill it with CTRL-C".
    until false {
      set wanted_throttle to hoverPID:UPDATE(time:seconds, alt:radar).
      print "Radar Alt " + round(alt:radar,1) + "m, PID wants throttle=" + round(wanted_throttle,3).
      wait 0.
    }


.. _please_use_setpoint:

Using SETPOINT is better than using Zero
----------------------------------------

The :struct:`PIDloop` type has a :attr:`SETPOINT` suffix, which tells the
loop what the desired target value is that the loop should be seeking.
In most cases the result of using :attr:`SETPOINT` would be the
same as just adjusting the value yourself to center it around zero,
and you'd think the following two would product identical results,
but they don't:

Version (A)::

  // assume `wanted` is a variable with the desired target value:
  // when initializing, do:
  set myPid to PIDLOOP(1, 0.2, 0.02, -1, 1).

  // later, when updating in a loop, do:
  set ctrl to myPid:UPDATE(time:seconds, measurement - wanted).

Version (B)::

  // assume `wanted` is a variable with the desired target value:
  // when initializing, do:
  set myPid to PIDLOOP(1, 0.2, 0.02, -1, 1).
  set myPid:SETPOINT to wanted.

  // Later, when updating in a loop, do:
  set ctrl to myPid:UPDATE(time:seconds, measurement).

Actually, with kOS's PIDLoop, the second version, Version(B), works a bit better
and should be preferred.  The reason is that when calcualting the D term,
:struct:`PIDLoop` uses the change in the raw measure, not the error of the
the measure, to calculate the rate of change of the value.  This becomes
relevant when your script suddenly changes its mind what the target value is
supposed to be.  If you change your mind when using Version(B) above, by
saying ``Set MyPid:SETPOINT to newValue.``, then the PIDLoop is aware that
the D value didn't actually suddenly change to an enormously large number,
because it measures the change in raw value not the change in error.  If
using Version(A), the PIDLoop would think there has been a huge sudden
large change all at once, and thus the D term calculation would take
that to mean it needs to violently counteract that change.  Using Version(B)
with kOS's PIDLoop allows it to respond to the change less violently
because it knows the actual raw value didn't suddenly jump - just the 
desired target value did.

Structure
---------

.. structure:: PIDLoop

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
    :attr:`LASTSAMPLETIME`                :struct:`scalar`          decimal value of the last sample time
    :attr:`KP`                            :struct:`scalar`          The proportional gain factor
    :attr:`KI`                            :struct:`scalar`          The integral gain factor
    :attr:`KD`                            :struct:`scalar`          The derivative gain factor
    :attr:`INPUT`                         :struct:`scalar`          The most recent input value
    :attr:`SETPOINT`                      :struct:`scalar`          The current setpoint
    :attr:`ERROR`                         :struct:`scalar`          The most recent error value
    :attr:`OUTPUT`                        :struct:`scalar`          The most recent output value
    :attr:`MAXOUTPUT`                     :struct:`scalar`          The maximum output value
    :attr:`MINOUTPUT`                     :struct:`scalar`          The maximum output value
    :attr:`EPSILON`                       :struct:`scalar`          The "don't care" tolerance of error
    :attr:`IGNOREERROR`                   :struct:`scalar`          Alias for :attr:`EPSILON`.
    :attr:`ERRORSUM`                      :struct:`scalar`          The time weighted sum of error
    :attr:`PTERM`                         :struct:`scalar`          The proportional component of output
    :attr:`ITERM`                         :struct:`scalar`          The integral component of output
    :attr:`DTERM`                         :struct:`scalar`          The derivative component of output
    :attr:`CHANGERATE`                    :struct:`scalar` (/s)     The most recent input rate of change
    :meth:`RESET`                         none                      Reset the integral and derivative components
    :meth:`UPDATE(time, input)`           :struct:`scalar`          Returns output based on time and input
    ===================================== ========================= =============

.. attribute:: PIDLoop:LASTSAMPLETIME

    :type: :struct:`scalar`
    :access: Get only

    The value representing the time of the last sample.  This value is equal to the time parameter of the :meth:`UPDATE` method.

.. attribute:: PIDLoop:KP

    :type: :struct:`scalar`
    :access: Get/Set

    The proportional gain factor.

.. attribute:: PIDLoop:KI

    :type: :struct:`scalar`
    :access: Get/Set

    The integral gain factor.

.. attribute:: PIDLoop:KD

    :type: :struct:`scalar`
    :access: Get/Set

    The derivative gain factor.

.. attribute:: PIDLoop:INPUT

    :type: :struct:`scalar`
    :access: Get only

    The value representing the input of the last sample.  This value is equal to the input parameter of the :meth:`UPDATE` method.

.. attribute:: PIDLoop:SETPOINT

    :type: :struct:`scalar`
    :access: Get/Set

    The current setpoint.  This is the value to which input is compared when :meth:`UPDATE` is called.  It may not be synced with the last sample.

    It is desirable to use :attr:`SETPOINT` for the
    :ref:`reasons described above <please_use_setpoint>`.


.. attribute:: PIDLoop:ERROR

    :type: :struct:`scalar`
    :access: Get only

    The calculated error from the last sample (setpoint - input).

.. attribute:: PIDLoop:OUTPUT

    :type: :struct:`scalar`
    :access: Get only

    The calculated output from the last sample.

.. attribute:: PIDLoop:MAXOUTPUT

    :type: :struct:`scalar`
    :access: Get/Set

    The current maximum output value.  This value also helps with regulating integral wind up mitigation.

.. attribute:: PIDLoop:MINOUTPUT

    :type: :struct:`scalar`
    :access: Get/Set

    The current minimum output value.  This value also helps with regulating integral wind up mitigation.


.. attribute:: PIDLoop:EPSILON

    :type: :struct:`scalar`
    :access: Get/Set

    Default = 0.

    The size of the "don't care" tolerance window of the error measurement.

    When the error measurement (difference between input and setpoint) is smaller
    than this number, then this PID loop will simply *pretend* the error is
    actually zero and react accordingly (it won't output any control deflection
    to bother correcting the error until after it's bigger than epsilon.)
    This can be handy when you want a null zone in the input measure.  (This is
    different from having a null zone in the output, as in having a lever
    that can't do anything unless it's moved far enough.  This is more of a
    null zone on the input measurement.)

    (In the PIDLoops that are contained internally within the
    :struct:`SteeringManager` that ``lock steering`` uses, they use this
    epsilon to try to reduce the use of RCS propellant that comes from
    wiggling the controls unnecessarily.)

    Because the PIDloop will pretend any error smaller than epsilon is zero,
    it also will not incur any "integral windup" for that error.

.. attribute:: PIDLoop:IGNOREERROR

    :type: :struct:`scalar`
    :access: Get/Set

    This is just an alias that is the same thing as :attr:`EPSILON`.

.. attribute:: PIDLoop:ERRORSUM

    :type: :struct:`scalar`
    :access: Get only

    The value representing the time weighted sum of all errrors.  It will be equal to :attr:`ITERM` / :attr:`KI`.  This value is adjusted by the integral windup mitigation logic.

.. attribute:: PIDLoop:PTERM

    :type: :struct:`scalar`
    :access: Get only

    The value representing the proportional component of :attr:`OUTPUT`.

.. attribute:: PIDLoop:ITERM

    :type: :struct:`scalar`
    :access: Get only

    The value representing the integral component of :attr:`OUTPUT`.  This value is adjusted by the integral windup mitigation logic.

.. attribute:: PIDLoop:DTERM

    :type: :struct:`scalar`
    :access: Get only

    The value representing the derivative component of :attr:`OUTPUT`.

.. attribute:: PIDLoop:CHANGERATE

    :type: :struct:`scalar`
    :access: Get only

    The rate of change of the :attr:`INPUT` during the last sample.  It will be equal to (input - last input) / (change in time).

.. method:: PIDLoop:RESET()

    :return: none

    Call this method to clear the :attr:`ERRORSUM`, :attr:`ITERM`, and :attr:`LASTSAMPLETIME` components of the PID calculation.

.. method:: PIDLoop:UPDATE(time, input)

    :parameter time: (:struct:`scalar`) the decimal time in seconds
    :parameter input: (:struct:`scalar`) the input variable to compare to the setpoint
    :return: :struct:`scalar` representing the calculated output

    Upon calling this method, the PIDLoop will calculate the output based on this this basic framework (see below for detailed derivation): output = error * kp + errorsum * ki + (change in input) / (change in time) * kd.  This method is usually called with the current time in seconds (`TIME:SECONDS`), however it may be called using whatever rate measurement you prefer.  Windup mitigation is included, based on :attr:`MAXOUTPUT` and :attr:`MINOUTPUT`.  Both integral components and derivative components are guarded against a change in time greater than 1s, and will not be calculated on the first iteration.

PIDLoop Update Derivation
-------------------------

The internal update method of the :struct:`PIDLoop` structure is the equivalent of the following in kerboscript ::

    // assume that the terms LastInput, LastSampleTime, ErrorSum, Kp, Ki, Kd, Setpoint, MinOutput, MaxOutput, and Epsilon are previously defined
    declare function Update {
        declare parameter sampleTime, input.
        set Error to Setpoint - input.
        if Error > -Epsilon and Error < Epsilon {
          set Error to 0. // pretend there is no error.
          set input to Setpoint. // pretend there is no error.
        }
        set PTerm to error * Kp.
        set ITerm to 0.
        set DTerm to 0.
        if (LastSampleTime < sampleTime) {
            set dt to sampleTime - LastSampleTime.
            // only calculate integral and derivative if their gain is not 0.
            if Ki <> 0 {
                set ITerm to (ErrorSum + Error * dt) * Ki.
            }
            set ChangeRate to (input - LastInput) / dt.
            if Kd <> 0 {
                set DTerm to -ChangeRate * Kd.
            }
        }
        set Output to pTerm + iTerm + dTerm.
        // if the output goes beyond the max/min limits, reset it and adjust ITerm.
        if Output > MaxOutput {
            set Output to MaxOutput.
            // adjust the value of ITerm as well to prevent the value
            // from winding up out of control.
            if (Ki <> 0) and (LastSampleTime < sampleTime) {
                set ITerm to Output - min(Pterm + DTerm, MaxOutput).
            }
        }
        else if Output < MinOutput {
            set Output to MinOutput.
            // adjust the value of ITerm as well to prevent the value
            // from winding up out of control.
            if (Ki <> 0) and (LastSampleTime < sampleTime) {
                set ITerm to Output - max(Pterm + DTerm, MinOutput).
            }
        }
        set LastSampleTime to sampleTime.
        set LastInput to input.
        if Ki <> 0 set ErrorSum to ITerm / Ki.
        else set ErrorSum to 0.
        return Output.
    }
