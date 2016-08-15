.. _uniqueset:

UniqueSet
=========

A :struct:`UniqueSet` is a collection of any type in kOS. It doesn't store items in any particular order and does not allow duplicate items.
You can read more about sets on `Wikipedia <https://en.wikipedia.org/wiki/Set_(abstract_data_type)>`_.

Usage example::

  SET S TO UNIQUESET(1,2,3).
  PRINT S:LENGTH. // will print 3
  S:ADD(1). // 1 was already in the set so nothing happens
  PRINT S:LENGTH. // will print 3 again


Structure
---------

.. structure:: UniqueSet

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`Enumerable`
          -
          - :struct:`UniqueSet` objects are a type of :struct:`Enumerable`
        * - :meth:`ADD(item)`
          - None
          - append an item
        * - :meth:`REMOVE(item)`
          - None
          - remove item
        * - :meth:`CLEAR()`
          - None
          - remove all elements
        * - :attr:`COPY`
          - :struct:`UniqueSet`
          - a new copy of this set

.. note::

    This type is serializable.


.. method:: UniqueSet:ADD(item)

    :parameter item: (any type) item to be added

    Appends the new value given.

.. method:: UniqueSet:REMOVE(item)

    :parameter item: (any type) item to be removed

    Remove the item from the set.

.. method:: UniqueSet:CLEAR()

    :return: none

    Calling this suffix will remove all of the items currently stored in the set.

.. attribute:: UniqueSet:COPY

    :type: :struct:`UniqueSet`
    :access: Get only

    Returns a new set that contains the same thing as the old set.
