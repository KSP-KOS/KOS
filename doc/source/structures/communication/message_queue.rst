.. _message_queue:

MessageQueue
============

Just like ordinary :struct:`queues <Queue>` message queues work according to `First-In first-out <https://en.wikipedia.org/wiki/FIFO_and_LIFO_accounting>`_ principle.
You can read more about queues on `Wikipedia <https://en.wikipedia.org/wiki/Queue_(abstract_data_type)>`_.

Whenever you send a message to a CPU or a vessel it gets added to the end of that CPU's or vessel's message queue. The recipient can then read those messages from the queue.

Accessing message queues
------------------------

You can access the current processor's message queue using :attr:`CORE:MESSAGES`::

  SET QUEUE TO CORE:MESSAGES.
  PRINT "Number of messages on the queue: " + QUEUE:LENGTH.

The current vessel's message queue can be accessed using :attr:`Vessel:MESSAGES`::

  SET QUEUE TO SHIP:MESSAGES.

Structure
---------

.. structure:: MessageQueue

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`EMPTY`
          - :struct:`Boolean`
          - true if there are no messages in the queue
        * - :attr:`LENGTH`
          - :struct:`Scalar`
          - number of messages in the queue
        * - :meth:`POP()`
          - :struct:`Message`
          - returns the oldest element in the queue and removes it
        * - :meth:`PEEK()`
          - :struct:`Message`
          - returns the oldest element in the queue without removing it
        * - :meth:`CLEAR()`
          - None
          - remove all messages
        * - :meth:`PUSH(message)`
          - None
          - explicitly append a message

.. attribute:: MessageQueue:EMPTY

    :type: :struct:`Boolean`

    True if there are no messages in this queue.

.. attribute:: MessageQueue:LENGTH

    :type: :struct:`Scalar`

    Number of messages in this queue.

.. method:: MessageQueue:POP()

    Returns the first (oldest) message in the queue and removes it. Messages in the queue are always ordered by their arrival date.

.. method:: MessageQueue:PEEK()

    :return: :struct:`Message`

    Returns the oldest message in the queue without removing it from the queue.

.. method:: MessageQueue:CLEAR()

    Removes all messages from the queue.

.. method:: MessageQueue:PUSH(message)

    :parameter message: :struct:`Message` message to be added

    You can use this message to explicitly add a message to this queue. This will insert this exact message to the queue, all attributes that are normally
    added automatically by kOS (:attr:`Message:SENTAT`, :attr:`Message:RECEIVEDAT` and :attr:`Message:SENDER`) will not be changed.
