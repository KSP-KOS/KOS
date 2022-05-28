.. _steeringmanager:

SteeringManager
===============

See :ref:`the cooked control tuning explanation <cooked_tuning>` for
information to help with tuning the steering manager.  It's important to read
that section first to understand which setting below is affecting which
portion of the steering system.

The SteeringManager is a bound variable, not a suffix to a specific vessel.  This prevents access to the SteeringManager of other vessels.  You can access the steering manager as shown below: ::

    // Display the ship facing, target facing, and world coordinates vectors.
    SET STEERINGMANAGER:SHOWFACINGVECTORS TO TRUE.

    // Change the torque calculation to multiply the available torque by 1.5.
    SET STEERINGMANAGER:ROLLTORQUEFACTOR TO 1.5.

.. structure:: SteeringManager

    ==================================== ========================= =============
    Suffix                               Type                      Description
    ==================================== ========================= =============
    :attr:`PITCHPID`                     :struct:`PIDLoop`         The PIDLoop for the pitch :ref:`rotational velocity PID <cooked_omega_pid>`.
    :attr:`YAWPID`                       :struct:`PIDLoop`         The PIDLoop for the yaw :ref:`rotational velocity PID <cooked_omega_pid>`.
    :attr:`ROLLPID`                      :struct:`PIDLoop`         The PIDLoop for the roll :ref:`rotational velocity PID <cooked_omega_pid>`.
    :attr:`ENABLED`                      :struct:`boolean`         Returns true if the `SteeringManager` is currently controlling the vessel
    :attr:`TARGET`                       :struct:`Direction`       The direction that the vessel is currently steering towards
    :meth:`RESETPIDS()`                  none                      Called to call `RESET` on all steering PID loops.
    :meth:`RESETTODEFAULT()`             none                      Called to reset all steering tuning parameters.
    :attr:`SHOWFACINGVECTORS`            :struct:`boolean`         Enable/disable display of ship facing, target, and world coordinates vectors.
    :attr:`SHOWANGULARVECTORS`           :struct:`boolean`         Enable/disable display of angular rotation vectors
    :attr:`SHOWSTEERINGSTATS`            :struct:`boolean`         Enable/disable printing of the steering information on the terminal
    :attr:`WRITECSVFILES`                :struct:`boolean`         Enable/disable logging steering to csv files.
    :attr:`PITCHTS`                      :struct:`scalar` (s)      Settling time for the pitch torque calculation.
    :attr:`YAWTS`                        :struct:`scalar` (s)      Settling time for the yaw torque calculation.
    :attr:`ROLLTS`                       :struct:`scalar` (s)      Settling time for the roll torque calculation.
    :attr:`TORQUEEPSILONMIN`             :struct:`scalar` (s)      Torque deadzone when not rotating at max rate
    :attr:`TORQUEEPSILONMAX`             :struct:`scalar` (s)      Torquw deadzone when rotating at max roatation rate
    :attr:`MAXSTOPPINGTIME`              :struct:`scalar` (s)      The maximum amount of stopping time to limit angular turn rate.
    :attr:`ROLLCONTROLANGLERANGE`        :struct:`scalar` (deg)    The maximum value of :attr:`ANGLEERROR` for which to control roll.
    :attr:`ANGLEERROR`                   :struct:`scalar` (deg)    The angle between vessel:facing and target directions
    :attr:`PITCHERROR`                   :struct:`scalar` (deg)    The angular error in the pitch direction
    :attr:`YAWERROR`                     :struct:`scalar` (deg)    The angular error in the yaw direction
    :attr:`ROLLERROR`                    :struct:`scalar` (deg)    The angular error in the roll direction
    :attr:`PITCHTORQUEADJUST`            :struct:`scalar` (kN)     Additive adjustment to pitch torque (calculated)
    :attr:`YAWTORQUEADJUST`              :struct:`scalar` (kN)     Additive adjustment to yaw torque (calculated)
    :attr:`ROLLTORQUEADJUST`             :struct:`scalar` (kN)     Additive adjustment to roll torque (calculated)
    :attr:`PITCHTORQUEFACTOR`            :struct:`scalar`          Multiplicative adjustment to pitch torque (calculated)
    :attr:`YAWTORQUEFACTOR`              :struct:`scalar`          Multiplicative adjustment to yaw torque (calculated)
    :attr:`ROLLTORQUEFACTOR`             :struct:`scalar`          Multiplicative adjustment to roll torque (calculated)
    ==================================== ========================= =============

.. warning::
    .. versionadded:: v0.20.1
        The suffixes ``SHOWRCSVECTORS`` and ``SHOWTHRUSTVECTORS`` were
        deprecated with the move to using stock torque calculation with KSP 1.1.

.. attribute:: SteeringManager:PITCHPID

    :type: :struct:`PIDLoop`
    :access: Get only

    Returns the PIDLoop object responsible for calculating the :ref:`target angular velocity <cooked_omega_pid>` in the pitch direction.  This allows direct manipulation of the gain parameters, and other components of the :struct:`PIDLoop` structure.  Changing the loop's `MAXOUTPUT` or `MINOUTPUT` values will have no effect as they are overwritten every physics frame.  They are set to limit the maximum turning rate to that which can be stopped in a :attr:`MAXSTOPPINGTIME` seconds (calculated based on available torque, and the ship's moment of inertia).

.. attribute:: SteeringManager:YAWPID

    :type: :struct:`PIDLoop`
    :access: Get only

    Returns the PIDLoop object responsible for calculating the :ref:`target angular velocity <cooked_omega_pid>` in the yaw direction.  This allows direct manipulation of the gain parameters, and other components of the :struct:`PIDLoop` structure.  Changing the loop's `MAXOUTPUT` or `MINOUTPUT` values will have no effect as they are overwritten every physics frame.  They are set to limit the maximum turning rate to that which can be stopped in a :attr:`MAXSTOPPINGTIME` seconds (calculated based on available torque, and the ship's moment of inertia).

.. attribute:: SteeringManager:ROLLPID

    :type: :struct:`PIDLoop`
    :access: Get only

    Returns the PIDLoop object responsible for calculating the :ref:`target angular velocity <cooked_omega_pid>` in the roll direction.  This allows direct manipulation of the gain parameters, and other components of the :struct:`PIDLoop` structure.  Changing the loop's `MAXOUTPUT` or `MINOUTPUT` values will have no effect as they are overwritten every physics frame.  They are set to limit the maximum turning rate to that which can be stopped in a :attr:`MAXSTOPPINGTIME` seconds (calculated based on available torque, and the ship's moment of inertia).

    .. note::

        The SteeringManager will ignore the roll component of steering
        until after both the pitch and yaw components are close to being
        correct.  In other words it will try to point the nose of the
        craft in the right direction first, before it makes any attempt
        to roll the craft into the right orientation.  As long as the
        pitch or yaw is still far off from the target aim, this PIDloop
        won't be getting used at all.

.. attribute:: SteeringManager:ENABLED

    :type: :ref:`boolean <boolean>`
    :access: Get only

    Returns true if the SteeringManager is currently controlling the vessel steering.

.. attribute:: SteeringManager:TARGET

    :type: :struct:`Direction`
    :access: Get only

    Returns direction that the is currently being targeted.  If steering is locked to a vector, this will return the calculated direction in which kOS chose an arbitrary roll to go with the vector.  If steering is locked to "kill", this will return the vessel's last facing direction.

.. method:: SteeringManager:RESETPIDS

    :return: none

    Resets the integral sum to zero for all six steering PID Loops.

.. method:: SteeringManager:RESETTODEFAULT

    :return: none

    Resets the various tuning parameters of the :struct:`SteeringManager` to
    their default values as if the ship had just been loaded.  This internally
    will also call :meth:`SteeringManager:RESETPIDS`.

.. attribute:: SteeringManager:SHOWFACINGVECTORS

    :type: :ref:`boolean <boolean>`
    :access: Get/Set

    Setting this suffix to true will cause the steering manager to display graphical vectors (see :struct:`VecDraw`) representing the forward, top, and starboard of the facing direction, as well as the world x, y, and z axis orientation (centered on the vessel).  Setting to false will hide the vectors, as will disabling locked steering.

.. attribute:: SteeringManager:SHOWANGULARVECTORS

    :type: :ref:`boolean <boolean>`
    :access: Get/Set

    Setting this suffix to true will cause the steering manager to display graphical vectors (see :struct:`VecDraw`) representing the current and target angular velocities in the pitch, yaw, and roll directions.  Setting to false will hide the vectors, as will disabling locked steering.

.. attribute:: SteeringManager:SHOWSTEERINGSTATS

    :type: :ref:`boolean <boolean>`
    :access: Get/Set

    Setting this suffix to true will cause the steering manager to clear the terminal screen and print steering data each update.

.. attribute:: SteeringManager:WRITECSVFILES

    :type: :ref:`boolean <boolean>`
    :access: Get/Set

    Setting this suffix to true will cause the steering manager log the data from all 6 PIDLoops calculating target angular velocity and target torque.  The files are stored in the `[KSP Root]\GameData\kOS\Plugins\PluginData\kOS` folder, with one file per loop and a new file created for each new manager instance (i.e. every launch, every revert, and every vessel load).  These files can grow quite large, and add up quickly, so it is recommended to only set this value to true for testing or debugging and not normal operation.

.. attribute:: SteeringManager:PITCHTS

    :type: :ref:`scalar <scalar>`
    :access: Get/Set

    Represents the settling time for the :ref:`PID calculating pitch torque based on target angular velocity <cooked_torque_pid>`.  The proportional and integral gain is calculated based on the settling time and the moment of inertia in the pitch direction.  Ki = (moment of inertia) * (4 / (settling time)) ^ 2.  Kp = 2 * sqrt((moment of inertia) * Ki).

.. attribute:: SteeringManager:YAWTS

    :type: :ref:`scalar <scalar>`
    :access: Get/Set

    Represents the settling time for the :ref:`PID calculating yaw torque based on target angular velocity <cooked_torque_pid>`.  The proportional and integral gain is calculated based on the settling time and the moment of inertia in the yaw direction.  Ki = (moment of inertia) * (4 / (settling time)) ^ 2.  Kp = 2 * sqrt((moment of inertia) * Ki).

.. attribute:: SteeringManager:ROLLTS

    :type: :ref:`scalar <scalar>`
    :access: Get/Set

    Represents the settling time for the :ref:`PID calculating roll torque based on target angular velocity <cooked_torque_pid>`.  The proportional and integral gain is calculated based on the settling time and the moment of inertia in the roll direction.  Ki = (moment of inertia) * (4 / (settling time)) ^ 2.  Kp = 2 * sqrt((moment of inertia) * Ki).

.. attribute:: SteeringManager:TORQUEEPSILONMIN

    :type: :ref:`scalar <scalar>`
    :access: Get/Set

    DEFAULT VALUE: 0.0002

    Tweaking this value can help make the controls stop wiggling so fast.

    You cannot set this value higher than
    :attr:`SteeringManager:TORQUEEPSILONMAX`.
    If you attempt to do so, then
    :attr:`SteeringManager:TORQUEEPSILONMAX` will be increased to match
    the value just set :attr:`SteeringManager:TORQUEEPSILONMIN` to.

    To see how to use this value, look at the description of
    :attr:`SteeringManager:TORQUEEPSILONMAX` below, which
    has the full documentation about how these two values, Min and Max,
    work together.

.. attribute:: SteeringManager:TORQUEEPSILONMAX

    :type: :ref:`scalar <scalar>`
    :access: Get/Set

    DEFAULT VALUE: 0.001

    Tweaking this value can help make the controls stop wiggling so fast.
    If you have problems wasting too much RCS propellant because kOS
    "cares too much" about getting the rotation rate exactly right and is
    wiggling the controls unnecessarily when rotating toward a new direction,
    setting thie value a bit higher can help.

    You cannot set this value lower than
    :attr:`SteeringManager:TORQUEEPSILONMIN`.
    If you attempt to do so, then
    :attr:`SteeringManager:TORQUEEPSILONMIN` will be decreased to match
    the value just set :attr:`SteeringManager:TORQUEEPSILONMAX` to.

    **HOW IT WORKS:**
    
    If the error in the desired rotation rate is smaller than the current epsilon,
    then the PID that calculates desired torque will ignore that error and not
    bother correcting it until it gets bigger.  The actual epsilon value used
    in the steering manager's internal PID controller is always something between 
    :attr:`SteeringManager:TORQUEEPSILONMIN`.
    and
    :attr:`SteeringManager:TORQUEEPSILONMAX`.
    It varies between these two values depending on whether the
    vessel is currently rotating at near the maximum rotation rate
    the SteeringManager allows (as determined by
    :attr:`SteeringManager:MAXSTOPPINGTIME`) or whether it's quite far
    from its maximum rotation rate.
    :attr:`SteeringManager:TORQUEEPSILONMAX` is used when the vessel is
    at it's maximum rotation rate (i.e. it's coasting around to a new
    orientation and shouldn't pointlessly spend RCS fuel trying to hold
    that angular velocity precisely).
    :attr:`SteeringManager:TORQUEEPSILONMIN` is used when the vessel is
    not trying to rotate at all and is supposed to be using the steering
    just to hold the aim at a standstill.  In between these two states,
    it uses a value partway between the two, linearly interpolated between
    them.

    If you desire a constant epsilon, set both the min and max values to the
    same value.

.. _rotationepsilonmax_math:

    ** MIN VESSEL CAPABILITY: **

    Warning: Setting :attr:`SteeringManager:ROTATIONEPSILONMAX` too large can
    make the SteeringManager fail to try turning the craft at all.  Use this
    formula to decide what is probably the maximum safe value you can set
    it to without it causing this problem:

    Let :math:`\omega = \text{rotational acceleration the vessel is
    capable of, expressed in} \frac{\text{degrees}}{\text{second}^2}`

    Then :math:`\epsilon`, the maximum safe ``RotationEpsilonMax``
    to pick, is:

    :math:`\epsilon = \omega \cdot {MAXSTOPPINGTIME}`
    Where MAXSTOPPINGTIME is :attr:`SteeringManager:MAXSTOPPINGTIME`

.. attribute:: SteeringManager:MAXSTOPPINGTIME

    :type: :ref:`scalar <scalar>` (s)
    :access: Get/Set

    This value is used to limit the turning rate when :ref:`calculating target angular velocity <cooked_omega_pid>`.  The ship will not turn faster than what it can stop in this amount of time.  The maximum angular velocity about each axis is calculated as: (max angular velocity) = MAXSTOPPINGTIME * (available torque) / (moment of inertia).

    .. note::

        This setting affects all three of the :ref:`rotational velocity PID's <cooked_omega_pid>` at once (pitch, yaw, and roll), rather than affecting the three axes individually one at a time.

.. attribute:: SteeringManager:ROLLCONTROLANGLERANGE

    :type: :ref:`scalar <scalar>` (deg)
    :access: Get/Set

    The maximum value of :attr:`ANGLEERROR<SteeringManager:ANGLEERROR>` for
    which kOS will attempt to respond to error along the roll axis.  If this
    is set to 5 (the default value), the facing direction will need to be within
    5 degrees of the target direction before it actually attempts to roll the
    ship.  Setting the value to 180 will effectivelly allow roll control at any
    error amount.  When :attr:`ANGLEERROR<SteeringManager:ANGLEERROR>` is
    greater than this value, kOS will only attempt to kill all roll angular
    velocity.  The value is clamped between 180 and 1e-16.

.. attribute:: SteeringManager:ANGLEERROR

    :type: :ref:`scalar <scalar>` (deg)
    :access: Get only

    The angle between the ship's facing direction forward vector and the target direction's forward.  This is the combined pitch and yaw error.

.. attribute:: SteeringManager:PITCHERROR

    :type: :ref:`scalar <scalar>` (deg)
    :access: Get only

    The pitch angle between the ship's facing direction and the target direction.

.. attribute:: SteeringManager:YAWERROR

    :type: :ref:`scalar <scalar>` (deg)
    :access: Get only

    The yaw angle between the ship's facing direction and the target direction.

.. attribute:: SteeringManager:ROLLERROR

    :type: :ref:`scalar <scalar>` (deg)
    :access: Get only

    The roll angle between the ship's facing direction and the target direction.

.. attribute:: SteeringManager:PITCHTORQUEADJUST

    :type: :ref:`scalar <scalar>` (kNm)
    :access: Get/Set

    You can set this value to provide an additive bias to the calculated available pitch torque used in the pitch :ref:`torque PID <cooked_torque_pid>`. (available torque) = ((calculated torque) + PITCHTORQUEADJUST) * PITCHTORQUEFACTOR.

.. attribute:: SteeringManager:YAWTORQUEADJUST

    :type: :ref:`scalar <scalar>` (kNm)
    :access: Get/Set

    You can set this value to provide an additive bias to the calculated available yaw torque used in the yaw :ref:`torque PID <cooked_torque_pid>`. (available torque) = ((calculated torque) + YAWTORQUEADJUST) * YAWTORQUEFACTOR.

.. attribute:: SteeringManager:ROLLTORQUEADJUST

    :type: :ref:`scalar <scalar>` (kNm)
    :access: Get/Set

    You can set this value to provide an additive bias to the calculated available roll torque used in the roll :ref:`torque PID <cooked_torque_pid>`. (available torque) = ((calculated torque) + ROLLTORQUEADJUST) * ROLLTORQUEFACTOR.

.. attribute:: SteeringManager:PITCHTORQUEFACTOR

    :type: :ref:`scalar <scalar>` (kNm)
    :access: Get/Set

    You can set this value to provide an multiplicative factor bias to the calculated available pitch torque used in the :ref:`torque PID <cooked_torque_pid>`. (available torque) = ((calculated torque) + PITCHTORQUEADJUST) * PITCHTORQUEFACTOR.

.. attribute:: SteeringManager:YAWTORQUEFACTOR

    :type: :ref:`scalar <scalar>` (kNm)
    :access: Get/Set

    You can set this value to provide an multiplicative factor bias to the calculated available yaw torque used in the :ref:`torque PID <cooked_torque_pid>`. (available torque) = ((calculated torque) + YAWTORQUEADJUST) * YAWTORQUEFACTOR.

.. attribute:: SteeringManager:ROLLTORQUEFACTOR

    :type: :ref:`scalar <scalar>` (kNm)
    :access: Get/Set

    You can set this value to provide an multiplicative factor bias to the calculated available roll torque used in the :ref:`torque PID <cooked_torque_pid>`. (available torque) = ((calculated torque) + ROLLTORQUEADJUST) * ROLLTORQUEFACTOR.
