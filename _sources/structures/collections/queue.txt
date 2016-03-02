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

        * - All suffixes of :struct:`Enumerable`
          -
          - :struct:`Queue` objects are a type of :struct:`Enumerable`
        * - :meth:`PUSH(item)`
          - None
          - add item to the end of the queue
        * - :meth:`POP()`
          - any type
          - returns the first element in the queue and removes it
        * - :meth:`PEEK()`
          - any type
          - returns the first element in the queue without removing it
        * - :meth:`CLEAR()`
          - None
          - remove all elements
        * - :attr:`COPY`
          - :struct:`Queue`
          - a new copy of this queue

.. note::

    This type is serializable.


.. method:: Queue:PUSH(item)

    :parameter item: (any type) item to be added
    
    Adds the item to the end of the queue.

.. method:: Queue:POP()

    Returns the item in the front of the queue and removes it.

.. method:: Queue:PEEK()
    
    Returns the item in the front of the queue without removing it.

.. method:: Queue:CLEAR()

    Removes all elements from the queue.

.. attribute:: Queue:COPY

    :type: :struct:`Queue`
    :access: Get only

    Returns a new queue that contains the same thing as the old one.

