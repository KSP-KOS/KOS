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

Starting version 0.17 of kOS you can access structure RTAddon via `ADDONS:RT`.

.. structure:: RTAddon

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
     :attr:`AVAILABLE`                     bool(readonly)            True if RT is installed and RT integration enabled.
     :meth:`DELAY(vessel)`                 double                    Get shortest possible delay to given :struct:`Vessel`
     :meth:`KSCDELAY(vessel)`              double                    Get delay from KSC to given :struct:`Vessel`
     :meth:`HASCONNECTION(vessel)`         bool                      True if given :struct:`Vessel` has any connection
     :meth:`HASKSCCONNECTION(vessel)`      bool                      True if given :struct:`Vessel` has connection to KSC
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

.. method:: RTAddon:HASCONNECTION(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: bool

    Returns True if `vessel` has any connection (including to local command posts).

.. method:: RTAddon:HASKSCCONNECTION(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: bool

    Returns True if `vessel` has connection to KSC.
