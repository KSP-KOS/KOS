.. _queue:

Queue
=====

A :struct:`Queue` is a collection of any type in kOS. Queues work according to `First-In first-out <https://en.wikipedia.org/wiki/FIFO_and_LIFO_accounting>`_ principle. It may be useful to contrast
:struct:`Queue` with :struct:`Stack` to better understand how both structures work. You can read more about queues on `Wikipedia <https://en.wikipedia.org/wiki/Queue_(abstract_data_type)>`_.

Using a queue
-------------

::

  SET Q TO QUEUE().
  Q:PUSH("alice").
  Q:PUSH("bob").

  PRINT Q:POP. // will print 'alice'
  PRINT Q:POP. // will print 'bob'

Structure
---------

.. structure:: Queue

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :meth:`PUSH(item)`
          - None
          - add item to the end of the queue
        * - :meth:`POP()`
          - any type
          - returns the first element in the queue and removes it
        * - :meth:`PEEK()`
          - any type
          - returns the first element in the queue without removing it
        * - :attr:`LENGTH`
          - integer
          - number of elements in the queue
        * - :meth:`CLEAR()`
          - None
          - remove all elements
        * - :attr:`ITERATOR`
          - :struct:`Iterator`
          - for iterating over the queue
        * - :attr:`COPY`
          - :struct:`Queue`
          - a new copy of this queue
        * - :meth:`CONTAINS(item)`
          - boolean
          - check if queue contains an item
        * - :attr:`EMPTY`
          - boolean
          - check if queue if empty
        * - :attr:`DUMP`
          - string
          - verbose dump of all contained elements



.. method:: Queue:PUSH(item)

    :parameter item: (any type) item to be added
    
    Adds the item to the end of the queue.

.. method:: Queue:POP()

    Returns the item in the front of the queue and removes it.

.. method:: Queue:PEEK()
    
    Returns the item in the front of the queue without removing it.

.. method:: Queue:CLEAR()

    Removes all elements from the queue.

.. attribute:: Queue:LENGTH

    :type: integer
    :access: Get only

    Returns the number of elements in the queue.

.. attribute:: Queue:ITERATOR

    :type: :struct:`Iterator`
    :access: Get only

    An alternate means of iterating over a queue. See: :struct:`Iterator`.

.. attribute:: Queue:COPY

    :type: :struct:`Queue`
    :access: Get only

    Returns a new queue that contains the same thing as the old one.

.. method:: Queue:CONTAINS(item)

    :parameter index: (integer) starting index (from zero)
    :return: boolean

    Returns true if the queue contains an item equal to the one passed as an argument

.. attribute:: Queue:EMPTY

    :type: boolean
    :access: Get only

    Returns true if the queue has zero items in it.

.. attribute:: Queue:DUMP

    :type: string
    :access: Get only

    Returns a string containing a verbose dump of the queue's contents.

