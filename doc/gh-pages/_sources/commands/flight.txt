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

    However, SAS will tend to fight and/or override kOS's attempts to
    steer.  In order for **kOS** to be able to turn the ship, you need to
    set ``SAS OFF``. You should take care in your scripts to manage the
    use of ``SAS`` appropriately. It is common for people writing
    **kOS** scripts to explicitly start them with a use of the
    ``SAS OFF`` command just in case you forgot to turn it off before
    running the script.  You could also store the current state in a
    temporary variable, and re-set it at the conclusion of your script.
