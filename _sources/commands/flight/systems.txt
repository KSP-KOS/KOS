Ship Systems
============

.. _CONTROLFROM:
.. object:: SET somepart:CONTROLFROM TO (true|false).

    If you have a handle on a part, from ``LIST PARTS``, you can select that part to set the orientation of the craft, just like using the "control from here" in the right-click menu in the game. For more information see :attr:`Part:CONTROLFROM`.

.. global:: RCS

    :access: Toggle ON/OFF

    Turns the RCS **on** or **off**, like using ``R`` at the keyboard::

        RCS ON.

.. global:: SAS

    :access: Toggle ON/OFF

    Turns the SAS **on** or **off**, like using ``T`` at the keybaord::

        SAS ON.

.. global:: LIGHTS

    :access: Toggle ON/OFF

    Turns the lights **on** or **off**, like using the ``U`` key at the keyboard::

        LIGHTS ON.

.. global:: BRAKES

    :access: Toggle ON/OFF

    Turns the brakes **on** or **off**, like clicking the brakes button, though *not* like using the ``B`` key, because they stay on::

        BRAKES ON.

.. global:: TARGET

    :access: Get/Set
    :type: string

    Where ``name`` is the name of a target vessel or planet, this will set the current target::

        SET TARGET TO name.

Note that the above options also can refer to a different vessel besides the current ship, for example, ``TARGET:THROTTLE`` to read the target's throttle. But not all "set" or "lock" options will work with a different vessel other than the current one, because there's no authority to control a craft the current program is not attached to.
