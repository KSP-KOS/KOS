.. _systems:

Ship Systems
============

CONTROL REFERENCE
-----------------

    The axes for ship control and ship-relative coordinate system are determined
    in relation to a "control from" part (more specifically a transform
    belonging to the part) on the ship. All vessels must have at least one
    "control from" part on them somewhere, which is why there's no mechanism for
    un-setting the "control from" part. You must instead pick another part and
    set it as the "control from" source using the :meth:`Part:CONTROLFROM`
    method.  You may retrieve the current control part using the
    :attr:`Vessel:CONTROLPART` suffix.

RCS and SAS
-----------

.. global:: RCS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Turns the RCS **on** or **off**, like using ``R`` at the keyboard::

        RCS ON. // same as SET RCS TO TRUE.
        RCS OFF. // same as SET RCS TO FALSE.
        PRINT RCS.  // prints either "True" or "False".

.. global:: SAS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Turns the SAS **on** or **off**, like using ``T`` at the keybaord::

        SAS ON. // same as SET SAS TO TRUE.
        SAS OFF. // same as SET SAS TO FALSE.
        PRINT SAS.  // prints either "True" or "False".

    .. warning::

        Be aware that having KSP's ``SAS`` turned on *will* conflict
        with using "cooked control" (the ``lock steering`` command).  You
        should not use these two modes of steering control at the same time.
        For further information see the
        :ref:`warning in lock steering documentation<locksteeringsaswarning>`.

.. _sasmode:

.. object:: SASMODE

    :access: Get/Set
    :type: :struct:`String`

    Getting this variable will return the currently selected SAS mode.  Where ``value`` is one of the valid strings listed below, this will set the stock SAS mode for the cpu vessel::

        SET SASMODE TO value.

    It is the equivalent to clicking on the buttons next to the nav ball while manually piloting the craft, and will respect the current mode of the nav ball (orbital, surface, or target velocity - use NAVMODE to read or set it).  Valid strings for ``value`` are ``"PROGRADE"``, ``"RETROGRADE"``, ``"NORMAL"``, ``"ANTINORMAL"``, ``"RADIALOUT"``, ``"RADIALIN"``, ``"TARGET"``, ``"ANTITARGET"``, ``"MANEUVER"``, ``"STABILITYASSIST"``, and ``"STABILITY"``.  A null or empty string will default to stability assist mode, however any other invalid string will throw an exception.  This feature will respect career mode limitations, and will throw an exception if the current vessel is not able to use the mode passed to the command.  An exception is also thrown if ``"TARGET"`` or ``"ANTITARGET"`` are used when no target is set.

    .. note::
        SAS mode is reset to stability assist when toggling SAS on, however it doesn't happen immediately.
        Therefore, after activating SAS, you'll have to skip a frame before setting the SAS mode.
        Velocity-related modes also reset back to stability assist when the velocity gets too low.

    .. warning:: SASMODE does not work with RemoteTech

        Due to the way that RemoteTech disables flight control input, the built in SAS modes do not function properly when there is no connection to the KSC or a Command Center.  If you are writing scripts for use with RemoteTech, make sure to take this into account.

    .. warning:: SASMODE should not be used with LOCK STEERING

        Be aware that having KSP's ``SAS`` turned on *will* conflict
        with using "cooked control" (the ``lock steering`` command).  You
        should not use these two modes of steering control at the same time.
        For further information see the
        :ref:`warning in lock steering documentation<locksteeringsaswarning>`.

.. _navmode:

.. object:: NAVMODE

    :access: Get/Set
    :type: :struct:`String`

    Getting this variable will return the currently selected nav ball speed display mode.  Where ``value`` is one of the valid strings listed below, this will set the nav ball mode for the cpu vessel::

        SET NAVMODE TO value.

    It is the equivalent to changing the nav ball mode by clicking on speed display on the nav ball while manually piloting the craft, and will change the current mode of the nav ball, affecting behavior of most SAS modes.  Valid strings for ``value`` are ``"ORBIT"``, ``"SURFACE"`` and ``"TARGET"``.  A null or empty string will default to orbit mode, however any other invalid string will throw an exception.  This feature is accessible only for the active vessel, and will throw an exception if the current vessel is not active.  An exception is also thrown if ``"TARGET"`` is used, but no target is selected.

.. _stock-boolean-flags:

STOCK ACTION GROUPS
-------------------

    These action groups (including abovementioned :global:`SAS` and
    :global:`RCS`) are stored as :struct:`Boolean` values which can be read to
    determine their current state.  Reading their value can be used by kOS as
    a form of user input::

        IF RCS PRINT "RCS is on".
        ON ABORT {
            PRINT "Aborting!".
        }

    Using the ``TOGGLE`` command will simply set the value to the opposite of
    the current value.  These two are essentially the same:
    ::

        TOGGLE AG1.
        SET AG1 TO NOT AG1.

    The action groups can be set both by giving ``ON`` or ``OFF`` command
    and by setting the :struct:`Boolean` value. The following commands will have
    the same effect::

        SAS ON.
        SET SAS TO TRUE.

    However, using the ``SET`` command allows the use of any :struct:`Boolean`
    variable or expression, for example::

        SET GEAR TO ALT:RADAR<1000.
        SET LIGHTS TO GEAR.
        SET BRAKES TO NOT BRAKES.

    Some parts automatically add their actions to basic action groups or
    otherwise react to them.  More actions can be added to the groups in the
    editor, if VAB or SPH is advanced enough.

    .. note::
        Pressing an action group's associated key will toggle it's value from
        ``TRUE`` TO ``FALSE`` or from ``FALSE`` to ``TRUE``.  If you are
        attempting to use action groups as user input, make sure to compare it
        to a stored "last value" or use the :ref:`ON Trigger<on_trigger>`

    .. note::
        Assigned actions only react to changes in action group state, therefore
        calling ``GEAR ON.`` when it's already on will have no effect even on
        undeployed landing gear.  The value will first need to be set to ``False``
        before setting it back to ``True``.

    .. note::
        Some actions react differently to toggling the group on and off,
        other will give the same response to both.  For example, landing gear
        will not deploy if they are currently retracted and you set ``GEAR OFF.``.
        However, if an engine is off and the "Toggle Engine" action is linked
        to ``AG1`` which is currently ``True``, calling ``AG1 OFF.`` will turn
        on the engine.

.. global:: LIGHTS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Turns the lights **on** or **off**, like using the ``U`` key at the keyboard::

        LIGHTS ON.

.. global:: BRAKES

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Turns the brakes **on** or **off**, like clicking the brakes button, though *not* like using the ``B`` key, because they stay on::

        BRAKES ON.

.. global:: GEAR

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Deploys or retracts the landing gear, like using the ``G`` key at the keyboard::

        GEAR ON.

.. global:: ABORT

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Abort action group (no actions are automatically assigned, configurable in the editor), like using the ``Backspace`` key at the keyboard::

        ABORT ON.

.. global:: AG1 ... AG10

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    10 custom action groups (no actions are automatically assigned, configurable in the editor), like using the numeric keys at the keyboard::

        AG1 ON.
        AG4 OFF.
        SET AG10 to AG3.

.. _kos-boolean-flags:

kOS PSEUDO ACTION GROUPS
------------------------

    kOS adds several :struct:`Boolean` flags (bound variable fields) that can be used by scripts in the
    same way the stock action groups are used::

        PANELS ON.
        IF BAYS PRINT "Payload/service bays are ajar!".
        SET RADIATORS TO LEGS.

    However, unlike the stock action groups, you can't manually assign actions
    to these fields in the VAB.  They automatically affect all parts of the
    corresponding type.  The biggest difference is that the values for these
    groups are not stored, instead, the value is directly dependent on the state
    of the associated parts.  Another difference from stock groups is that both
    ``ON`` and ``OFF`` commands work independently of the initial state of the
    field.  For example, if some of the payload bays are closed and some are
    open (``BAYS`` would return true), ``BAYS ON`` will still open any bays that
    are currently closed, and ``BAYS OFF`` will close the ones that are opened.

    .. note::
        Because these fields return their value based on the actual status of
        the associated parts, it is not guaranteed that the return value will
        match the value you set immediately.  Some parts may not report the
        new state until an animation has finished, or the part may not be able
        to perform the selected action at this time.

.. global:: LEGS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Deploys or retracts all the landing legs (but not wheeled landing gear)::

        LEGS ON.

    Returns true if all the legs are deployed.

.. global:: CHUTES

    :access: Toggle ON; get/set
    :type: Action Group, :struct:`Boolean`

    Deploys all the parachutes (only `ON` command has effect)::

        CHUTES ON.

    Returns true if all the chutes are deployed.

.. global:: CHUTESSAFE

    :access: Toggle ON; get/set
    :type: Action Group, :struct:`Boolean`

    Deploys all the parachutes than can be safely deployed in the current conditions (only `ON` command has effect)::

        CHUTESSAFE ON.

    Returns false only if there are disarmed parachutes chutes which may be safely
    deployed, and true if all safe parachutes are already deployed including
    any time where there are no safe parachutes.

    The following code will gradually deploy all the chutes as the speed drops::

        WHEN (NOT CHUTESSAFE) THEN {
            CHUTESSAFE ON.
            RETURN (NOT CHUTES).
        }

.. global:: PANELS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Extends or retracts all the deployable solar panels::

        PANELS ON.

    Returns true if all the panels are extended, including those inside of
    fairings or cargo bays.

    .. note::
        Some solar panels can't be retracted once deployed.  Consult the part's
        description for details.

.. global:: RADIATORS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Extends or retracts all the deployable radiators and activates or deactivates all the fixed ones::

        RADIATORS ON.

    Returns true if all the radiators are extended (if deployable) and active.

.. global:: LADDERS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Extends or retracts all the extendable ladders::

        LADDERS ON.

    Returns true if all the ladders are extended.

.. global:: BAYS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Opens or closes all the payload and service bays (including the cargo ramp)::

        BAYS ON.

    Returns true if at least one bay is open.

.. global:: DEPLOYDRILLS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Deploys or retracts all the mining drills::

        DEPLOYDRILLS ON.

    Returns true if all the drills are deployed.

.. global:: DRILLS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Activates (has effect only on drills that are deployed and in contact with minable surface) or stops all the mining drills::

        DRILLS ON.

    Returns true if at least one drill is actually mining.

.. global:: FUELCELLS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Activates or deactivates all the fuel cells (distingushed from other conveters by converter/action names)::

        FUELCELLS ON.

    Returns true if at least one fuel cell is activated.

.. global:: ISRU

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Activates or deactivates all the ISRU converters (distingushed from other conveters by converter/action names)::

        ISRU ON.

    Returns true if at least one ISRU converter is activated.

.. global:: INTAKES

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :struct:`Boolean`

    Opens or closes all the air intakes::

        INTAKES ON.

    Returns true if all the intakes are open.


TARGET
------

.. global:: TARGET

    :access: Get/Set
    :type: :struct:`String` (set); :struct:`Vessel` or :struct:`Body` or :struct:`Part` (get/set)

    Where ``name`` is the name of a target vessel or planet, this will set the current target::

        SET TARGET TO name.

    For more information see :ref:`bindings`.

    NOTE, the way to de-select the target is to set it to an empty
    string like this::

        SET TARGET TO "". // de-selects the target, setting it to nothing.

    (Trying to use :ref:`UNSET TARGET.<unset>` will have no effect because
    ``UNSET`` means "get rid of the variable itself" which you're not
    allowed to do with built-in bound variables like ``TARGET``.)

Note that the above options also can refer to a different vessel besides the current ship, for example, ``TARGET:THROTTLE`` to read the target's throttle. But not all "set" or "lock" options will work with a different vessel other than the current one, because there's no authority to control a craft the current program is not attached to.
