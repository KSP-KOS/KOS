.. _pidloop:

PIDLoop
=======

The `PIDLoop` has multiple constructors available.  Valid syntax can be seen here: ::

    // Create a loop with default parameters
    // kp = 1, ki = 0, kd = 0
    // maxValue = maximum number value, extraunwind = false.
    SET PID TO PIDLOOP().

    // Other constructors include:
    SET PID TO PIDLOOP(KP).
    SET PID TO PIDLOOP(KP, KI, KD).
    SET PID TO PIDLOOP(KP, KI, KD, MAXOUTPUT).
    SET PID TO PIDLOOP(KP, KI, KD, MAXOUTPUT, EXTRAUNWIND).

    // PIDLOOP's max value is symmetric, if you require an asymmetric
    // limit, you must apply an output offset yourself.  The following
    // code will set OUT to a value between 0 and 1.
    SET OFFSET TO 0.5.
    SET PID:MAXOUTPUT TO 0.5.
    SET OUT TO PID:UPDATE(TIME:SECONDS, IN) + OFFSET.

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
    :attr:`LASTSAMPLETIME`                scalar                    decimal value of the last sample time
    :attr:`KP`                            scalar                    The proportional gain factor
    :attr:`KI`                            scalar                    The integral gain factor
    :attr:`KD`                            scalar                    The derivative gain factor
    :attr:`INPUT`                         scalar                    The most recent input value
    :attr:`SETPOINT`                      scalar                    The current setpoint
    :attr:`ERROR`                         scalar                    The most recent error value
    :attr:`OUTPUT`                        scalar                    The most recent output value
    :attr:`MAXOUTPUT`                     scalar                    The maximum output value
    :attr:`ERRORSUM`                      scalar                    The time weighted sum of error
    :attr:`PTERM`                         scalar                    The proportional component of output
    :attr:`ITERM`                         scalar                    The integral component of output
    :attr:`DTERM`                         scalar                    The derivative component of output
    :attr:`EXTRAUNWIND`                   bool                      Enables the use of aggressive integral unwind
    :attr:`CHANGERATE`                    scalar (/s)               The most recent input rate of change
    :meth:`RESET`                         none                      Reset the integral component
    :meth:`UPDATE(time, input)`           scalar                    Returns output based on time and input
    ===================================== ========================= =============

.. attribute:: PIDLoop:LASTSAMPLETIME

    :type: scalar
    :access: Get only

    The value representing the time of the last sample.  This value is equal to the time parameter of the :meth:`UPDATE` method.

.. attribute:: PIDLoop:KP

    :type: scalar
    :access: Get/Set

    The proportional gain factor.

.. attribute:: PIDLoop:KI

    :type: scalar
    :access: Get/Set

    The integral gain factor.

.. attribute:: PIDLoop:KD

    :type: scalar
    :access: Get/Set

    The derivative gain factor

.. attribute:: PIDLoop:INPUT

    :type: scalar
    :access: Get only

    The value representing the input of the last sample.  This value is equal to the input parameter of the :meth:`UPDATE` method.

.. attribute:: PIDLoop:SETPOINT

    :type: scalar
    :access: Get/Set

    The current setpoint.  This is the value to which input is compared when :meth:`UPDATE` is called.  It may not be synced with the last sample.

.. attribute:: PIDLoop:ERROR

    :type: scalar
    :access: Get only

    The calculated error from the last sample (setpoint - input).

.. attribute:: PIDLoop:OUTPUT

    :type: scalar
    :access: Get only

    The calculated output from the last sample.

.. attribute:: PIDLoop:MAXOUTPUT

    :type: scalar
    :access: Get/Set

    The current maximum output value.  This value controls the maximum and minimum output values, as well as regulating integral wind up mitigation.

.. attribute:: PIDLoop:ERRORSUM

    :type: scalar
    :access: Get only

    The value representing the time weighted sum of all errrors.  It will be equal to :attr:`ITERM` / :attr:`KI`.  This value is adjusted by the integral windup mitigation logic.

.. attribute:: PIDLoop:PTERM

    :type: scalar
    :access: Get only

    The value representing the proportional component of :attr:`OUTPUT`.

.. attribute:: PIDLoop:ITERM

    :type: scalar
    :access: Get only

    The value representing the integral component of :attr:`OUTPUT`.  This value is adjusted by the integral windup mitigation logic.

.. attribute:: PIDLoop:DTERM

    :type: scalar
    :access: Get only

    The value representing the derivative component of :attr:`OUTPUT`.

.. attribute:: PIDLoop:EXTRAUNWIND

    :type: scalar
    :access: Get/Set

    When true, :attr:`KI` will be multiplied by 2 when the sign (+ or -) of :attr:`ERROR` does not match the sign of :attr:`ERRORSUM`.  This can help to unwind the integral component more quickly after a zero crossing, and may decrease settling time.

.. attribute:: PIDLoop:CHANGERATE

    :type: scalar
    :access: Get only

    The rate of change of the :attr:`INPUT` during the last sample.  It will be equal to (input - last input) / (change in time).

.. method:: PIDLoop:RESET()

    :return: none

    Call this method to clear the :attr:`ERRORSUM` and :attr:`ITERM` components of the PID calculation.

.. method:: PIDLoop:UPDATE(time, input)

    :parameter time: (double) the decimal time in seconds
    :parameter input: (double) the input variable to compare to the setpoint
    :return: scalar representing the calculated output

    Upon calling this method, the PIDLoop will calculate the output based on this this basic framework (see below for detailed derivation): output = error * kp + errorsum * ki + (change in input) / (change in time) * kd.  This method is usually called with the current time in seconds (`TIME:SECONDS`), however it may be called using whatever rate measurement you prefer.  Windup mitigation is included, based on :attr:`MAXOUTPUT` and :attr:`EXTRAUNWIND`.  Both integral components and derivative components are guarded against a change in time greater than 1s, and will not be calculated on the first iteration.

PIDLoop Update Derivation
-------------------------

The internal update method of the :struct:`PIDLoop` structure is the equivalent of the following in kerboscript ::

    // assume that the terms LastSampleTime, Kp, Ki, Kd, Setpoint, and MaxOutput are previously defined
    declare function Update {
        declare parameter sampleTime, input.
        set Error to Setpoint - input.
        set PTerm to error * Kp.
        set ITerm to 0.
        set DTerm to 0.
        if (LastSampleTime < sampleTime) {
            set dt to sampleTime - LastSampleTime.
            if dt < 1 {
                // only calculate integral and derivative if the time
                // difference is less than one second, and their gain is not 0.
                if Ki <> 0 {
                    ITerm = (ErrorSum + Error) * dt * Ki.
                }
                set ChangeRate to (input - LastInput) / dt.
                if Kd <> 0 {
                    DTerm = ChangeRate * Kd.
                }
            }
        }
        set Output to pTerm + iTerm + dTerm.
        if abs(Output) > MaxOutput {
            // if the output goes beyond the max/min limits, reset it.
            if Output > 0 set Output to MaxOutput.
            else set Output to -MaxOutput.
            // adjust the value of ITerm as well to prevent the value
            // from winding up out of control.
            if (Ki != 0) and (LastSampleTime < sampleTime) {
                set ITerm to Output - Pterm - DTerm.
            }
        }
        LastSampleTime = sampleTime.
        if Ki <> 0 set ErrorSum to ITerm / Ki.
        else ErrorSum = 0.
        return Output.
    }
