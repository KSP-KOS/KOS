.. _pilot:

Pilot Input
===========

These are suffixes of ``ship:control``.

They are similar to the suffixes on the :ref:`Raw control<raw>` page, except
that these suffixes refer to the player's actual input controls, rather
than kOS's own controls that are layered on top of them, overriding them.

Please note that the *Breaking Ground DLC* parts that can be configured
to respond to the throttle, such as electric motors and propellor pitches
will only respond to the settings described here on this page, and NOT
the settings described in the :ref:`raw control <raw>` page, or the
settings in ``lock throttle`` or ``lock steering``.  SQUAD designed
these to only pay attention to the player's controls, not autopilot
controls.

Most are read-only
------------------

**Most** of these controls are get-only because trying to change them has
no effect anyway (If kOS did let you set them, KSP would just reset them
back to match the player's actual input axis device immediately anyway.)
The controls can be read to find out what the player is *trying* to do, if
you'd like to write a script that tries to respond to that information.

But some are set-able
---------------------

There are exceptions - **some** controls here can be set.  The only
controls that might be settable in here are the ones that are for
settings that "stay put" and don't reset to center when the pilot lets
go of the stick.  For example, while ``PILOTPITCH`` matches whatever
temporary position the pilot's stick is in, ``PILOTPITCHTRIM`` stays
where it was last set.  Thus ``PILOTPITCHTRIM`` is settable here,
while ``PILOTPITCH`` is not.  The docmentation below marks which
suffixes are indeed set-able.  Through the use of these trim-able
settings, it is possible to control the ship in a way that still
lets the pilot temporarily grab the controls for a second to override
what kOS is doing.

.. structure:: Control

.. list-table::
    :widths: 1 1 1 1
    :header-rows: 1

    * - Suffix
      - Get/Set
      - Type, Range
      - Equivalent Key

    * - :ref:`PILOTMAINTHROTTLE <SHIP CONTROL PILOTMAINTHROTTLE>`
      - Get and **Set**
      - :ref:`scalar <scalar>` [0,1]
      - ``LEFT-CTRL``, ``LEFT-SHIFT``

    * - :ref:`PILOTYAW <SHIP CONTROL PILOTYAW>`
      - Get-only
      - :ref:`scalar <scalar>` [-1,1]
      - ``D``, ``A``
    * - :ref:`PILOTPITCH <SHIP CONTROL PILOTPITCH>`
      - Get-only
      - :ref:`scalar <scalar>` [-1,1]
      - ``W``, ``S``
    * - :ref:`PILOTROLL <SHIP CONTROL PILOTROLL>`
      - Get-only
      - :ref:`scalar <scalar>` [-1,1]
      - ``Q``, ``E``
    * - :ref:`PILOTROTATION <SHIP CONTROL PILOTROTATION>`
      - Get-only
      - :struct:`Vector`
      - ``(YAW,PITCH,ROLL)``

    * - :ref:`PILOTYAWTRIM <SHIP CONTROL PILOTYAWTRIM>`
      - Get and **Set**
      - :ref:`scalar <scalar>` [-1,1]
      - ``ALT+D``, ``ALT+A``
    * - :ref:`PILOTPITCHTRIM <SHIP CONTROL PILOTPITCHTRIM>`
      - Get and **Set**
      - :ref:`scalar <scalar>` [-1,1]
      - ``ALT+W``, ``ALT+S``
    * - :ref:`PILOTROLLTRIM <SHIP CONTROL PILOTROLLTRIM>`
      - Get and **Set**
      - :ref:`scalar <scalar>` [-1,1]
      - ``ALT+Q``, ``ALT+E``

    * - :ref:`PILOTFORE <SHIP CONTROL PILOTFORE>`
      - Get-only
      - :ref:`scalar <scalar>` [-1,1]
      - ``N``, ``H``
    * - :ref:`PILOTSTARBOARD <SHIP CONTROL PILOTSTARBOARD>`
      - Get-only
      - :ref:`scalar <scalar>` [-1,1]
      - ``L``, ``J``
    * - :ref:`PILOTTOP <SHIP CONTROL PILOTTOP>`
      - Get-only
      - :ref:`scalar <scalar>` [-1,1]
      - ``I``, ``K``
    * - :ref:`PILOTTRANSLATION <SHIP CONTROL PILOTTRANSLATION>`
      - Get-only
      - :struct:`Vector`
      - ``(STARBOARD,TOP,FORE)``

    * - :ref:`PILOTWHEELSTEER <SHIP CONTROL PILOTWHEELSTEER>`
      - Get-only
      - :ref:`scalar <scalar>` [-1,1]
      - ``A``, ``D``
    * - :ref:`PILOTWHEELTHROTTLE <SHIP CONTROL PILOTWHEELTHROTTLE>`
      - Get and **Set**
      - :ref:`scalar <scalar>` [-1,1]
      - ``W``, ``S``

    * - :ref:`PILOTWHEELSTEERTRIM <SHIP CONTROL PILOTWHEELSTEERTRIM>`
      - Get and **Set**
      - :ref:`scalar <scalar>` [-1,1]
      - ``ALT+A``, ``ALT+D``
    * - :ref:`PILOTWHEELTHROTTLETRIM <SHIP CONTROL PILOTWHEELTHROTTLETRIM>`
      - Get and **Set**
      - :ref:`scalar <scalar>` [-1,1]
      - ``ALT+W``, ``ALT+S``

    * - :ref:`PILOTNEUTRAL <SHIP CONTROL PILOTNEUTRAL>`
      - Get-only
      - :ref:`Boolean <boolean>`
      - Are the pilot's controls zeroed, including trim?


.. _SHIP CONTROL PILOTMAINTHROTTLE:
.. object:: SHIP:CONTROL:PILOTMAINTHROTTLE

    Get and **Set**

    Returns the pilot's input for the throttle.  If this is set, and a
    ``lock throttle`` is in effect, the ``lock throttle`` will override
    this, BUT it still affects where the throttle returns to when kOS
    lets go of the controls.

    RP-1 Special Case:  If using the RP-1 mod, and flying a "sounding rocket"
    where the avionics controls are insuficcient to steer but are good
    enough to ignite engines, then ``lock throttle`` does not work to
    activate those engines, but this suffix can do it.  RP-1 actively
    suppresses the normal ability for an autopilot to control the throttle
    in this case and only pays attention to the pilot's own manual control.

.. _SHIP CONTROL PILOTYAW:
.. object:: SHIP:CONTROL:PILOTYAW

    Get-only.

    Returns the pilot's rotation input about the "up" vector as the pilot faces forward. Essentially left :math:`(-1)` or right :math:`(+1)`.

.. _SHIP CONTROL PILOTPITCH:
.. object:: SHIP:CONTROL:PILOTPITCH

    Get-only.

    Returns the pilot's rotation input  about the starboard vector up :math:`(+1)` or down :math:`(-1)`.

.. _SHIP CONTROL PILOTROLL:
.. object:: SHIP:CONTROL:PILOTROLL

    Get-only.

    Returns the pilot's rotation input  about the logintudinal axis of the ship left-wing-down :math:`(-1)` or left-wing-up :math:`(+1)`.

.. _SHIP CONTROL PILOTROTATION:
.. object:: SHIP:CONTROL:PILOTROTATION

    Get-only.

    Returns the pilot's rotation input as a :struct:`Vector` object containing ``(YAW, PITCH, ROLL)`` in that order.


.. _SHIP CONTROL PILOTYAWTRIM:
.. object:: SHIP:CONTROL:PILOTYAWTRIM

    Get and **Set**

    The pilot's input for the ``YAW`` of the rotational trim.
    Note that this CAN be set, unlike ``PILOTYAW``, making it
    possible to use it for an autopilot control program.
    The player can also adjust it too, though, overriding
    what you set it to.

.. _SHIP CONTROL PILOTPITCHTRIM:
.. object:: SHIP:CONTROL:PILOTPITCHTRIM

    Get and **Set**

    The pilot's input for the ``PITCH`` of the rotational trim.
    Note that this CAN be set, unlike ``PILOTPITCH``, making it
    possible to use it for an autopilot control program.
    The player can also adjust it too, though, overriding
    what you set it to.

.. _SHIP CONTROL PILOTROLLTRIM:
.. object:: SHIP:CONTROL:PILOTROLLTRIM

    Get and **Set**

    The pilot's input for the ``ROLL`` of the rotational trim.
    Note that this CAN be set, unlike ``PILOTROLL``, making it
    possible to use it for an autopilot control program.
    The player can also adjust it too, though, overriding
    what you set it to.

.. _SHIP CONTROL PILOTFORE:
.. object:: SHIP:CONTROL:PILOTFORE

    Get-only.

    Returns the the pilot's input for the translation of the ship forward :math:`(+1)` or backward :math:`(-1)`.

.. _SHIP CONTROL PILOTSTARBOARD:
.. object:: SHIP:CONTROL:PILOTSTARBOARD

    Get-only.

    Returns the the pilot's input for the translation of the ship to the right :math:`(+1)` or left :math:`(-1)` from the pilot's perspective.

.. _SHIP CONTROL PILOTTOP:
.. object:: SHIP:CONTROL:PILOTTOP

    Get-only.

    Returns the the pilot's input for the translation of the ship up :math:`(+1)` or down :math:`(-1)` from the pilot's perspective.

.. _SHIP CONTROL PILOTTRANSLATION:
.. object:: SHIP:CONTROL:PILOTTRANSLATION

    Get-only.

    Returns the the pilot's input for translation as a :struct:`Vector` ``(STARBOARD, TOP, FORE)``.

.. _SHIP CONTROL PILOTWHEELSTEER:
.. object:: SHIP:CONTROL:PILOTWHEELSTEER

    Get-only.

    Returns the the pilot's input for wheel steering left :math:`(-1)` or right :math:`(+1)`.

.. _SHIP CONTROL PILOTWHEELTHROTTLE:
.. object:: SHIP:CONTROL:PILOTWHEELTHROTTLE

    Get and **Set**

    The the pilot's input for the wheels to move the ship forward :math:`(+1)` or backward :math:`(-1)` while on the ground.

    Because this is not an axis that resets, it can be set by a script although
    it may get suppressed when a ``lock throttle`` is in effect.

.. _SHIP CONTROL PILOTWHEELSTEERTRIM:
.. object:: SHIP:CONTROL:PILOTWHEELSTEERTRIM

    Get and **Set**

    Returns the the pilot's input for the trim of the wheel steering.

    Because this is a trim, it can be set by a kOS script.

.. _SHIP CONTROL PILOTWHEELTHROTTLETRIM:
.. object:: SHIP:CONTROL:PILOTWHEELTHROTTLETRIM

    Get and **Set**

    Returns the the pilot's input for the trim of the wheel throttle.

    Because this is a trim, it can be set by a kOS script.

.. _SHIP CONTROL PILOTNEUTRAL:
.. object:: SHIP:CONTROL:PILOTNEUTRAL

    Get-only.

    Returns true or false if the pilot is active or not.


Be aware that **kOS** can't control a control at the same time that a player controls it. If **kOS** is taking control of the yoke, then the player can't manually control it. Remember to run::

    SET SHIP:CONTROL:NEUTRALIZE TO TRUE.

after the script is done using the controls, or the player will be locked out of control.



