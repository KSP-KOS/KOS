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
          - true if there are messages in the queue
        * - :attr:`LENGTH`
          - :struct:`Scalar`
          - number of messages in the queue
        * - :meth:`POP()`
          - :struct:`Message`
          - returns the first element in the queue and removes it
        * - :meth:`PEEK()`
          - :struct:`Message`
          - returns the first element in the queue without removing it
        * - :meth:`CLEAR()`
          - None
          - remove all messages
        * - :meth:`PUSH(message)`
          - None
          - explicitely append a message

.. attribute:: MessageQueue:EMPTY

    :type: :struct:`Boolean`

    True if there are message in this queue.

.. attribute:: MessageQueue:LENGTH

    :type: :struct:`Scalar`

    Number of messages in this queue.

.. method:: MessageQueue:POP()

    :parameter item: (any type) item to be added

    Appends the new value given to the end of the list.

.. method:: MessageQueue:PEEK()

    :parameter item: (any type) item to be added

    Appends the new value given to the end of the list.

.. method:: MessageQueue:CLEAR()

    :parameter item: (any type) item to be added

    Appends the new value given to the end of the list.

.. method:: MessageQueue:PUSH(message)

    :parameter message: :struct:`Message` message to be added

    You can use this message to explicitely add a message to a queue.

