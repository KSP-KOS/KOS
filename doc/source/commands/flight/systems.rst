.. _systems:

Ship Systems
============

CONTROL REFERENCE
-----------------

    The axis for ship control and ship-relative coordinate system are determined in relation to a specific part (or a specific transform belonging to the part) on the ship.
    All vessels must have at least one "control from"
    part on them somewhere, which is why there's no mechanism for un-setting
    the "control from" setting other than to pick another part and set it
    to that part instead.

.. Vessel:CONTROLPART:

    Returns the part currently used as the control reference for the vessel. 
    For more information see :attr:`Vessel:CONTROLPART`. 

.. Part:CONTROLFROM:

    e.g.::

        set somepart to ship:partstagged("my favorite docking port")[0].
        somepart:CONTROLFROM().

    If you have a handle on a part, from ``LIST PARTS``, you can select that part to set the orientation of the craft, just like using the "control from here" in the right-click menu in the game. For more information see :attr:`Part:CONTROLFROM`. 

RCS and SAS
-----------

.. global:: RCS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Turns the RCS **on** or **off**, like using ``R`` at the keyboard::

        RCS ON.

.. global:: SAS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Turns the SAS **on** or **off**, like using ``T`` at the keybaord::

        SAS ON.

.. _sasmode:

.. object:: SASMODE

    :access: Get/Set
    :type: :ref:`string <string>`

    Getting this variable will return the currently selected mode.  Where ``value`` is one of the valid strings listed below, this will set the stock SAS mode for the cpu vessel::

        SET SASMODE TO value.

    It is the equivalent to clicking on the buttons next to the nav ball while manually piloting the craft, and will respect the current mode of the nav ball (orbital, surface, or target velocity).  Valid strings for ``value`` are ``"PROGRADE"``, ``"RETROGRADE"``, ``"NORMAL"``, ``"ANTINORMAL"``, ``"RADIALOUT"``, ``"RADIALIN"``, ``"TARGET"``, ``"ANTITARGET"``, ``"MANEUVER"``, ``"STABILITYASSIST"``, and ``"STABILITY"``.  A null or empty string will default to stability assist mode, however any other invalid string will throw an exception.  This feature will respect career mode limitations, and will throw an exception if the current vessel is not able to use the mode passed to the command.  An exception is also thrown if ``"TARGET"`` or ``"ANTITARGET"`` are used, but no target is selected.

    .. note::
        SAS mode is reset to stability assist when toggling SAS on, however it doesn't happen immediately.
        Therefore, after activating SAS, you'll have to skip a frame before setting the SAS mode.
        Velocity-related modes also reset back to stability assist when the velocity gets too low.		

.. warning:: SASMODE does not work with RemoteTech

    Due to the way that RemoteTech disables flight control input, the built in SAS modes do not function properly when there is no connection to the KSC or a Command Center.  If you are writing scripts for use with RemoteTech, make sure to take this into account.

STOCK ACTION GROUPS
-------------------

    These action groups (including abovementioned SAS and RCS) are stored as boolean values which can be read to determine their current state, which can be used by kOS as one of the forms of user input::

        IF RCS PRINT "RCS is on".

    The action groups can be toggled both by giving `ON` or `OFF` command and by setting the boolean value. The following commands will have the same effect::

        SAS ON.
        SET SAS TO TRUE.

    However, using the `SET` command allows to use any boolean variable or expression, for example::

        SET GEAR TO ALT:RADAR<1000.
        SET LIGHTS TO GEAR.
        SET BRAKES TO NOT BRAKES.

    Some parts automatically add their actions to basic action groups or otherwise react to them.
    More actions can be added to the groups in the editor, if VAB or SPH is advanced enough.
    .. note::
        Part modules react to changes in action group state, therefore calling `GEAR ON` when it's already on will have no effect even on undeployed gear.
        Some actions react differently to toggling the group on and off, other will give the same response to both.

.. global:: LIGHTS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Turns the lights **on** or **off**, like using the ``U`` key at the keyboard::

        LIGHTS ON.

.. global:: BRAKES

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Turns the brakes **on** or **off**, like clicking the brakes button, though *not* like using the ``B`` key, because they stay on::

        BRAKES ON.

.. global:: GEAR

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Deploys or retracts the landing gear, like using the ``G`` key at the keyboard::

        GEAR ON.

.. global:: ABORT

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Abort action group (no actions are automatically assigned, configurable in the editor), like using the ``Backspace`` key at the keyboard::

        ABORT ON.

.. global:: AG1 ... AG10

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    10 custom action groups (no actions are automatically assigned, configurable in the editor), like using the numeric keys at the keyboard::

        AG1 ON.
        AG4 OFF.
        SET AG10 to AG3.

kOS CUSTOM ACTION GROUPS
------------------------

    kOS adds several action groups that can be used by scripts in the same way the stock groups are used::

        PANELS ON.
        IF BAYS PRINT "Payload/service bays are ajar!".
        SET RADIATORS TO LEGS.

    However, unlike the stock groups, you can't manually assign actions to these groups in the VAB. 
    They automatically affect all the parts of the corresponding type. 
    The biggest difference is that the values for these groups are not stored, instead, the value is directly dependent on the state of the corresponding parts.
    Another difference from stock groups is that both `ON` and `OFF` commands work independantly of the initial state of the action group.
    For example, if some of the payload bays are closed and some are open (`BAYS` would return true), `BAYS ON` will open the ones that were closed, and `BAYS OFF` will close the ones that are opened.


.. global:: LEGS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Deploys or retracts all the landing legs (but not wheeled landing gear)::

        LEGS ON.

    The `LEGS` value is true if all the legs are deployed.

.. global:: CHUTES

    :access: Toggle ON; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Deploys all the parachutes (only `ON` command has effect)::

        CHUTES ON.

    The `CHUTES` value is true if all the chutes are deployed.

.. global:: CHUTESSAFE

    :access: Toggle ON; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Deploys all the parachutes than can be safely deployed in the current conditions (only `ON` command has effect)::

        CHUTESSAFE ON.

    The `CHUTESSAFE` value is false only if there are undeployed chutes to be safely deployed, true if nothing more can be deployed safely. 
    The following code will gradually deploy all the chutes as the speed drops::

        WHEN (NOT CHUTESSAFE) THEN {CHUTESSAFE ON. RETURN (NOT CHUTES).}

.. global:: PANELS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Extends or retracts all the deployable solar panels::

        PANELS ON.

    Note that some of the panels can't be retracted once deployed. The `PANELS` value is true if all the panels are extended.

.. global:: RADIATORS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Extends or retracts all the deployable radiators and activates or deactivates all the fixed ones::

        RADIATORS ON.

    The `RADIATORS` value is true if all the radiators are extended (if deployable) extended and active.

.. global:: LADDERS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Extends or retracts all the extendable ladders::

        LADDERS ON.

    The `LADDERS` value is true if all the ladders are extended.

.. global:: BAYS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Opens or closes all the payload and service bays (including the cargo ramp)::

        BAYS ON.

    The `BAYS` value is true if at least one bay is open.

.. global:: DEPLOYDRILLS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Deploys or retracts all the mining drills::

        DEPLOYDRILLS ON.

    The `DEPLOYDRILLS` value is true if all the drills are deployed.

.. global:: DRILLS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Activates (has effect only on drills that are deployed and in contact with minable surface) or stops all the mining drills::

        DRILLS ON.

    The `DRILLS` value is true if at least one drill is actually mining.

.. global:: FUELCELLS

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Activates or deactivates all the fuel cells (distingushed from other conveters by converter/action names)::

        FUELCELLS ON.

    The `FUELCELLS` value is true if at least one fuel cell is activated.

.. global:: ISRU

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Activates or deactivates all the ISRU converters (distingushed from other conveters by converter/action names)::

        ISRU ON.

    The `ISRU` value is true if at least one ISRU converter is activated.

.. global:: INTAKES

    :access: Toggle ON/OFF; get/set
    :type: Action Group, :ref:`Boolean <boolean>`

    Opens or closes all the air intakes::

        INTAKES ON.

    The value is true if all the intakes are open.


TARGET
------

.. global:: TARGET

    :access: Get/Set
    :type: :ref:`string <string>` (set); `Vessel <structures/vessels/vessel.html>`__ or `Body <structures/celestial_bodies/body.html>`__ or `Part <structures/vessels/part.html>`__ (get)

    Where ``name`` is the name of a target vessel or planet, this will set the current target::

        SET TARGET TO name.

Note that the above options also can refer to a different vessel besides the current ship, for example, ``TARGET:THROTTLE`` to read the target's throttle. But not all "set" or "lock" options will work with a different vessel other than the current one, because there's no authority to control a craft the current program is not attached to.
