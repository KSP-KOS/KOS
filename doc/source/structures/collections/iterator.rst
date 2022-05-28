.. _iterator:

Iterator
========

An iterator can be obtained from :attr:`List:ITERATOR` as well as from other places.
An ITERATOR is a
`generic computer programming concept <http://en.wikipedia.org/wiki/Iterator>`__.
In the general case it's a variable type that allows you to get
the value at a position in some collection, as well as increment
to the next item in the collection in order to operate on all
objects in the collection one at a time. In kOS it operates
on :struct:`Lists <List>` and most other collection types.

A loop using an :struct:`Iterator` on a :struct:`List` might look like this::

    // Starting with a list that was built like this
    SET MyList To LIST( "Hello", "Aloha", "Bonjour").

    // It could be looped over like this
    SET MyCurrent TO MyList:ITERATOR.
    PRINT "before the first NEXT, position = " + MyCurrent:INDEX.
    UNTIL NOT MyCurrent:NEXT {
        PRINT "Item at position " + MyCurrent:INDEX + " is [" + MyCurrent:VALUE + "].".
    }

.. highlight:: none

Which would result in this output::

    before the first NEXT, position = -1.
    Item at position 0 is [Hello].
    Item at position 1 is [Aloha].
    Item at position 2 is [Bonjour].

When you first create an iterator by using an ITERATOR suffix of some collection
type like :struct:`List`, :struct:`List`, or even :struct:`String`, the
initial position of the index is always -1, and the current value is always
invalid.  This represents a position just *before the start* of the list of
items.  Only after the first time :attr:`NEXT` is called does the value of
:attr:`VALUE` become usable as the first thing in the collection.

If you want to restart an iteration you must call the ``:ITERATOR`` suffix of
the collection again to obtain a new iterator.  Older versions of kOS had a ``:RESET`` suffix but it was removed as it could not be supported on all types.

Members
-------

.. highlight:: kerboscript

.. structure:: Iterator

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description


        * - :meth:`RESET`
          - n/a
          - (This method has been removed)
        * - :meth:`NEXT`
          - :ref:`boolean <boolean>`
          - Move iterator to the next item
        * - :attr:`ATEND`
          - :ref:`boolean <boolean>`
          - Check if iterator is at the end of the list
        * - :attr:`INDEX`
          - :ref:`scalar <scalar>`
          - Current index starting from zero
        * - :attr:`VALUE`
          - varies
          - The object currently being pointed to


.. method:: Iterator:RESET

    :returns: n/a

    This suffix has been deleted from kOS.

.. method:: Iterator:NEXT

    :returns: :ref:`boolean <boolean>`

    Call this to move the iterator to the next item in the list. Returns true if there is such an item, or false if no such item exists because it's already at the end of the list.

.. attribute:: Iterator:ATEND

    :access: Get only
    :type: :ref:`boolean <boolean>`

    Returns true if the iterator is at the end of the list and therefore cannot be "NEXTed", false otherwise.

.. attribute:: Iterator:INDEX

    :access: Get only
    :type: :ref:`scalar <scalar>` (integer)

    Returns the numerical index of how far you are into the list, starting the counting at 0 for the first item in the list. The last item in the list is numbered N-1, where N is the number of items in the list.

    .. note::

        If you have just created the ITERATOR, then the value of :attr:`Iterator:INDEX` is -1. It only becomes 0 after the first call to :meth:`Iterator:NEXT`.

.. attribute:: Iterator:VALUE

    :access: Get only
    :type: varies

    Returns the thing stored at the current position in the list.
