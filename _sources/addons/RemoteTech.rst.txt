.. _remotetech:

RemoteTech
==========

RemoteTech is a modification for Squad's "Kerbal Space Program" (KSP) which overhauls the unmanned space program. It does this by requiring unmanned vessels have a connection to Kerbal Space Center (KSC) to be able to be controlled. This adds a new layer of difficulty that compensates for the lack of live crew members.

- Download: http://spacedock.info/mod/520/RemoteTech
- Sources: https://github.com/RemoteTechnologiesGroup/RemoteTech
- Documentation: http://remotetechnologiesgroup.github.io/RemoteTech/

You can find out if the RemoteTech addon is available in the
current game installation by usng the boolean expression::

    addons:available("RT")

Then you can access the Remote Tech addon with::

    set myRemoteTech to addons:RT.

Quick example
-------------

A quick example of usage::

    if addons:available("RT") {
      local myRT is addons:RT.
      print "The delay from KSC to Myself is:".
      print myRT:KSCDELAY(ship) + " seconds.".
    }

Connectivity Manager
--------------------

Note, that some of the methods in here can be handled more generically
regardless of whether RT is installed or not, by using the methods
:ref:`in the Connectivity Manager <connectivityManagers>`.  The
ConnectivityManager can abstract away the differences between 
communication mods.

Interaction with kOS
--------------------

.. note::

    .. versionadded:: v1.0.2
        kOS now supports access to connection informaion from a unified location.
        See :ref:`Connectivity Managers <connectivityManagers>` for more
        information. All of the previous implementation as detailed on this page
        remains supported.

When you have RemoteTech installed you can only interact with the core's terminal when you have a connection to KSC on any unmanned craft. Scripts launched when you still had a connection will continue to execute even if your unmanned craft loses connection to KSC. But you should note, that when there is no connection to KSC the archive volume is inaccessible. This will require you to plan ahead and copy necessary scripts for your mission to probe hard disk, if your kerbals and/or other scripts need to use them while not connected.

If you launch a manned craft while using RemoteTech, you are still able to input commands from the terminal even if you do not have a connection to the KSC.  The archive will still be inaccessible without a connection to the KSC.  Under the current implementation, there is no delay when accessing the archive with a local terminal.  This implementation may change in the future to account for delays in reading and writing data over the connection.

Remote Tech and the kOS GUI widgets
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. versionadded:: v1.1.0

The kOS :ref:`GUI widget <gui>` system tries to
:ref:`obey the signal delay <gui_delay>` imposed by RemoteTech,
when Remote Tech is installed.  A user's interaction with
GUI widgets on the screen will be subject to the same
signal delay rules as the interactive flying controls of the
ship.


Antennas
~~~~~~~~

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

Communication
~~~~~~~~~~~~~

When installed RemoteTech will influence :ref:`communication <communication>` between vessels. In order to send a message to another vessel a valid RemoteTech connection will have to exist between them
and of course messages will arrive to their destination with a proper delay. Documentation of :struct:`Connection` class contains further information on how RemoteTech will change the behaviour
of some of its suffixes.

RTAddon
~~~~~~~

This is obtained with ``Addons:RT``.


.. structure:: RTAddon

    ===================================== ===================================== =============
     Suffix                                Type                                  Description
    ===================================== ===================================== =============
     :attr:`AVAILABLE`                     :struct:`Boolean` (readonly)          True if RT is installed and RT integration enabled. It is better to use ``addons:available("IR")`` for this.
     :meth:`DELAY(vessel)`                 :struct:`Scalar`                      Get shortest possible delay to given :struct:`Vessel`
     :meth:`KSCDELAY(vessel)`              :struct:`Boolean`                     Get delay from KSC to given :struct:`Vessel`
     :meth:`ANTENNAHASCONNECTION(part)`    :struct:`Boolean`                     True if given :struct:`Part` has any connection
     :meth:`HASCONNECTION(vessel)`         :struct:`Boolean`                     True if given :struct:`Vessel` has any connection
     :meth:`HASKSCCONNECTION(vessel)`      :struct:`Boolean`                     True if given :struct:`Vessel` has connection to KSC
     :meth:`HASLOCALCONTROL(vessel)`       :struct:`Boolean`                     True if given :struct:`Vessel` has local control
     :meth:`GROUNDSTATIONS()`              :struct:`List` of :struct:`String`    Get names of all ground stations
    ===================================== ===================================== =============



.. attribute:: RTADDON:AVAILABLE

    :type: :struct:`Boolean`
    :access: Get only

    True if RT is installed and RT integration enabled.

    It is better to use ``ADDONS:AVAILABLE("RT")`` first to discover if
    RemoteTech is installed.

.. method:: RTAddon:DELAY(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: (:struct:`Scalar`) seconds

    Returns shortest possible delay for `vessel` (Will be less than KSC delay if you have a local command post).

.. method:: RTAddon:KSCDELAY(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: (:struct:`Scalar`) seconds

    Returns delay in seconds from KSC to `vessel`.

.. method:: RTAddon:ANTENNAHASCONNECTION(part)

    :parameter part: :struct:`Part`
    :return: :struct:`Boolean`

    Returns True if `part` has any connection (including to local command posts).

.. method:: RTAddon:HASCONNECTION(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: :struct:`Boolean`

    Returns True if `vessel` has any connection (including to local command posts).

.. method:: RTAddon:HASKSCCONNECTION(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: :struct:`Boolean`

    Returns True if `vessel` has connection to KSC.

.. method:: RTAddon:HASLOCALCONTROL(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: :struct:`Boolean`

    Returns True if `vessel` has local control (and thus not requiring a RemoteTech connection).

.. method:: RTAddon:GROUNDSTATIONS()

    :return: :struct:`List` of :struct:`String`

    Returns names of all RT ground stations
