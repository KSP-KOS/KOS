.. _systems:

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

.. _sasmode:

.. object:: SET SASMODE TO value.

    :access: Get/Set
    :type: string

    Getting this variable will return the currently selected mode.  Where ``value`` is one of the valid strings listed below, this will set the stock SAS mode for the cpu vessel::

        SET SASMODE TO value.

    It is the equivalent to clicking on the buttons next to the nav ball while manually piloting the craft, and will respect the current mode of the nav ball (orbital, surface, or target velocity).  Valid strings for ``value`` are ``"PROGRADE"``, ``"RETROGRADE"``, ``"NORMAL"``, ``"ANTINORMAL"``, ``"RADIALOUT"``, ``"RADIALIN"``, ``"TARGET"``, ``"ANTITARGET"``, ``MANEUVER``, ``"STABILITYASSIST"``, and ``"STABILITY"``.  A null or empty string will default to stability assist mode, however any other invalid string will throw an exception.  This feature will respect career mode limitations, and will throw an exception if the current vessel is not able to use the mode passed to the command.  An exception is also thrown if ``"TARGET"`` or ``"ANTITARGET"`` are used, but no target is selected.
		
.. warning:: SASMODE does not work with RemoteTech

    Due to the way that RemoteTech disables flight control input, the built in SAS modes do not function properly when there is no connection to the KSC or a Command Center.  If you are writing scripts for use with RemoteTech, make sure to take this into account.

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
