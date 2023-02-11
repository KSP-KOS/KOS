.. _stack:

Stack
=====

A :struct:`Stack` is a collection of any type in kOS. Stacks work according to `Last-In first-out <https://en.wikipedia.org/wiki/FIFO_and_LIFO_accounting>`_ principle. It may be useful to contrast
:struct:`Stack` with :struct:`Queue` to better understand how both structures work. You can read more about stacks on `Wikipedia <https://en.wikipedia.org/wiki/Stack_(abstract_data_type)>`_.

Using a stack
-------------

::

  SET S TO STACK().
  S:PUSH("alice").
  S:PUSH("bob").

  PRINT S:POP. // will print 'bob'
  PRINT S:POP. // will print 'alice'

Structure
---------

.. structure:: Stack

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`Enumerable`
          -
          - :struct:`Stack` objects are a type of :struct:`Enumerable`
        * - :meth:`PUSH(item)`
          - None
          - add item to the top of the stack
        * - :meth:`POP()`
          - any type
          - returns the item on top of the stack and removes it
        * - :meth:`PEEK()`
          - any type
          - returns the item on top of the stack without removing it
        * - :meth:`CLEAR()`
          - None
          - remove all elements
        * - :attr:`COPY`
          - :struct:`Stack`
          - a new copy of this stack

.. note::

    This type is serializable.


.. method:: Stack:PUSH(item)

    :parameter item: (any type) item to be added
    
    Adds the item to the top of the stack.

.. method:: Stack:POP()

    Returns the item on top of the stack and removes it.

.. method:: Stack:PEEK()
    
    Returns the item on top of the stack without removing it.

.. method:: Stack:CLEAR()

    Removes all elements from the stack.

.. attribute:: Stack:COPY

    :type: :struct:`Stack`
    :access: Get only

    Returns a new stack that contains the same thing as the old one.
