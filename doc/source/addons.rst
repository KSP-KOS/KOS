.. _addons:

Addon Reference
===============

    This section is for ways in which kOS has special case
    exceptions to its normal generic behaviours, in order to
    accommodate other KSP mods.  If you don't use any of KSP mods mentioned,
    you don't need to read this section.


.. toctree::

    Action Groups Extended <addons/AGX>
    RemoteTech <addons/RemoteTech>
    Kerbal Alarm Clock <addons/KAC>

To help KOS scripts identify whether or not certain mod is installed and available following suffixed functions were introduced in version 0.17

``ADDONS:AGX:AVAILABLE``
------------------------

Returns True if mod Action Group Extended is installed and available to KOS.


``ADDONS:RT:AVAILABLE``
------------------------

Returns True if mod RemoteTech is installed and available to KOS. See more RemoteTech functions here <addons/RemoteTech>.


``ADDONS:KAC:AVAILABLE``
------------------------

Returns True if mod Kerbal Alarm Clock is installed and available to KOS.