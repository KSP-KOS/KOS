.. _raw:

Raw Control
===========

If you wish to have your kOS script manipulate a vessel's flight controls directly in a raw way, rather than relying on kOS to handle the flying for you, then this is the type of structure you will need to use to do it. This is offered as an alternative to using the combination of ``LOCK STEERING`` and ``LOCK THROTTLE`` commands. To obtain the CONTROL variable for a vessel, use its :CONTROL suffix::

    SET controlStick to SHIP:CONTROL.
    SET controlStick:PITCH to 0.2.

Unlike with so-called "Cooked" steering, "raw" steering uses the ``SET`` command, not the ``LOCK`` command. Using ``LOCK`` with these controls won't work. When controlling the ship in a raw way, you must decide how to move the controls in detail. Here is another example::

    SET SHIP:CONTROL:YAW to 0.2.

This will start pushing the ship to rotate a bit faster to the right, like pushing the ``D`` key gently. All the following values are set between :math:`-1` and :math:`+1`. Zero means the control is neutral. You can set to values smaller in magnitude than :math:`-1` and :math:`+1` for gentler control::

    print "Gently pushing forward for 3 seconds.".
    SET SHIP:CONTROL:FORE TO 0.2.
    SET now to time:seconds.
    WAIT until time:seconds > now + 3.
    SET SHIP:CONTROL:FORE to 0.0.

    print "Gently Pushing leftward for 3 seconds.".
    SET SHIP:CONTROL:STARBOARD TO -0.2.
    SET now to time:seconds.
    WAIT until time:seconds > now + 3.
    SET SHIP:CONTROL:STARBOARD to 0.0.

    print "Starting an upward rotation.".
    SET SHIP:CONTROL:PITCH TO 0.2.
    SET now to time:seconds.
    WAIT until time:seconds > now + 0.5.
    SET SHIP:CONTROL:PITCH to 0.0.

    print "Giving control back to the player now.".
    SET SHIP:CONTROL:NEUTRALIZE to True.

One can use :ref:`SHIP:CONTROL:ROTATION <SHIP CONTROL ROTATION>` and :ref:`SHIP:CONTROL:TRANSLATION <SHIP CONTROL TRANSLATION>` to see the ship's current situation.

Raw Flight Controls Reference
-----------------------------

These "Raw" controls allow you the direct control of flight parameters while the current program is running.

.. note::
    The ``MAINTHROTTLE`` requires active engines and, of course,
    sufficient and appropriate fuel. The rotational controls ``YAW``,
    ``PITCH`` and ``ROW`` require one of the following: active reaction
    wheels with sufficient energy, *RCS* to be ON with properly placed
    thrusters and appropriate fuel, or control surfaces with an atmosphere
    in which to operate. The translational controls ``FORE``, ``STARBOARD``
    and ``TOP`` only work with *RCS*, and require RCS to be ON with
    properly placed thrusters and appropriate fuel.


.. list-table::
    :widths: 1 1 1
    :header-rows: 1

    * - Suffix
      - Type, Range
      - Equivalent Key

    * - :ref:`MAINTHROTTLE <SHIP CONTROL MAINTHROTTLE>`
      - scalar [0,1]
      - ``LEFT-CTRL``, ``LEFT-SHIFT``

    * - :ref:`YAW <SHIP CONTROL YAW>`
      - scalar [-1,1]
      - ``D``, ``A``
    * - :ref:`PITCH <SHIP CONTROL PITCH>`
      - scalar [-1,1]
      - ``W``, ``S``
    * - :ref:`ROLL <SHIP CONTROL ROLL>`
      - scalar [-1,1]
      - ``Q``, ``E``
    * - :ref:`ROTATION <SHIP CONTROL ROTATION>`
      - :struct:`Vector`
      - ``(YAW,PITCH,ROLL)``

    * - :ref:`YAWTRIM <SHIP CONTROL YAWTRIM>`
      - scalar [-1,1]
      - ``ALT+D``, ``ALT+A``
    * - :ref:`PITCHTRIM <SHIP CONTROL PITCHTRIM>`
      - scalar [-1,1]
      - ``ALT+W``, ``ALT+S``
    * - :ref:`ROLLTRIM <SHIP CONTROL ROLLTRIM>`
      - scalar [-1,1]
      - ``ALT+Q``, ``ALT+E``

    * - :ref:`FORE <SHIP CONTROL FORE>`
      - scalar [-1,1]
      - ``N``, ``H``
    * - :ref:`STARBOARD <SHIP CONTROL STARBOARD>`
      - scalar [-1,1]
      - ``L``, ``J``
    * - :ref:`TOP <SHIP CONTROL TOP>`
      - scalar [-1,1]
      - ``I``, ``K``
    * - :ref:`TRANSLATION <SHIP CONTROL TRANSLATION>`
      - :struct:`Vector`
      - ``(STARBOARD,TOP,FORE)``

    * - :ref:`WHEELSTEER <SHIP CONTROL WHEELSTEER>`
      - scalar [-1,1]
      - ``A``, ``D``
    * - :ref:`WHEELTHROTTLE <SHIP CONTROL WHEELTHROTTLE>`
      - scalar [-1,1]
      - ``W``, ``S``

    * - :ref:`WHEELSTEERTRIM <SHIP CONTROL WHEELSTEERTRIM>`
      - scalar [-1,1]
      - ``ALT+A``, ``ALT+D``
    * - :ref:`WHEELTHROTTLETRIM <SHIP CONTROL WHEELTHROTTLETRIM>`
      - scalar [-1,1]
      - ``ALT+W``, ``ALT+S``

    * - :ref:`NEUTRAL <SHIP CONTROL NEUTRAL>`
      - boolean
      - Is **kOS** Controlling?
    * - :ref:`NEUTRALIZE <SHIP CONTROL NEUTRALIZE>`
      - boolean
      - Releases Control




.. _SHIP CONTROL MAINTHROTTLE:
.. object:: SHIP:CONTROL:MAINTHROTTLE

    Set between 0 and 1 much like the cooked flying ``LOCK THROTTLE`` command.

.. _SHIP CONTROL YAW:
.. object:: SHIP:CONTROL:YAW

    This is the rotation about the "up" vector as the pilot faces forward. Essentially left :math:`(-1)` or right :math:`(+1)`.

.. _SHIP CONTROL PITCH:
.. object:: SHIP:CONTROL:PITCH

    Rotation about the starboard vector up :math:`(+1)` or down :math:`(-1)`.

.. _SHIP CONTROL ROLL:
.. object:: SHIP:CONTROL:ROLL

    Rotation about the longitudinal axis of the ship left-wing-down :math:`(-1)` or left-wing-up :math:`(+1)`.

.. _SHIP CONTROL ROTATION:
.. object:: SHIP:CONTROL:ROTATION

    This is a :struct:`Vector` object containing ``(YAW, PITCH, ROLL)`` in that order.



.. _SHIP CONTROL YAWTRIM:
.. object:: SHIP:CONTROL:YAWTRIM

    Controls the ``YAW`` of the rotational trim.

.. _SHIP CONTROL PITCHTRIM:
.. object:: SHIP:CONTROL:PITCHTRIM

    Controls the ``PITCH`` of the rotational trim.

.. _SHIP CONTROL ROLLTRIM:
.. object:: SHIP:CONTROL:ROLLTRIM

    Controls the ``ROLL`` of the rotational trim.




.. _SHIP CONTROL FORE:
.. object:: SHIP:CONTROL:FORE

    Controls the translation of the ship forward :math:`(+1)` or backward :math:`(-1)`.

.. _SHIP CONTROL STARBOARD:
.. object:: SHIP:CONTROL:STARBOARD

    Controls the translation of the ship to the right :math:`(+1)` or left :math:`(-1)` from the pilot's perspective.

.. _SHIP CONTROL TOP:
.. object:: SHIP:CONTROL:TOP

    Controls the translation of the ship up :math:`(+1)` or down :math:`(-1)` from the pilot's perspective.

.. _SHIP CONTROL TRANSLATION:
.. object:: SHIP:CONTROL:TRANSLATION

    Controls the translation as a :struct:`Vector` ``(STARBOARD, TOP, FORE)``.

.. _SHIP CONTROL WHEELSTEER:
.. object:: SHIP:CONTROL:WHEELSTEER

    Turns the wheels left :math:`(-1)` or right :math:`(+1)`.

.. _SHIP CONTROL WHEELTHROTTLE:
.. object:: SHIP:CONTROL:WHEELTHROTTLE

    Controls the wheels to move the ship forward :math:`(+1)` or backward :math:`(-1)` while on the ground.

.. _SHIP CONTROL WHEELSTEERTRIM:
.. object:: SHIP:CONTROL:WHEELSTEERTRIM

    Controls the trim of the wheel steering.

.. _SHIP CONTROL WHEELTHROTTLETRIM:
.. object:: SHIP:CONTROL:WHEELTHROTTLETRIM

    Controls the trim of the wheel throttle.

.. _SHIP CONTROL NEUTRAL:
.. object:: SHIP:CONTROL:NEUTRAL

    Returns true or false depending if **kOS** has any set controls. *This is not settable.*

.. _SHIP CONTROL NEUTRALIZE:
.. object:: SHIP:CONTROL:NEUTRALIZE

    This causes manual control to let go. When set to true, **kOS** lets go of the controls and allows the player to manually control them again. *This is not gettable.*


Unlocking controls
------------------

Setting any one of ``SHIP:CONTROL`` values will prevent player from manipulating that specific control manually. Other controls will not be locked.
To free any single control, set it back to zero. To give all controls back to the player you must execute::

    SET SHIP:CONTROL:NEUTRALIZE to TRUE.


Advantages/Disadvantages
------------------------

The control over *RCS* translation requires the use of Raw control. Also, with raw control you can choose how gentle to be with the controls and it can be possible to control wobbly craft better with raw control than with cooked control.





