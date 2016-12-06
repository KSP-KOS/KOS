.. _addons:

Addon Reference
===============

    This section is for ways in which kOS has special case exceptions to its normal generic behaviours, in order to accommodate other KSP mods.  If you don't use any of KSP mods mentioned, you don't need to read this section.


.. toctree::

    Action Groups Extended <addons/AGX>
    RemoteTech <addons/RemoteTech>
    Kerbal Alarm Clock <addons/KAC>
    Infernal Robotics <addons/IR>
    DMagic Orbital Science <addons/OrbitalScience>
    Trajectories <addons/Trajectories>

To help KOS scripts identify whether or not certain mod is installed and available following suffixed functions were introduced in version 0.17

``ADDONS:AVAILABLE("AGX")``
---------------------------

Returns True if mod Action Group Extended is installed and available to KOS.


``ADDONS:AVAILABLE("RT")``
--------------------------

Returns True if mod RemoteTech is installed and available to KOS. See more RemoteTech functions :doc:`here <addons/RemoteTech>`.


``ADDONS:AVAILABLE("KAC")``
---------------------------

Returns True if mod Kerbal Alarm Clock is installed and available to KOS.


``ADDONS:AVAILABLE("IR")``
--------------------------

Returns True if mod Infernal Robotics is installed, available to KOS and applicable to current craft. See more :doc:`here <addons/IR>`.

``ADDONS:TR:AVAILABLE``
------------------------

Returns True if a compatible version of the mod Trajectories is installed. See more :doc:`here <addons/Trajectories>`.
