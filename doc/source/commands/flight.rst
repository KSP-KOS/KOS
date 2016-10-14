.. _flight:

Flight Control
++++++++++++++

.. toctree::
    :maxdepth: 1

    flight/cooked
    flight/raw
    flight/pilot
    flight/systems

Unless otherwise stated, all controls that a **kOS** CPU attempts will be done on the :ref:`CPU Vessel <cpu vessel>`. There are three styles of control:

:ref:`Cooked <cooked>`
    Give a goal direction to seek, and let **kOS** find the way to maneuver toward it.

:ref:`Raw <raw>`
    Control the craft just like a manual pilot would do from a keyboard or joystick.

:ref:`Pilot <pilot>`
    This is the stock way of controlling craft, the state of which can be read in **KerboScript**.

.. warning:: **SAS OVERRIDES kOS**

    With the current implementation of flight control, you may now leave ``SAS`` turned on in ``"STABILITYASSIST"`` mode, and it will not override **kOS**'s attempts to steer the ship. However, it will fight and/or override **kOS**'s attempts to steer when using any other mode.  In order for **kOS** to be able to turn the ship in other modes, you need to set ``SAS OFF`` or ``SET SASMODE TO "STABILITYASSIST"``. You should take care in your scripts to manage the use of ``SAS`` and ``SASMODE`` appropriately. It is common for people writing **kOS** scripts to explicitly start them with a use of the ``SAS OFF`` and/or ``SET SASMODE TO "STABILITYASSIST"`` commands just in case you forgot to turn it off before running the script.  You could also store the current state in a temporary variable, and re-set it at the conclusion of your script.
