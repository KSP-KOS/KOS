.. _enumerable:

Enumerable
==========

:struct:`Enumerable` is a parent structure that contains a set of suffixes common to few structures in kOS. As a user of kOS you will never handle pure instances of this structure,
but rather concrete types like :struct:`List`, :struct:`Range`, :struct:`Queue` etc.

.. BOGUS_TODO_DIRECTIVE_TO_MAKE_SPHINX_COMPLAIN:: REMEMBER THIS FILE NEEDS TO BE EDITIED FOR REFS TO SCALARS, BOOLEANS, STRINGS


Structure
---------

.. structure:: Enumerable

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`ITERATOR`
          - :struct:`Iterator`
          - for iterating over the elements
        * - :attr:`REVERSEITERATOR`
          - :struct:`Iterator`
          - for iterating over the elements in the reverse order
        * - :attr:`LENGTH`
          - integer
          - number of elements in the enumerable
        * - :meth:`CONTAINS(item)`
          - boolean
          - check if enumerable contains an item
        * - :attr:`EMPTY`
          - boolean
          - check if enumerable is empty
        * - :attr:`DUMP`
          - string
          - verbose dump of all contained elements


.. attribute:: Enumerable:ITERATOR

    :type: :struct:`Iterator`
    :access: Get only

    An alternate means of iterating over an enumerable. See: :struct:`Iterator`.

.. attribute:: Enumerable:REVERSEITERATOR

    :type: :struct:`Iterator`
    :access: Get only

    An alternate means of iterating over an enumerable. Order of items is reversed. See: :struct:`Iterator`.

.. attribute:: Enumerable:LENGTH

    :type: integer
    :access: Get only

    Returns the number of elements in the enumerable.

.. method:: Enumerable:CONTAINS(item)

    :parameter item: element whose presence in the enumerable should be checked
    :return: boolean

    Returns true if the enumerable contains an item equal to the one passed as an argument

.. attribute:: Enumerable:EMPTY

    :type: boolean
    :access: Get only

    Returns true if the enumerable has zero items in it.

.. attribute:: Enumerable:DUMP

    :type: string
    :access: Get only

    Returns a string containing a verbose dump of the enumerable's contents.

