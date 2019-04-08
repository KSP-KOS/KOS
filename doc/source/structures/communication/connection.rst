.. _connection:

Connection
==========

Connections represent your ability to communicate with other :struct:`processors <kOSProcessor>` or :struct:`vessels <Vessel>`. You can
use them to find out whether such communication is currently possible and send messages.

Obtaining a connection
----------------------

There are 2 types of connections. The first type is used to communicate with other :struct:`processors <kOSProcessor>` within the same vessel.
You can obtain a connection by using `CONNECTION` suffix. Assuming your vessel has a second processor tagged `'second'` it could look like this::

  SET MY_PROCESSOR TO PROCESSOR("second").
  SET MY_CONNECTION TO MY_PROCESSOR:CONNECTION.

The second type are connections to other vessels. Assuming you have a rover on duna named `'dunarover'` you could obtain a connection to it like this::

  SET MY_VESSEL TO VESSEL("dunarover").
  SET MY_CONNECTION TO MY_VESSEL:CONNECTION.


Structure
---------

.. structure:: Connection

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`ISCONNECTED`
          - :struct:`Boolean`
          - true if this connection is currently opened
        * - :attr:`DELAY`
          - :struct:`Scalar`
          - delay in seconds
        * - :attr:`DESTINATION`
          - :struct:`Vessel` or :struct:`kOSProcessor`
          - destination of this connection
        * - :meth:`SENDMESSAGE(message)`
          - :struct:`Boolean`
          - Sends a message using this connection

.. attribute:: Connection:ISCONNECTED

    :type: :struct:`Boolean`

    True if the connection is opened and messages can be sent.
    
    - For CPU connections:
        - This will be true if the destination CPU belongs to the same vessel
          as the current CPU, and will be false otherwise.
    - For Vessel connections:
        - If you are using Stock KSP and chose the PermitAll connectivity
          manager, then this will aways return true.
        - If you are using Stock KSP and chose the CommNet connectivity
          manager, then this will obey the rules of the stock CommNet system
          for whether a connection path exists between the source and
          destination vessel.
        - If you are using the RemoteTech and chose the RemoteTech
          connectivity manager, then this will obey the rules of the
          RemoteTech mod for whether a connection path exists between the
          source and destination vessel.

    The connection has to be opened only in the moment of sending the message in order for it to arrive. If connection is lost after the message was sent,
    but before it arrives at its destination, this will have no effect on whether the message will reach its destination or not.

    .. note::
        **Debris Vessels**: If you are using the KSP Stock CommNet system,
        be aware that it never includes "debris" type vessels in the
        communications network.  ``ISCONNECTED`` will always be false
        for any vessel of type "debris", no matter what antennas it
        may have on it.

     .. note::
        **ISCONNECTION fails just after scene load**: If you have just loaded
        the scene, such as after a vessel switch, then both the Stock CommNet
        system and the RemoteTech mod often have a slight delay before they
        "find" all the communication paths that exist.  This means that
        ISCONNECTION will often return ``False`` for the first second or two
        after a scene load, even when the correct answer should be ``True``.
        It will be unable to report the correct answer until a second or so
        later after the communications paths have all been discovered by the
        game.  Because of this, if you have a boot script that depends on an
        accurate answer for ISCONNECTED, it's a good idea for that boot
        script to start with a short wait of a second or two at the top of
        the script.

.. attribute:: Connection:DELAY

    The number of seconds that it will take for messages sent using this connection to arrive at their destination. This value will be equal to -1 if connection is not opened.

    - For CPU connections:
	- This will be always equal to 0 if the destination CPU belongs
	  to the same vessel as the current CPU.  Otherwise it will be
	  equal to -1 as no such connection is allowed.
    - For vessel connections:
	- If you are using the PermitAll Connectivity Manager, then this
	  will always be zero, as messages arrive instantly.
	- If you are using the stock CommNet Connectivirty Manager, then this
	  will always be zero, as stock CommNet does not impose any delay
	  from radio signals.
	- If you are using the RemoteTech Connectivity Manager, then this
	  will report RemoteTech's signal delay along the path being used
	  to form the connection.  RemoteTech calculates the number of 
	  seconds of delay due radio signals traveling at the speed of light,
	  which can be quite significant when dealing with interplanetary
	  distances.

.. attribute:: Connection:DESTINATION

    :type: :struct:`Vessel` or :struct:`kOSProcessor`

    Destination of this connection. Will be either a vessel or a processor.

.. method:: Connection:SENDMESSAGE(message)

    :parameter message: :struct:`Structure`
    :return: (:struct:`Boolean`) true if the message was successfully sent.

    Send a message using this connection. Any serializable structure or a primitive (:struct:`String`, :struct:`Scalar` or :struct:`Boolean`) can be given as an argument.
    It is always worth checking the return value of this function. A returned false value would indicate that the message was not sent for some reason.
    This method will fail to send the message and return false if :attr:`Connection:ISCONNECTED` is false.
