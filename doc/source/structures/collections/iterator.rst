.. _iterator:

Iterator
========

An iterator can be obtained from :attr:`List:ITERATOR`. Once a :struct:`List` has given you an :struct:`Iterator` object, you can use it to access elements inside the :struct:`List`. An ITERATOR is a `generic computer programming concept <http://en.wikipedia.org/wiki/Iterator>`__. In the general case it's a variable type that allows you to get the value at a position in some collection, as well as increment to the next item in the collection in order to operate on all objects in the collection one at a time. In kOS it operates on :struct:`Lists <List>`.

A loop using an :struct:`Iterator` on a :struct:`List` might look like this::

    // Starting with a list that was built like this
    SET MyList To LIST( "Hello", "Aloha", "Bonjour").

    // It could be looped over like this
    SET MyCurrent TO MyList:ITERATOR.
    MyCurrent:RESET().
    PRINT "After reset, position = " + MyCurrent:INDEX.
    UNTIL NOT MyCurrent:NEXT {
        PRINT "Item at position " + MyIter:INDEX + " is [" + MyIter:VALUE + "].".
    }

.. hightlight:: none

Which would result in this output::

    After reset, position = -1.
    Item at position 0 is [Hello].
    Item at position 1 is [Aloha].
    Item at position 2 is [Bonjour].

.. highlight:: kerboscript

.. structure:: Iterator

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description


        * - :meth:`RESET`
          -
          - Rewind to the just before the beginning
        * - :meth:`NEXT`
          - boolean
          - Move iterator to the next item
        * - :attr:`ATEND`
          - boolean
          - Check if iterator is at the end of the list
        * - :attr:`INDEX`
          - integer
          - Current index starting from zero
        * - :attr:`VALUE`
          - varies
          - The object currently being pointed to


.. method:: Iterator:RESET

    Call this to rewind the iterator to just before the beginning of the list. After a call to :meth:`Iterator:RESET`, the iterator must be moved with :meth:`Iterator:NEXT` before it gets to the first value in the list.

.. method:: Iterator:NEXT

    :returns: boolean

    Call this to move the iterator to the next item in the list. Returns true if there is such an item, or false if no such item exists because it's already at the end of the list.

.. attribute:: Iterator:ATEND

    :access: Get only
    :type: boolean

    Returns true if the iterator is at the end of the list and therefore cannot be "NEXTed", false otherwise.

.. attribute:: Iterator:INDEX

    :access: Get only
    :type: integer

    Returns the numerical index of how far you are into the list, starting the counting at 0 for the first item in the list. The last item in the list is numbered N-1, where N is the number of items in the list.

    .. note::

        If you have just used :meth:`Iterator:RESET` or have just created the ITERATOR, then the value of :attr:`Iterator:INDEX` is -1. It only becomes 0 after the first call to :meth:`Iterator:NEXT`.

.. attribute:: Iterator:VALUE

    :access: Get only
    :type: varies

    Returns the thing stored at the current position in the list.
