.. _message:

Message
=======

Represents a single message stored in a CPU's or vessel's :struct:`MessageQueue`.

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
          - :struct:`TimeStamp`
          - date this message was sent at
        * - :attr:`RECEIVEDAT`
          - :struct:`TimeStamp`
          - date this message was received at
        * - :attr:`SENDER`
          - :struct:`Vessel` or :struct:`Boolean`
          - vessel which has sent this message, or Boolean false if sender vessel is now gone
        * - :attr:`HASSENDER`
          - :struct:`Boolean`
          - Tests whether or not the sender vessel still exists.
        * - :attr:`CONTENT`
          - :struct:`Structure`
          - message content

.. note::

    This type is serializable.

.. attribute:: Message:SENTAT

    :type: :struct:`TimeStamp`

    Date this message was sent at.

.. attribute:: Message:RECEIVEDAT

    :type: :struct:`TimeStamp`

    Date this message was received at.

.. attribute:: Message:SENDER

    :type: :struct:`Vessel` or :struct:`Boolean`

    Vessel which has sent this message, or a boolean false value if
    the sender vessel no longer exists.

    If the sender of the message doesn't exist anymore (see the explanation
    for :attr:`HASSENDER`), this suffix will return a different type
    altogether.  It will be a :struct:`Boolean` (which is false).

    You can check for this condition either by using the :attr:`HASSENDER`
    suffix, or by checking the ``:ISTYPE`` suffix of the sender to
    detect if it's really a vessel or not.

.. attribute:: Message:HASSENDER

    :type: :struct:`Boolean`

    Because there can be a delay between when the message was sent and
    when it was processed by the receiving script, it's possibile that
    the vessel that sent the message might not exist anymore.  It could
    have either exploded, or been recovered, or been merged into another
    vessel via docking.  You can check the value of the ``:HASSENDER``
    suffix to find out if the sender of the message is still a valid vessel.
    If :attr:`HASSENDER` is false, then :attr:`SENDER` won't give you an
    object of type :struct:`Vessel` and instead will give you just a
    :struct:`Boolean` false.

.. attribute:: Message:CONTENT

    :type: :struct:`Structure`

    Content of this message.
