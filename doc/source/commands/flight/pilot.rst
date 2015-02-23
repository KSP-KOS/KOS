.. _pilot:

Pilot Input
===========

This is not, strictly speaking, a method of controlling the craft. "Pilot" controls are a way to read the input from the pilot. Most of these controls share the same name as their flight control, prefixed with ``PILOT`` (eg ``YAW`` and ``PILOTYAW``)  the one exception to this is the ``PILOTMAINTHROTTLE``. This suffix has a setter and allows you to change the behavior of the throttle that persists even after the current program ends::

    SET SHIP:CONTROL:PILOTMAINTHROTTLE TO 0.

Will ensure that the throttle will be 0 when execution stops. These suffixes allow you to read the input given to the system by the user.

.. structure:: Control

.. list-table::
    :widths: 1 1 1
    :header-rows: 1

    * - Suffix
      - Type, Range
      - Equivalent Key

    * - :ref:`PILOTMAINTHROTTLE <SHIP CONTROL PILOTMAINTHROTTLE>`
      - scalar [0,1]
      - ``LEFT-CTRL``, ``LEFT-SHIFT``

    * - :ref:`PILOTYAW <SHIP CONTROL PILOTYAW>`
      - scalar [-1,1]
      - ``D``, ``A``
    * - :ref:`PILOTPITCH <SHIP CONTROL PILOTPITCH>`
      - scalar [-1,1]
      - ``W``, ``S``
    * - :ref:`PILOTROLL <SHIP CONTROL PILOTROLL>`
      - scalar [-1,1]
      - ``Q``, ``E``
    * - :ref:`PILOTROTATION <SHIP CONTROL PILOTROTATION>`
      - :struct:`Vector`
      - ``(YAW,PITCH,ROLL)``

    * - :ref:`PILOTYAWTRIM <SHIP CONTROL PILOTYAWTRIM>`
      - scalar [-1,1]
      - ``ALT+D``, ``ALT+A``
    * - :ref:`PILOTPITCHTRIM <SHIP CONTROL PILOTPITCHTRIM>`
      - scalar [-1,1]
      - ``ALT+W``, ``ALT+S``
    * - :ref:`PILOTROLLTRIM <SHIP CONTROL PILOTROLLTRIM>`
      - scalar [-1,1]
      - ``ALT+Q``, ``ALT+E``

    * - :ref:`PILOTFORE <SHIP CONTROL PILOTFORE>`
      - scalar [-1,1]
      - ``N``, ``H``
    * - :ref:`PILOTSTARBOARD <SHIP CONTROL PILOTSTARBOARD>`
      - scalar [-1,1]
      - ``L``, ``J``
    * - :ref:`PILOTTOP <SHIP CONTROL PILOTTOP>`
      - scalar [-1,1]
      - ``I``, ``K``
    * - :ref:`PILOTTRANSLATION <SHIP CONTROL PILOTTRANSLATION>`
      - :struct:`Vector`
      - ``(STARBOARD,TOP,FORE)``

    * - :ref:`PILOTWHEELSTEER <SHIP CONTROL PILOTWHEELSTEER>`
      - scalar [-1,1]
      - ``A``, ``D``
    * - :ref:`PILOTWHEELTHROTTLE <SHIP CONTROL PILOTWHEELTHROTTLE>`
      - scalar [-1,1]
      - ``W``, ``S``

    * - :ref:`PILOTWHEELSTEERTRIM <SHIP CONTROL PILOTWHEELSTEERTRIM>`
      - scalar [-1,1]
      - ``ALT+A``, ``ALT+D``
    * - :ref:`PILOTWHEELTHROTTLETRIM <SHIP CONTROL PILOTWHEELTHROTTLETRIM>`
      - scalar [-1,1]
      - ``ALT+W``, ``ALT+S``

    * - :ref:`PILOTNEUTRAL <SHIP CONTROL PILOTNEUTRAL>`
      - boolean
      - Is **kOS** Controlling?


.. _SHIP CONTROL PILOTMAINTHROTTLE:
.. object:: SHIP:CONTROL:MAINTHROTTLE

    Returns the pilot's input for the throttle. This is the only ``PILOT`` variable that is settable and is used to set the throttle upon termination of the current **kOS** program.

.. _SHIP CONTROL PILOTYAW:
.. object:: SHIP:CONTROL:YAW

    Returns the pilot's rotation input about the "up" vector as the pilot faces forward. Essentially left :math:`(-1)` or right :math:`(+1)`.

.. _SHIP CONTROL PILOTPITCH:
.. object:: SHIP:CONTROL:PITCH

    Returns the pilot's rotation input  about the starboard vector up :math:`(+1)` or down :math:`(-1)`.

.. _SHIP CONTROL PILOTROLL:
.. object:: SHIP:CONTROL:ROLL

    Returns the pilot's rotation input  about the logintudinal axis of the ship left-wing-down :math:`(-1)` or left-wing-up :math:`(+1)`.

.. _SHIP CONTROL PILOTROTATION:
.. object:: SHIP:CONTROL:ROTATION

    Returns the pilot's rotation input as a :struct:`Vector` object containing ``(YAW, PITCH, ROLL)`` in that order.



.. _SHIP CONTROL PILOTYAWTRIM:
.. object:: SHIP:CONTROL:YAWTRIM

    Returns the pilot's input for the ``YAW`` of the rotational trim.

.. _SHIP CONTROL PILOTPITCHTRIM:
.. object:: SHIP:CONTROL:PITCHTRIM

    Returns the pilot's input for the ``PITCH`` of the rotational trim.

.. _SHIP CONTROL PILOTROLLTRIM:
.. object:: SHIP:CONTROL:ROLLTRIM

    Returns the pilot's input for the ``ROLL`` of the rotational trim.




.. _SHIP CONTROL PILOTFORE:
.. object:: SHIP:CONTROL:FORE

    Returns the the pilot's input for the translation of the ship forward :math:`(+1)` or backward :math:`(-1)`.

.. _SHIP CONTROL PILOTSTARBOARD:
.. object:: SHIP:CONTROL:STARBOARD

    Returns the the pilot's input for the translation of the ship to the right :math:`(+1)` or left :math:`(-1)` from the pilot's perspective.

.. _SHIP CONTROL PILOTTOP:
.. object:: SHIP:CONTROL:TOP

    Returns the the pilot's input for the translation of the ship up :math:`(+1)` or down :math:`(-1)` from the pilot's perspective.

.. _SHIP CONTROL PILOTTRANSLATION:
.. object:: SHIP:CONTROL:TRANSLATION

    Returns the the pilot's input for translation as a :struct:`Vector` ``(STARBOARD, TOP, FORE)``.

.. _SHIP CONTROL PILOTWHEELSTEER:
.. object:: SHIP:CONTROL:WHEELSTEER

    Returns the the pilot's input for wheel steering left :math:`(-1)` or right :math:`(+1)`.

.. _SHIP CONTROL PILOTWHEELTHROTTLE:
.. object:: SHIP:CONTROL:WHEELTHROTTLE

    Returns the the pilot's input for the wheels to move the ship forward :math:`(+1)` or backward :math:`(-1)` while on the ground.

.. _SHIP CONTROL PILOTWHEELSTEERTRIM:
.. object:: SHIP:CONTROL:WHEELSTEERTRIM

    Returns the the pilot's input for the trim of the wheel steering.

.. _SHIP CONTROL PILOTWHEELTHROTTLETRIM:
.. object:: SHIP:CONTROL:WHEELTHROTTLETRIM

    Returns the the pilot's input for the trim of the wheel throttle.

.. _SHIP CONTROL PILOTNEUTRAL:
.. object:: SHIP:CONTROL:NEUTRAL

    Returns true or false if the pilot is active or not.

Be aware that **kOS** can't control a control at the same time that a player controls it. If **kOS** is taking control of the yoke, then the player can't manually control it. Remember to run::

    SET SHIP:CONTROL:NEUTRALIZE TO TRUE.

after the script is done using the controls, or the player will be locked out of control.



