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

        * - :meth:`PUSH(item)`
          - None
          - add item to the top of the stack
        * - :meth:`POP()`
          - any type
          - returns the item on top of the stack and removes it
        * - :meth:`PEEK()`
          - any type
          - returns the item on top of the stack without removing it
        * - :attr:`LENGTH`
          - :ref:`scalar <scalar>`
          - number of elements in the stack
        * - :meth:`CLEAR()`
          - None
          - remove all elements
        * - :attr:`ITERATOR`
          - :struct:`Iterator`
          - for iterating over the stack
        * - :attr:`COPY`
          - :struct:`Stack`
          - a new copy of this stack
        * - :meth:`CONTAINS(item)`
          - :ref:`boolean <boolean>`
          - check if stack contains an item
        * - :attr:`EMPTY`
          - :ref:`boolean <boolean>`
          - check if stack if empty
        * - :attr:`DUMP`
          - :ref:`string <string>`
          - verbose dump of all contained elements

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

.. attribute:: Stack:LENGTH

    :type: :ref:`scalar <scalar>`
    :access: Get only

    Returns the number of elements in the stack.

.. attribute:: Stack:ITERATOR

    :type: :struct:`Iterator`
    :access: Get only

    An alternate means of iterating over a stack. See: :struct:`Iterator`.

.. attribute:: Stack:COPY

    :type: :struct:`Stack`
    :access: Get only

    Returns a new stack that contains the same thing as the old one.

.. method:: Stack:CONTAINS(item)

    :parameter index: (integer) starting index (from zero)
    :return: :ref:`boolean <boolean>`

    Returns true if the stack contains an item equal to the one passed as an argument

.. attribute:: Stack:EMPTY

    :type: :ref:`boolean <boolean>`
    :access: Get only

    Returns true if the stack has zero items in it.

.. attribute:: Stack:DUMP

    :type: :ref:`string <string>`
    :access: Get only

    Returns a :ref:`string <string>` containing a verbose dump of the stack's contents.

