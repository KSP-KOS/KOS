.. _communication:

Communication
=============

kOS allows you to write scripts that communicate with scripts running on other processors within the same vessel
(inter-processor communication) or on other vessels (inter-vessel communication).

Messages
--------

The basic unit of data that is sent between two entities (CPUs or vessels) is called a :struct:`Message`.
Messages can contain any primitive (scalars, strings, booleans) data types as well as any
:ref:`serializable <serialization>` types. This allows for a lot of flexibility. It is up to you as a developer
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

Inter-processor communication
-----------------------------

First we'll have a look at the scenario where we want to send messages between two processors on the same vessel. As
a first step we must obtain the :struct:`kOSProcessor` structure associated with the target CPU.

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

Sending messages
~~~~~~~~~~~~~~~~

Once we have a :struct:`kOSProcessor` structure associated with the processor we want to send the message to we can
easily obtain a :struct:`Connection` to that processor using :attr:`kOSProcessor:CONNECTION`. Next we're going to
send a message using :meth:`Connection:SENDMESSAGE`. This is an example of how the whole thing could look::

  SET MESSAGE TO "undock". // can be any serializable value or a primitive
  SET P TO PROCESSOR("probe").
  IF P:SENDMESSAGE(MESSAGE) {
    PRINT "Message sent!".
  }

Receiving messages
~~~~~~~~~~~~~~~~~~

We now switch to the second CPU (in the example above it had a name tag `"probe."`). It should have a message
in its message queue. To access the queue from the current processor we use the :attr:`CORE:MESSAGES` suffix.
The following code will initiate undocking after the `"undock"` message is received::

  WAIT UNTIL NOT CORE:MESSAGES:EMPTY. // make sure we've received something
  SET RECEIVED TO CORE:MESSAGES:POP.
  IF RECEIVED:CONTENT = "undock" {
    PRINT "Undocking!!!".
    UNDOCK().
  } ELSE {
    PRINT "Unexpected message: " + RECEIVED:CONTENT.
  }


Inter-vessel communication
--------------------------

This will be very similar to how inter-processor communication was done. We'll just have to use
:struct:`vessel(vesselName) <Vessel>` to find the vessel we want to send the message to
and then :attr:`Vessel:CONNECTION` to get the connection. This is how sender's code could look like::


  SET MESSAGE TO "HELLO". // can be any serializable value or a primitive
  SET C TO VESSEL("SOMEVESSEL"):CONNECTION.
  PRINT "Delay is " + C:DELAY + " seconds".
  IF C:SENDMESSAGE(MESSAGE) {
    PRINT "Message sent!".
  }

The CPU on the receiving vessel will use :attr:`SHIP:MESSAGES <Vessel:MESSAGES>` to access the vessel's message queue::

  WHEN NOT SHIP:MESSAGES:EMPTY {
    SET RECEIVED TO SHIP:MESSAGES:POP. // or do something else...
    PRINT "Sent by " + RECEIVED:SENDER:NAME + " at " + RECEIVED:SENTAT.
    PRINT RECEIVED:CONTENT.
  }
