.. _flight:

Flight Control
++++++++++++++

.. toctree::
    :maxdepth: 1

    flight/cooked
    flight/raw
    flight/pilot
    flight/systems
    flight/warp

Unless otherwise stated, all controls that a **kOS** CPU attempts will be done on the :ref:`CPU Vessel <cpu vessel>`. There are three styles of control:

:ref:`Cooked <cooked>`
    Give a goal direction to seek, and let **kOS** find the way to maneuver toward it.

:ref:`Raw <raw>`
    Control the craft just like a manual pilot would do from a keyboard or joystick.

:ref:`Pilot <pilot>`
    This is the stock way of controlling craft, the state of which can be read in **KerboScript**.

.. warning:: **SAS OVERRIDES kOS**

    With the current implementation of flight control, if you leave ``SAS`` turned on, it will override **kOS**'s attempts to steer the ship. In order for **kOS** to be able to turn the ship, you need to set ``SAS OFF``. In manual control, you can pilot with ``SAS ON``, because the pilot's manual controls override the ``SAS`` and "fight" against it. In **KOS** no such ability exists. If ``SAS`` is on, **kOS** won't be able to turn the ship. It is common for people writing **kOS** scripts to explicitly start them with a use of the ``SAS OFF`` command just in case you forgot to turn it off before running the script.
