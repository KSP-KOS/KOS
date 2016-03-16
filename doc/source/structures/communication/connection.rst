.. _connection:

Connection
==========

Connections represent your ability to communicate with other :struct:`processors <kOSProcessor>` or :struct:`vessels <Vessel>`. You can
use them to find out whether such communication is currently possible and send messages.

Obtaining a connection
----------------------

There are 2 types of connections. The first type is used to communicate with other :struct:`processors <kOSProcessor>` within the same vessel.
You can obtain a connection by using `CONNECTION` suffix. Assuming your vessel has a second processor tagged `'second'` it could look like this::

  SET PROCESSOR TO PROCESSOR("second").
  SET CONNECTION TO PROCESSOR:CONNECTION.

The second type are connections to other vessels. Assuming you have a rover on duna named `'dunarover'` you could obtain a connection to it like this::

  SET VESSEL TO VESSEL("dunarover").
  SET CONNECTION TO VESSEL:CONNECTION.

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
          - true if the message was succesfully sent

.. attribute:: Connection:ISCONNECTED

    :type: :struct:`Boolean`

    True if the connection is opened and messages can be sent. For CPU connections this will be always true if the destionation CPU belongs to the same vessel as the current CPU.
    For vessel connections this will be always true in stock game. :ref:`RemoteTech <remotetech>` introduces the concept of connectivity and may cause this to be false.

.. attribute:: Connection:DELAY

    The number of seconds that it will take for messages sent using this connection to arrive at their destination. This value will be equal to -1 if connection is not opened.
    For CPU connections this will be always equal to 0 if the destination CPU belongs to the same vessel as the current CPU.  Otherwise it will be equal to -1.
    For vessel connections this will be always zero in stock game as messages arrive instantaneously. :ref:`RemoteTech <remotetech>` introduces the concept of
    connectivity and may cause this to be a positive number (if there is some signal delay due to the large distance between the vessels) or -1 (if there is no connection
    between the vessels).

.. attribute:: Connection:DESTINATION

    :type: :struct:`Vessel` or :struct:`kOSProcessor`

    Destination of this connection. Will be either a vessel or a processor.

.. method:: Connection:SENDMESSAGE(message)

    :parameter message: :struct:`Structure`
    :return: (:struct:`Boolean`) true if the message was successfully sent.

    Send a message using this connection. Any serializable structure or a primitive (:struct:`String`, :struct:`Scalar` or :struct:`Boolean`) can be given as an argument.
    It is always worth checking the return value of this function. A returned false value would indicate that the message was not sent.
