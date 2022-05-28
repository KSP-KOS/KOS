.. _raw:

Raw Control
===========

If you wish to have your kOS script manipulate a vessel's flight controls directly in a raw way, rather than relying on kOS to handle the flying for you, then this is the type of structure you will need to use to do it. This is offered as an alternative to using the combination of ``LOCK STEERING`` and ``LOCK THROTTLE`` commands. To obtain the CONTROL variable for a vessel, use its :CONTROL suffix::

    SET controlStick to SHIP:CONTROL.
    SET controlStick:PITCH to 0.2.

Unlike with so-called "Cooked" steering, "raw" steering uses the ``SET`` command, not the ``LOCK`` command. Using ``LOCK`` with these controls won't work. When controlling the ship in a raw way, you must decide how to move the controls in detail. Here is another example::

    SET SHIP:CONTROL:YAW to 0.2.

This will start pushing the ship to rotate a bit faster to the right, like pushing the ``D`` key gently. All the following values are set between :math:`-1` and :math:`+1`. Zero means the control is neutral. You can set to values smaller in magnitude than :math:`-1` and :math:`+1` for gentler control, (but be aware of the 5% :ref:`null zone mentioned below<raw null zone>`)::

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

CONFIG:SUPPRESSAUTOPILOT
------------------------

If :attr:`Config:SUPPRESSAUTOPILOT` is true, then none of the controls
on this page will have an effect.  That setting is there to provide
the player with an emergency way to quickly click a toggle on the
toolbar window to force kOS to stop taking control, letting the player
move the controls manually.

Breaking Ground DLC
-------------------

Please note that the *Breaking Ground DLC* parts that can be configured
to respond to the control axes, such as electric motors and propellor
pitches, will only respond to the settings described on the 
:ref:`pilot controls page <pilot>` and NOT the settings described
here on this :ref:`raw control <raw>` page, nor will it respond to
the settings in ``lock throttle`` or ``lock steering``.  SQUAD designed
the *Breaking Ground* DLC parts to only pay attention to the player's
controls, not autopilot controls.

.. _raw null zone:

5% null zone
------------

.. warning::

    KSP imposes a built-in 5% null zone on RCS thrusters that makes it
    impossible for small raw inputs to have any effect in situations
    where RCS thrusters are the only source of control.  However,
    kOS allows you to override KSP's stock RCS null zone for RCS parts
    with a bit of trickery under the hood, using the :attr:`RCS:DEADZONE`
    suffix of RCS parts.

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
      - :ref:`scalar <scalar>` [0,1]
      - ``LEFT-CTRL``, ``LEFT-SHIFT``

    * - :ref:`YAW <SHIP CONTROL YAW>`
      - :ref:`scalar <scalar>` [-1,1]
      - ``D``, ``A``
    * - :ref:`PITCH <SHIP CONTROL PITCH>`
      - :ref:`scalar <scalar>` [-1,1]
      - ``W``, ``S``
    * - :ref:`ROLL <SHIP CONTROL ROLL>`
      - :ref:`scalar <scalar>` [-1,1]
      - ``Q``, ``E``
    * - :ref:`ROTATION <SHIP CONTROL ROTATION>`
      - :struct:`Vector`
      - ``(YAW,PITCH,ROLL)``

    * - :ref:`YAWTRIM <SHIP CONTROL YAWTRIM>`
      - :ref:`scalar <scalar>` [-1,1]
      - (No real effect, see below) ``ALT+D``, ``ALT+A``
    * - :ref:`PITCHTRIM <SHIP CONTROL PITCHTRIM>`
      - :ref:`scalar <scalar>` [-1,1]
      - (No real effect, see below) ``ALT+W``, ``ALT+S``
    * - :ref:`ROLLTRIM <SHIP CONTROL ROLLTRIM>`
      - :ref:`scalar <scalar>` [-1,1]
      - (No real effect, see below) ``ALT+Q``, ``ALT+E``

    * - :ref:`FORE <SHIP CONTROL FORE>`
      - :ref:`scalar <scalar>` [-1,1]
      - ``N``, ``H``
    * - :ref:`STARBOARD <SHIP CONTROL STARBOARD>`
      - :ref:`scalar <scalar>` [-1,1]
      - ``L``, ``J``
    * - :ref:`TOP <SHIP CONTROL TOP>`
      - :ref:`scalar <scalar>` [-1,1]
      - ``I``, ``K``
    * - :ref:`TRANSLATION <SHIP CONTROL TRANSLATION>`
      - :struct:`Vector`
      - ``(STARBOARD,TOP,FORE)``

    * - :ref:`WHEELSTEER <SHIP CONTROL WHEELSTEER>`
      - :ref:`scalar <scalar>` [-1,1]
      - ``A``, ``D``
    * - :ref:`WHEELTHROTTLE <SHIP CONTROL WHEELTHROTTLE>`
      - :ref:`scalar <scalar>` [-1,1]
      - ``W``, ``S``

    * - :ref:`WHEELSTEERTRIM <SHIP CONTROL WHEELSTEERTRIM>`
      - :ref:`scalar <scalar>` [-1,1]
      - (No real effect, see below) ``ALT+A``, ``ALT+D``
    * - :ref:`WHEELTHROTTLETRIM <SHIP CONTROL WHEELTHROTTLETRIM>`
      - :ref:`scalar <scalar>` [-1,1]
      - (No real effect, see below) ``ALT+W``, ``ALT+S``

    * - :ref:`NEUTRAL <SHIP CONTROL NEUTRAL>`
      - :ref:`Boolean <boolean>`
      - True if ship:control is doing nothing.

    * - :ref:`NEUTRALIZE <SHIP CONTROL NEUTRALIZE>`
      - :ref:`Boolean <boolean>`
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

    *This has no real effect and is just here for completeness.*

    IF you *really* want to control TRIM, use ``SHIP:CONTROL:PILOTYAWTRIM``
    from the suffixes in the :ref:`Pilot control section <pilot>` instead.

    The reason why this trim does nothing and you have to use the pilot
    trim instead is because KSP only looks at the trim when its part of
    the *pilot's* own control structure, not an autpilot's control structure.

    *Warning*:
    Setting this value can cause :ref:`:NEUTRAL <SHIP CONTROL NEUTRAL>` to
    return false negatives by confusing the system about where the "at
    rest" point of the controls are.

.. _SHIP CONTROL PITCHTRIM:
.. object:: SHIP:CONTROL:PITCHTRIM

    *This has no real effect and is just here for completeness.*

    IF you *really* want to control TRIM, use ``SHIP:CONTROL:PILOTPITCHTRIM``
    from the suffixes in the :ref:`Pilot control section <pilot>` instead.

    The reason why this trim does nothing and you have to use the pilot
    trim instead is because KSP only looks at the trim when its part of
    the *pilot's* own control structure, not an autpilot's control structure.

    *Warning*:
    Setting this value can cause :ref:`NEUTRAL <SHIP CONTROL NEUTRAL>` to
    return false negatives by confusing the system about where the "at
    rest" point of the controls are.

.. _SHIP CONTROL ROLLTRIM:
.. object:: SHIP:CONTROL:ROLLTRIM

    *This has no real effect and is just here for completeness.*

    IF you *really* want to control TRIM, use ``SHIP:CONTROL:PILOTROLLTRIM``
    from the suffixes in the :ref:`Pilot control section <pilot>` instead.

    The reason why this trim does nothing here is because KSP only looks at the
    trim when its part of the *pilot's* own control structure, not an
    autpilot's control structure.

    *Warning*:
    Setting this value can cause :ref:`NEUTRAL <SHIP CONTROL NEUTRAL>` to
    return false negatives by confusing the system about where the "at
    rest" point of the controls are.

.. _SHIP CONTROL FORE:
.. object:: SHIP:CONTROL:FORE

    Controls the translation of the ship forward :math:`(+1)` or backward :math:`(-1)`.
    Note that this control has a :ref:`game-enforced 5% null zone <raw null zone>` that
    kOS doesn't seem to be able to change.

.. _SHIP CONTROL STARBOARD:
.. object:: SHIP:CONTROL:STARBOARD

    Controls the translation of the ship to the right :math:`(+1)` or left :math:`(-1)` from the pilot's perspective.
    Note that this control has a :ref:`game-enforced 5% null zone <raw null zone>` that
    kOS doesn't seem to be able to change.

.. _SHIP CONTROL TOP:
.. object:: SHIP:CONTROL:TOP

    Controls the translation of the ship up :math:`(+1)` or down :math:`(-1)` from the pilot's perspective.
    Note that this control has a :ref:`game-enforced 5% null zone <raw null zone>` that
    kOS doesn't seem to be able to change.

.. _SHIP CONTROL TRANSLATION:
.. object:: SHIP:CONTROL:TRANSLATION

    Controls the translation as a :struct:`Vector` ``(STARBOARD, TOP, FORE)``.
    Note that each axis of this this control vector has a
    :ref:`game-enforced 5% null zone <raw null zone>` that kOS doesn't seem to be
    able to change.

.. _SHIP CONTROL WHEELSTEER:
.. object:: SHIP:CONTROL:WHEELSTEER

    Turns the wheels left :math:`(-1)` or right :math:`(+1)`.

.. _SHIP CONTROL WHEELTHROTTLE:
.. object:: SHIP:CONTROL:WHEELTHROTTLE

    Controls the wheels to move the ship forward :math:`(+1)` or backward :math:`(-1)` while on the ground.

.. _SHIP CONTROL WHEELSTEERTRIM:
.. object:: SHIP:CONTROL:WHEELSTEERTRIM

    *This has no real effect and is just here for completeness.*

    IF you *really* want to control TRIM, use ``SHIP:CONTROL:PILOTYAWTRIM``
    from the suffixes in the :ref:`Pilot control section <pilot>` instead.

    The reason why this trim does nothing here is because KSP only looks at the
    trim when its part of the *pilot's* own control structure, not an
    autpilot's control structure.

    *Warning*:
    Setting this value can cause :ref:`NEUTRAL <SHIP CONTROL NEUTRAL>` to
    return false negatives by confusing the system about where the "at
    rest" point of the controls are.

.. _SHIP CONTROL WHEELTHROTTLETRIM:
.. object:: SHIP:CONTROL:WHEELTHROTTLETRIM

    *This has no real effect and is just here for completeness.*

    IF you *really* want to control TRIM, use ``SHIP:CONTROL:PILOTYAWTRIM``
    from the suffixes in the :ref:`Pilot control section <pilot>` instead.

    The reason why this trim does nothing here is because KSP only looks at the
    trim when its part of the *pilot's* own control structure, not an
    autpilot's control structure.

    *Warning*:
    Setting this value can cause :ref:`NEUTRAL <SHIP CONTROL NEUTRAL>` to
    return false negatives by confusing the system about where the "at
    rest" point of the controls are.

.. _SHIP CONTROL NEUTRAL:
.. _SHIP CONTROL NEUTRALIZE:
.. object:: SHIP:CONTROL:NEUTRAL
.. object:: SHIP:CONTROL:NEUTRALIZE

    These used to be two suffixes but they are now synonyms who's meaning
    changes depending on if you set or get them.

    *Getting*:

    ``if (SHIP:CONTROL:NEUTRAL)`` is true when the raw controls are at rest.

    *Setting*:

    ``set SHIP:CONTROL:NEUTRALIZE TO TRUE.`` causes the raw controls to let go.
    Setting it to false has no effect.

    *Warnings*:

    Although it has no effect, setting a raw control TRIM value CAN cause
    ``NEUTRAL`` to return false when the control is at rest.  For example,
    if you do ``SET SHIP:CONTROL:YAWTRIM to 0.1.` then when the controls
    are at rest, ``SHIP:CONTROL:NEUTRAL`` will return false because the yaw
    position of 0 is differing from its trim position of 0.1.

    The two terms ``NEUTRAL`` and ``NEUTRALIZE`` are synonyms.  (They used to
    be two separate suffixes, one for getting and one for setting, but
    that made no sense so they were combined but both spellings were
    retained for backward compantiblity with old scripts.)


Unlocking controls
------------------

Setting any one of ``SHIP:CONTROL`` values will prevent player from manipulating that specific control manually. Other controls will not be locked.
To free any single control, set it back to zero. To give all controls back to the player you must execute::

    SET SHIP:CONTROL:NEUTRALIZE to TRUE.


Advantages/Disadvantages
------------------------

The control over *RCS* translation requires the use of Raw control. Also, with raw control you can choose how gentle to be with the controls and it can be possible to control wobbly craft better with raw control than with cooked control.





