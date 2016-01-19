.. _remotetech:

RemoteTech
==========

RemoteTech is a modification for Squad’s "Kerbal Space Program" (KSP) which overhauls the unmanned space program. It does this by requiring unmanned vessels have a connection to Kerbal Space Center (KSC) to be able to be controlled. This adds a new layer of difficulty that compensates for the lack of live crew members.

- Download: http://kerbalstuff.com/mod/134/RemoteTech
- Sources: https://github.com/RemoteTechnologiesGroup/RemoteTech
- Documentation: http://remotetechnologiesgroup.github.io/RemoteTech/


Interaction with kOS
--------------------

When you have RemoteTech installed you can only interact with the core's terminal when you have a connection to KSC on any unmanned craft. Scripts launched when you still had a connection will continue to execute even if your unmanned craft loses connection to KSC. But you should note, that when there is no connection to KSC the archive volume is inaccessible. This will require you to plan ahead and copy necessary scripts for your mission to probe hard disk, if your kerbals and/or other scripts need to use them while not connected.

If you launch a manned craft while using RemoteTech, you are still able to input commands from the terminal even if you do not have a connection to the KSC.  The archive will still be inaccessible without a connection to the KSC.  Under the current implementation, there is no delay when accessing the archive with a local terminal.  This implementation may change in the future to account for delays in reading and writing data over the connection.

It is possible to activate/deactivate RT antennas, as well as set their targets using kOS::

  SET P TO SHIP:PARTSNAMED("mediumDishAntenna")[0].
  SET M to p:GETMODULE("ModuleRTAntenna").
  M:DOEVENT("activate").
  M:SETFIELD("target", "Mission Control").
  M:SETFIELD("target", mun).
  M:SETFIELD("target", somevessel).
  M:SETFIELD("target", "minmus").

Acceptable values for `"target"` are:

- `"no-target"`
- `"active-vessel"`
- a :struct:`Body`
- a :struct:`Vessel`
- a string containing the name of a body or vessel
- a string containing the name of a ground station (case-sensitive)

You can use :meth:`RTADDON:GROUNDSTATIONS` to get a list of all ground stations. The default ground station is called `"Mission Control"`.

Starting version 0.17 of kOS you can access structure RTAddon via `ADDONS:RT`.

.. structure:: RTAddon

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
     :attr:`AVAILABLE`                     bool(readonly)            True if RT is installed and RT integration enabled.
     :meth:`DELAY(vessel)`                 double                    Get shortest possible delay to given :struct:`Vessel`
     :meth:`KSCDELAY(vessel)`              double                    Get delay from KSC to given :struct:`Vessel`
     :meth:`ANTENNAHASCONNECTION(part)`    bool                      True if given :struct:`Part` has any connection
     :meth:`HASCONNECTION(vessel)`         bool                      True if given :struct:`Vessel` has any connection
     :meth:`HASKSCCONNECTION(vessel)`      bool                      True if given :struct:`Vessel` has connection to KSC
     :meth:`HASLOCALCONTROL(vessel)`       bool                      True if given :struct:`Vessel` has local control
     :meth:`GROUNDSTATIONS()`              :struct:`List`            Get names of all ground stations
    ===================================== ========================= =============



.. attribute:: RTADDON:AVAILABLE

    :type: bool
    :access: Get only

    True if RT is installed and RT integration enabled.

.. method:: RTAddon:DELAY(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: (double) seconds

    Returns shortest possible delay for `vessel` (Will be less than KSC delay if you have a local command post).

.. method:: RTAddon:KSCDELAY(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: (double) seconds

    Returns delay in seconds from KSC to `vessel`.

.. method:: RTAddon:ANTENNAHASCONNECTION(part)

    :parameter part: :struct:`Part`
    :return: bool

    Returns True if `part` has any connection (including to local command posts).

.. method:: RTAddon:HASCONNECTION(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: bool

    Returns True if `vessel` has any connection (including to local command posts).

.. method:: RTAddon:HASKSCCONNECTION(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: bool

    Returns True if `vessel` has connection to KSC.

.. method:: RTAddon:HASLOCALCONTROL(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: bool

    Returns True if `vessel` has local control (and thus not requiring a RemoteTech connection).

.. method:: RTAddon:GROUNDSTATIONS()

    :return: :struct:`List`

    Returns names of all RT ground stations
