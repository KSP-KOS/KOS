.. _pidloop:

PIDLoop
=======

The `PIDLoop` has multiple constructors available.  Valid syntax can be seen here: ::

    // Create a loop with default parameters
    // kp = 1, ki = 0, kd = 0
    // maxoutput = maximum number value
    // minoutput = minimum number value
    SET PID TO PIDLOOP().

    // Other constructors include:
    SET PID TO PIDLOOP(KP).
    SET PID TO PIDLOOP(KP, KI, KD).
    // you must specify both minimum and maximum output directly.
    SET PID TO PIDLOOP(KP, KI, KD, MINOUTPUT, MAXOUTPUT).

    // remember to update both minimum and maximum output if the value changes symmetrically
    SET LIMIT TO 0.5.
    SET PID:MAXOUTPUT TO LIMIT.
    SET PID:MINOUTPUT TO -LIMIT.

    // call the update suffix to get the output
    SET OUT TO PID:UPDATE(TIME:SECONDS, IN).

    // you can also get the output value later from the PIDLoop object
    SET OUT TO PID:OUTPUT.

Please see the bottom of this page for information on the derivation of the loop's output.

.. note::

    .. versionadded:: 0.18
        While the `PIDLOOP` structure was added in version 0.18, you may feel free to continue to use any
        previously implemented PID logic.  This loop is intended to be a basic and flexible PID, but you
        may still find benefit in using customized logic.


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
    :attr:`ERRORSUM`                      :struct:`scalar`          The time weighted sum of error
    :attr:`PTERM`                         :struct:`scalar`          The proportional component of output
    :attr:`ITERM`                         :struct:`scalar`          The integral component of output
    :attr:`DTERM`                         :struct:`scalar`          The derivative component of output
    :attr:`CHANGERATE`                    :struct:`scalar` (/s)     The most recent input rate of change
    :meth:`RESET`                         none                      Reset the integral component
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

    The derivative gain factor

.. attribute:: PIDLoop:INPUT

    :type: :struct:`scalar`
    :access: Get only

    The value representing the input of the last sample.  This value is equal to the input parameter of the :meth:`UPDATE` method.

.. attribute:: PIDLoop:SETPOINT

    :type: :struct:`scalar`
    :access: Get/Set

    The current setpoint.  This is the value to which input is compared when :meth:`UPDATE` is called.  It may not be synced with the last sample.

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

    Call this method to clear the :attr:`ERRORSUM` and :attr:`ITERM` components of the PID calculation.

.. method:: PIDLoop:UPDATE(time, input)

    :parameter time: (:struct:`scalar`) the decimal time in seconds
    :parameter input: (:struct:`scalar`) the input variable to compare to the setpoint
    :return: :struct:`scalar` representing the calculated output

    Upon calling this method, the PIDLoop will calculate the output based on this this basic framework (see below for detailed derivation): output = error * kp + errorsum * ki + (change in input) / (change in time) * kd.  This method is usually called with the current time in seconds (`TIME:SECONDS`), however it may be called using whatever rate measurement you prefer.  Windup mitigation is included, based on :attr:`MAXOUTPUT` and :attr:`MINOUTPUT`.  Both integral components and derivative components are guarded against a change in time greater than 1s, and will not be calculated on the first iteration.

PIDLoop Update Derivation
-------------------------

The internal update method of the :struct:`PIDLoop` structure is the equivalent of the following in kerboscript ::

    // assume that the terms LastInput, LastSampleTime, ErrorSum, Kp, Ki, Kd, Setpoint, MinOutput, and MaxOutput are previously defined
    declare function Update {
        declare parameter sampleTime, input.
        set Error to Setpoint - input.
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
