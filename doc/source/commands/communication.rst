.. _communication:

Communication
=============

kOS allows you to write scripts that communicate with scripts running on other processors within the same vessel
(inter-processor communication) or on other vessels (inter-vessel communication).

Limitations
-----------

While you are able to send messages to vessels that are unloaded, the receiving
vessel must be loaded in order to use and reply to the message.  This is because
kOS is unable to run on an unloaded vessel.  The loaded status of a vessel
depends on proximity to the active vessel (usually a sphere a couple of
kilometers in radius) as well as current situation (landed, orbit, suborbit,
etc).  For more information about how vessels are loaded, check the
:struct:`loaddistance` documentation page.  In order to have the receiving
vessel reply when unloaded, it will need to be set to the
:attr:`KUNIVERSE:ACTIVEVESSEL` or the load distance needs to be adjusted.

Messages
--------

The basic unit of data that is sent between two entities (CPUs or vessels) is called a :struct:`Message`.
Messages can contain any primitive (scalars, strings, booleans) data types as well as any
:ref:`serializable <serialization>` types. This allows for a lot of flexibility. It is up to you as a script author
to decide what those messages will need to contain in order to achieve a specific task.

kOS will automatically add 3 values to every message that is sent: :attr:`Message:SENTAT`, :attr:`Message:RECEIVEDAT`
and :attr:`Message:SENDER`. The original data that the sender has sent can be accessed using :attr:`Message:CONTENT`.

Message queues
--------------

Whenever a message is received by a CPU or a vessel it is added to its :struct:`MessageQueue`. Message queues
store messages in order they were received in and allow the recipient to read them in the same order.

**It is important to understand that every CPU has its own message queue, but also every vessel as a whole has
its own queue**. A vessel that has 2 processors has 3 message queues in total: one for each of the CPUs and one
for the vessel. Why does the vessel has its own, separate queue? Well, if it hadn't then you as a sender would
have to know the names of processors on board the target vessel in order to send the message to a specific CPU.
That would complicate the whole process - you would have to store those names for every vessel you plan on contacting
in the future. When every vessel has a separate queue the sender doesn't have to worry about how that message will be
handled by the recipient. It also allows you to easily differentiate between messages coming from other CPUs on board
the same vessel and messages coming from other vessels.  The downside is of course that in certain complex cases it
might be necessary for a CPU to operate on two message queues - its own and vessel's.

There is one major difference in how CPU and vessel queues are handled. **Messages on vessel queues are persisted when
changing scenes in KSP while messages on CPU queues are not**. This difference stems from the fact that when you're
switching from one vessel to another that is further than 2.5km away KSP actually saves the game, constructs a new
scene and loads the game from that previously created save file. If messages hadn't been added to the save file they
would be lost and any kind of long distance inter-vessel communication would be impossible. Obviously in the case of
inter-processor communication all processors are part of the same vessel. No scene changes are required and hence no
need to persist messages.

Connections
-----------

:struct:`Connection` structure represents your ability to communicate with a processor or a vessel. Whenever you want
to send a message you need to obtain a connection first.

Inter-vessel communication
--------------------------

First we'll have a look at the scenario where we want to send messages between two processors on different vessels. As
the first step we must obtain the :struct:`Vessel` structure associated with the target vessel. We can do that using::

  SET V TO VESSEL("vesselname").

Sending messages
~~~~~~~~~~~~~~~~

Once we have a :struct:`Vessel` structure associated with the vessel we want to send the message to we can
easily obtain a :struct:`Connection` to that vessel using :attr:`Vessel:CONNECTION`. Next we're going to
send a message using :meth:`Connection:SENDMESSAGE`. This is an example of how the whole thing could look::

  SET MESSAGE TO "HELLO". // can be any serializable value or a primitive
  SET C TO VESSEL("probe"):CONNECTION.
  PRINT "Delay is " + C:DELAY + " seconds".
  IF C:SENDMESSAGE(MESSAGE) {
    PRINT "Message sent!".
  }

Receiving messages
~~~~~~~~~~~~~~~~~~

We now switch to the second vessel (in the example above it was named `"probe."`). It should have a message
in its message queue. To access the queue from the current processor we use the :attr:`SHIP:MESSAGES <Vessel:MESSAGES>` suffix::

  WHEN NOT SHIP:MESSAGES:EMPTY {
    SET RECEIVED TO SHIP:MESSAGES:POP.
    PRINT "Sent by " + RECEIVED:SENDER:NAME + " at " + RECEIVED:SENTAT.
    PRINT RECEIVED:CONTENT.
  }

Inter-processor communication
-----------------------------

This will be very similar to how inter-vessel communication was done. As
the first step we must obtain the :struct:`kOSProcessor` structure associated with the target CPU.

Accessing processors
~~~~~~~~~~~~~~~~~~~~

The easiest way of accessing the processor's :struct:`kOSProcessor` structure (as long as your CPUs have their
:ref:`name tags <nametag>` set) is to use the following function:

.. function:: PROCESSOR(volumeOrNameTag)

    :parameter volumeOrNameTag: (:struct:`Volume` | `String`) can be either an instance of :struct:`Volume` or a string

    Depending on the type of the parameter value will either return the processor associated with the given
    :struct:`Volume` or the processor with the given name tag.

A list of all processors can be obtained using the :ref:`List <list>` command::

  LIST PROCESSORS IN ALL_PROCESSORS.
  PRINT ALL_PROCESSORS[0]:NAME.

Finally, processors can be accessed directly, like other :ref:`parts and modules <part>`::

  PRINT SHIP:MODULESNAMED("kOSProcessor")[0]:VOLUME:NAME.

Sending and receiving messages
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Then we can use :attr:`kOSProcessor:CONNECTION` to get the connection to that processor. This is how sender's code could look like::

  SET MESSAGE TO "undock". // can be any serializable value or a primitive
  SET P TO PROCESSOR("probe").
  IF P:CONNECTION:SENDMESSAGE(MESSAGE) {
    PRINT "Message sent!".
  }

The receiving CPU will use :attr:`CORE:MESSAGES` to access its message queue::

  WAIT UNTIL NOT CORE:MESSAGES:EMPTY. // make sure we've received something
  SET RECEIVED TO CORE:MESSAGES:POP.
  IF RECEIVED:CONTENT = "undock" {
    PRINT "Undocking!!!".
    UNDOCK().
  } ELSE {
    PRINT "Unexpected message: " + RECEIVED:CONTENT.
  }

.. _connectivityManagers:

Connectivity Managers
---------------------

.. note::

    .. versionadded:: v1.0.2
        The concept of selectable connectivity managers was added after KSP
        introduced a stock communications system (CommNet). kOS was updated to
        support both CommNet and RemoteTech. Other mods may be supported or
        provide their own support in the future.

kOS can implement communications over a variaty of connectivity configurations.
We refer to these options as "Connectivity Managers." You can slect the active
manager from the :ref:`kOS section of KSP's Difficulty Settings<settingsWindow>`.
If the currently selected manager no longer is available, or if a new manager
becomes available, you will be prompted with a dialog box to select the manager
you want to use.

.. object:: Connectivity Managers

    By default kOS supports the following list of managers. If a manager is not
    currently available (because the required mod isn't installed, or the system
    is disabled in the settings) it will not be shown in lists of available managers.

    PermitAllConnectivityManager
      This manager will permit all connectivity at all times.  Connections
      between vessels, home, and control always show as being connected. This
      is the equivalent of setting ``CONFIG:RT`` to ``False`` under the former
      system. It is possible kOS will be unable to use all features if this
      manager is selected while communication limitations are enforced by
      another mod or a setting.
    CommNetConnectivityManager
      This manager will use KSP's stock CommNet implementation to monitor
      connections.  It will only be available if CommNet is enabled in the KSP
      difficulty settings.

      .. note::
          **Active Vessel**: CommNet has limitations on updating connections for vessels
          which are not the active vessel.  The best way to ensure that a connection
          is updated is to include one of the kinds of antenna that can act as
          a relay on one or both of the vessels.
      .. note::
          **Debris Vessel**: To save on computer load, CommNet cuts all "debris"
          vessels out of the system when loading the scene and does not track
          them.  Therefore any vessel that was of type "debris" when the scene
          was first loaded will be unable to have a connection, regardless of
          what antennas it may have on it, and regardless of whether you change
          its vessel type to something other than "debris" during the scene.
          The vessel type has to be something other than "debris" *at the moment
          the scene is first loaded* in order for it to be able to have CommNet
          connections.  If you have a "debris" vessel that has no communication
          connections, you can change the vessel type from "debris" to something
          else, *and then go to the tracking station and come back*, then *that*
          can cause the vessel to have a connection again.
          
    RemoteTechConnectivityManager
      This manager will use the RemoteTech mod to monitor connections. It will
      only be available if RemoteTech is installed.  You can access more
      detailed information and methods using the :ref:`RemoteTech Addon<remotetech>`

    .. warning::

        Take care when configuring your game for connectivity.  Enabling
        multiple systems at the same time may result in unexpected behaviors
        both for the game connectivity itself, and for kOS's connectivity
        manager.  kOS only supports selecting one connectivity manager at a time
        and you should ensure that only the corresponding in game connectivity
        system is enabled.

You can check communication status between vessels by checking the
:attr:`Vessel:CONNECTION`.  To monitor the status for home and control
connections the following bound variables are available.  This allows you to
monitor the basic network status using a single unified system, regardless of
which connectivity manager is selected.

.. global:: HOMECONNECTION

    Returns a :struct:`Connection` representing the :ref:`CPU Vessel's<cpu vessel>`
    communication line to a network "home" node.  This home node may be the KSC
    ground station, or other ground stations added by the CommNet settings or
    RemoteTech.  Functionally, this connection may be used to determine if the
    archive volume is accessible.

    .. warning::

        Attempting to send a message to the "home" connection will result in an
        error message.  While this connection uses the same structure as when
        sending inter-vessel and inter-processor messages, message support is
        not included.

.. global:: CONTROLCONNECTION

    Returns a :struct:`Connection` representing the :ref:`CPU Vessel's<cpu vessel>`
    communication line to a control source.  This may be the same as the
    :global:`HOMECONNECTION`, or it may represent a local crewed command pod,
    or it may represent a connection to a control station.  When using the
    ``CommNetConnectivityManager`` this should show as connected whenever a vessel
    has partial manned control, or full control.  Functionally this may be used
    to determine if terminal input is available, and what the potential signal
    delay may be for this input.

    .. warning::

        Attempting to send a message to the "control" connection will result in
        an error message.  While this connection uses the same structure as when
        sending inter-vessel and inter-processor messages, message support is
        not included.
