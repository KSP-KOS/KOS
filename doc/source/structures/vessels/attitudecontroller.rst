.. _attitudecontroller:

Attitude Controller
======

A ship usually has various attitude controllers like control surfaces, engines, RCS thrusters, rotors and drain valves. These controllers can be configured as follows:

    local controllers to ship:AttitudeControllers.
    print(controllers[0]:ControllerType + " allows pitch: " + controllers[0]:allowPitch).

.. structure:: AttitudeController

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 2

        * - Suffix
          - Type (units)
          - Description

        * - :attr:`PART`
          - :struct:`Part <Part>`
          - The part this controller belongs to.
        * - :attr:`MODULE`
          - :struct:`PartModule <PartModule>`
          - The module this controller belongs to. Will return false if there is no matching module.
        * - :attr:`ALLOWPITCH`
          - :ref:`Boolean <boolean>`
          - Gets or sets wheter this controller should respond to pitch input.
        * - :attr:`ALLOWYAW`
          - :ref:`Boolean <boolean>`
          - Gets or sets wheter this controller should respond to yaw input.
        * - :attr:`ALLOWROLL`
          - :ref:`Boolean <boolean>`
          - Gets or sets wheter this controller should respond to roll input.
        * - :attr:`ALLOWX`
          - :ref:`Boolean <boolean>`
          - Gets or sets wheter this controller should respond to x translation input.
        * - :attr:`ALLOWY`
          - :ref:`Boolean <boolean>`
          - Gets or sets wheter this controller should respond to y translation input.
        * - :attr:`ALLOWZ`
          - :ref:`Boolean <boolean>`
          - Gets or sets wheter this controller should respond to z translation input.
        * - :attr:`HASCUSTOMTHROTTLE`
          - :ref:`Boolean <boolean>`
          - Wheter this controller has a custom throttle input.
        * - :attr:`CUSTOMTHROTTLE`
          - :ref:`scalar <scalar>` (%)
          - The value the custom throttle.
        * - :attr:`ROTATIONAUTHRORITYLIMITER`
          - :ref:`scalar <scalar>` (%)
          - The authority limit for rotation.
        * - :attr:`TRANSLATIONAUTHRORITYLIMITER`
          - :ref:`scalar <scalar>` (%)
          - The authority limit for translation.
        * - :attr:`CONTROLLERTYPE`
          - :ref:`string <string>`
          - The type of the controller.
        * - :attr:`STATUS`
          - :ref:`string <string>`
          - A string indicating more detailed status about the controller if available.
        * - :attr:`RESPONSETIME`
          - :ref:`scalar <scalar>`
          - The reported responsetime of the controller.
        * - :attr:`POSITIVEROTATION`
          - :struct:`AttitudeCorrectionResult <AttitudeCorrectionResult>`
          - What is expected to happen when you provide a positive value to pitch, yaw, roll.
        * - :attr:`NEGATIVEROTATION`
          - :struct:`AttitudeCorrectionResult <AttitudeCorrectionResult>`
          - What is expected to happen when you provide a negative value to pitch, yaw, roll.
        * - :meth:`RESPONSEFOR`
          - :struct:`AttitudeCorrectionResult <AttitudeCorrectionResult>`
          - What is expected to happen for arbitrary combinations of pitch, yaw, roll, translate x, translate y, translate z, custom throttle.


.. note::

    The rotation responses are simplified models of reality and are likely to be off to various degrees.



.. _attitudecontroller_PART:

.. attribute:: AttitudeController:PART

    :access: Get
    :type: :struct:`Part <Part>`

    The part this controller belongs to.

.. _attitudecontroller_MODULE:

.. attribute:: AttitudeController:MODULE

    :access: Get only
    :type: :struct:`PartModule <PartModule>`

    The module this controller belongs to. Will return false if there is no matching module.

.. _attitudecontroller_ALLOWPITCH:

.. attribute:: AttitudeController:ALLOWPITCH

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Determines whether this controller is allowed to respond to pitch input.

.. _attitudecontroller_ALLOWYAW:

.. attribute:: AttitudeController:ALLOWYAW

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Determines whether this controller is allowed to respond to yaw input.

.. _attitudecontroller_ALLOWROLL:

.. attribute:: AttitudeController:ALLOWROLL

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Determines whether this controller is allowed to respond to roll input.

.. _attitudecontroller_ALLOWX:

.. attribute:: AttitudeController:ALLOWX

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Determines whether this controller is allowed to respond to translation fore input.

.. _attitudecontroller_ALLOWY:

.. attribute:: AttitudeController:ALLOWY

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Determines whether this controller is allowed to respond to translation top input.

.. _attitudecontroller_ALLOWZ:

.. attribute:: AttitudeController:ALLOWZ

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Determines whether this controller is allowed to respond to translation star input.

.. _attitudecontroller_HASCUSTOMTHROTTLE:

.. attribute:: AttitudeController:HASCUSTOMTHROTTLE`

    :access: Get only
    :type: :ref:`boolean <boolean>`

    Returns true if this controller has a custom throttle you can modify.

.. _attitudecontroller_CUSTOMTHROTTLE:

.. attribute:: AttitudeController:CUSTOMTHROTTLE

    :access: Get/Set
    :type: :ref:`scalar <scalar>` (%)

    Sets the custom throttle for this controller.

.. _attitudecontroller_ROTATIONAUTHORITYLIMITER:

.. attribute:: AttitudeController:ROTATIONAUTHORITYLIMITER

    :access: Get/Set
    :type: :ref:`scalar <scalar>` (%)

    Sets the authority limiter used during rotation.

.. _attitudecontroller_TRANSLATIONAUTHORITYLIMITER:

.. attribute:: AttitudeController:TRANSLATIONAUTHORITYLIMITER

    :access: Get/Set
    :type: :ref:`scalar <scalar>` (%)

    Sets the authority limiter used during translation.

.. _attitudecontroller_CONTROLLERTYPE:

.. attribute:: AttitudeController:CONTROLLERTYPE

    :access: Get only
    :type: :ref:`string <string>`

    The type of the attitude controller (ENGINE, DRAINVALVE, ROTOR, RCS, REACTIONWHEEL) or UNKNOWN if the exact type is unknown.

.. _attitudecontroller_STATUS:

.. attribute:: AttitudeController:STATUS

    :access: Get only
    :type: :ref:`string <string>`

    The status of the controller if known. UNKNOWN otherwise.
   
.. _attitudecontroller_RESPONSETIME:

.. attribute:: AttitudeController:RESPONSETIME

    :access: Get only
    :type: :ref:`scalar <scalar>`

    The reported response time of this controller.

.. _attitudecontroller_POSITIVEROTATION:

.. attribute:: AttitudeController:POSITIVEROTATION

    :access: Get only
    :type: :struct:`AttitudeCorrectionResult <AttitudeCorrectionResult>`

    What is expected to happen when you provide a positive value to pitch, yaw, roll.

.. _attitudecontroller_NEGATIVEROTATION:

.. attribute:: AttitudeController:NEGATIVEROTATION

    :access: Get only
    :type: :struct:`AttitudeCorrectionResult <AttitudeCorrectionResult>`

    What is expected to happen when you provide a negative value to pitch, yaw, roll.

.. _attitudecontroller_RESPONSEFOR:

.. method:: AttitudeController:RESPONSEFOR(pitchYawRollInput, translateXYZInput, throttle)

    :parameter pitchYawRollInput: A vector describing user pitch, yaw, roll input between -1 and 1.
	:parameter translateXYZInput: A vector describing user fore, top, star translation input between -1 and 1.
	:parameter throttle: A scalar representing the custom throttle value in percent.
    :type: :struct:`AttitudeCorrectionResult <AttitudeCorrectionResult>`

    Simulates the effect of the given input on the ship. This allows computing things like RCS thruster inbalances.
