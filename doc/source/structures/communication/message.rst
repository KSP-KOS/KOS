.. _message:

Message
=======

Represents a single message stored in a CPU's or vessel's :struct:`message queue <MessageQueue>`.

The main message content that the sender intended to send can be retrieved using :attr:`Message:CONTENT` attribute. Other suffixes are
automatically added to every message by kOS.

Messages are serializable and thus can be passed along::

  // if there is a message in the ship's message queue
  // we can forward it to a different CPU

  // cpu1
  SET CPU2 TO PROCESSOR("cpu2").
  CPU2:CONNECTION:SENDMESSAGE(SHIP:MESSAGES:POP).

  // cpu2
  SET RECEIVED TO CORE:MESSAGES:POP.
  PRINT "Original message sent at: " + RECEIVED:CONTENT:SENTAT.

Structure
---------

.. structure:: Message

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`SENTAT`
          - :struct:`TimeSpan`
          - date this message was sent at
        * - :attr:`RECEIVEDAT`
          - :struct:`TimeSpan`
          - date this message was received at
        * - :attr:`SENDER`
          - :struct:`Vessel`
          - vessel which has sent this message
        * - :attr:`CONTENT`
          - :struct:`Structure`
          - message content

.. note::

    This type is serializable.

.. attribute:: Message:SENTAT

    :type: :struct:`TimeSpan`

    Date this message was sent at.

.. attribute:: Message:RECEIVEDAT

    :type: :struct:`TimeSpan`

    Date this message was received at.

.. attribute:: Message:SENDER

    :type: :struct:`Vessel`

    Vessel which has sent this message.

.. attribute:: Message:CONTENT

    :type: :struct:`Structure`

    Content of this message.
