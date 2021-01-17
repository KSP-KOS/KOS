.. _list:

List
====

A :struct:`List` is a collection of any type in kOS. Many places throughout the system return variables of the :struct:`List` type, and you can create your own :struct:`List` variables as well. One of the places you are likely to find that kOS gives you a :struct:`List` is when you use the :ref:`LIST command <list command>` to list some query into one of your variables.

Constructing a list
-------------------

Numerous built-in functions in kOS return a list.  If you wish
to make your own list from scratch you can do so with the
LIST() built-in function.  You pass a varying number of arguments
into it to pre-populate the list with an initial list of items:
::

    // Make an empty list with zero items in it:
    set mylist to list().
    // Make a list with 3 numbers in it:
    set mylist to list(10,20,30).
    // Make a list with 3 strings in it:
    set mylist to list("10","20","30").
    // Make a two dimensional 2x3 list with heterogenious contents
    // mixing strings and numbers:
    set mylist to list( list("a","b","c"), list(1,2,3) ).

The contents of a list can be any objects you feel like, and do not
need to be of a homogeneous type.

Structure
---------

.. structure:: List

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`Enumerable`
          -
          - :struct:`List` objects are a type of :struct:`Enumerable`
        * - :meth:`ADD(item)`
          - None
          - append an item
        * - :meth:`INSERT(index,item)`
          - None
          - insert item at index
        * - :meth:`REMOVE(index)`
          - None
          - remove item by index
        * - :meth:`CLEAR()`
          - None
          - remove all elements
        * - :attr:`COPY`
          - :struct:`List`
          - a new copy of this list
        * - :meth:`SUBLIST(index,length)`
          - :struct:`List`
          - new list of given length starting with index
        * - :meth:`JOIN(separator)`
          - :ref:`string <string>`
          - joins all list elements into a string
        * - :meth:`FIND(item)`
          - :ref:`scalar <scalar>`
          - returns the first index of the item within the list
        * - :meth:`INDEXOF(item)`
          - :ref:`scalar <scalar>`
          - Alias for :meth:`FIND(item)`
        * - :meth:`FINDLAST(item)`
          - :ref:`scalar <scalar>`
          - returns the last index of the item within the list
        * - :meth:`LASTINDEXOF(item)`
          - :ref:`scalar <scalar>`
          - Alias for :meth:`FINDLAST(item)`

.. note::

    This type is serializable.


.. method:: List:ADD(item)

    :parameter item: (any type) item to be added

    Appends the new value given to the end of the list.

.. method:: List:INSERT(index,item)

    :parameter index: (integer) position in list (starting from zero)
    :parameter item: (any type) item to be added

    Inserts a new value at the position given, pushing all the other values in the list (if any) one spot to the right.

.. method:: List:REMOVE(index)

    :parameter index: (integer) position in list (starting from zero)

    Remove the item from the list at the numeric index given, with counting starting at the first item being item zero

.. method:: List:CLEAR()

    :return: none

    Calling this suffix will remove all of the items currently stored in the :struct:`List`.

.. attribute:: List:COPY

    :type: :struct:`List`
    :access: Get only

    Returns a new list that contains the same thing as the old list.

.. method:: List:SUBLIST(index,length)

    :parameter index: (integer) starting index (from zero)
    :parameter length: (integer) resulting length of returned :struct:`List`
    :return: :struct:`List`

    Returns a new list that contains a subset of this list starting at the given index number, and running for the given length of items.

.. method:: List:JOIN(separator)

    :parameter separator: (string) separator that will be inserted between the list items
    :return: :ref:`string <string>`

    Returns a string created by converting each element of the array to a string, separated by the given separator.

.. method:: List:FIND(item)

    :parameter item: (any type) the item to attempt to find within the list
    :return: :struct:`Scalar`

    Returns the first integer index within the list where an item equal to
    this item can be found.  Whatever the definition of "equals" is for this
    item type will be used to decide if a match is found.  This is a linear
    search from start to finish so it can be slow if the list is long.

    If no such item is found, a ``-1`` is returned.

.. method:: List:INDEXOF(item)

    This is just an alias for :meth:`FIND(item)`.

.. method:: List:FINDLAST(item)

    :parameter item: (any type) the item to attempt to find within the list
    :return: :struct:`Scalar`

    This is the same as :meth:`FIND(item)`, except that it searches
    backward instead of forward through the list.  It finds the lastmost
    element that is equal to the item.

.. method:: List:LASTINDEXOF(item)

    This is just an alias for :meth:`FINDLAST(item)`.

Access to Individual Elements
-----------------------------

All list indexes start counting at zero. (The list elements are numbered from 0 to N-1 rather than from 1 to N.)

``list[expression]``
    operator: another syntax to access the element at position 'expression'. Works for get or set. Any arbitrary complex expression may be used with this syntax, not just a number or variable name. This syntax is preferred over the older "#" syntax, which is kept only for backward compatibility.
``FOR VAR IN LIST { ... }.``
    :ref:`A type of loop <flow>` in which var iterates over all the items of list from item 0 to item LENGTH-1.
``ITERATOR``
    An alternate means of iterating over a list. See :struct:`Iterator`.
``list#x`` *(deprecated)*
    operator: access the element at postion x. Works for get or set. X must be a hardcoded number or a variable name. This is here for backward compatibility. The syntax in the next bullet point is preferred over this.

Examples::

    SET BAR TO LIST(5,3,6).  // Creates a new list with 3 integers in it.
    SET FOO TO LIST().       // Creates a new empty list in FOO variable
    FOO:ADD(5).              // Adds a new element to the end of the list
    FOO:ADD( ALTITUDE ).     // Adds current altitude number to the end of the list
    FOO:ADD(ETA:APOAPSIS).   // Adds current seconds to apoapsis to the end of the list

    // As a reminder, at this point your list, if you did all the above
    // steps in order, would look like this now:
    //
    //  FOO[0] = 5.
    //  FOO[1] = 99999. // or whatever your altitude was when you added it.
    //  FOO[2] = 99. // or whatever your ETA:APOAPSIS was when you added it.

    PRINT FOO:LENGTH.        // Prints 3
    PRINT FOO:LENGTH().      // Also prints 3.  LENGTH is a method that, because it takes zero arguments, can omit the parentheses.
    PRINT FOO#0.             // Prints 5, using deprecated old '#' syntax.
    PRINT FOO[0].            // Prints 5, using newer preferred '[]' syntax.
    PRINT FOO[1].            // Prints altitude number.
    PRINT FOO[2].            // Prints eta:apoapsis number.
    SET x TO 2. PRINT FOO#x. // Prints the same thing as FOO[2], using deprecated old '#' syntax.
    SET x TO 2. PRINT FOO[x].// Prints the same thing as FOO[2].
    SET y to 3. PRINT FOO[ y/3 + 1 ].
                             // Prints the same thing as FOO#2, using a mathematical expression as the index.
    SET FOO#0 to 4.          // Replace the 5 at position 0 with a 4.
    FOO:INSERT(0,"skipper 1"). // Inserts the string "skipper 1" to the start of the list, pushing the rest of the contents right.
    FOO:INSERT(2,"skipper 2"). // Inserts the string "skipper 2" at position 2 of the list, pushing the rest of the contents right.

    // As a reminder, at this point your list, if you did all the above
    // steps in order, would look like this now:
    //
    //  FOO[0] = "skipper 1".
    //  FOO[1] = 5.
    //  FOO[2] = "skipper 2".
    //  FOO[3] = 99999. // or whatever your altitude was when you added it.
    //  FOO[4] = 99. // or whatever your ETA:APOAPSIS was when you added it.

    FOO:REMOVE( 1).              // Removes the element at index 1 from the list, moving everything else back one.
    FOO:REMOVE(FOO:LENGTH - 1).  // Removes whatever element happens to be at the end of the list, at position length-1.

    // As a reminder, at this point your list, if you did all the above
    // steps in order, would look like this now:
    //
    //  FOO[0] = "skipper 1".
    //  FOO[1] = "skipper 2".
    //  FOO[2] = 99999. // or whatever your altitude was when you added it.

    SET BAR TO FOO:COPY.     // Makes a copy of the FOO list
    FOO:CLEAR.               // Removes all elements from the FOO list.
    FOO:CLEAR().             // Also removes all elements from the FOO list.  The parentheses are optional because the method takes zero arguments.
    FOR var in BAR {         // --.
      print var.             //   |-- Print all the contents of FOO.
    }.                       // --'

Multidimensional Arrays
-----------------------

A 2-D array is a :struct:`List` who's elements are themselves also :struct:`Lists`. A 3-D array is a :struct:`List` of :struct:`Lists <List>` of :struct:`Lists <List>`. Any number of dimensions is possible.

``list[x][y]`` (or ``list#x#y``)
    Access the element at position x,y of the 2-D array (list of lists). The use of the '#' syntax is deprecated and exists for backward compatibility only. The newer '[]' square-bracket syntax is preferred.

* The elements of the array need not be uniform (any mix of strings, numbers, structures is allowed).
* The dimensions of the array need not be uniform (row 1 might have 3 columns while row 2 has 5 columns)::

    SET FOO TO LIST(). // Empty list.
    FOO:ADD( LIST() ). // Element 0 is now itself a list.
    FOO[0]:ADD( "A" ). // Element 0,0 is now "A".
    FOO[0]:ADD( "B" ). // Element 0,1 is now "B".
    FOO:ADD(LIST()).   // Element 1 is now itself a list.
    FOO[1]:ADD(10).    // Element 1,0 is now 10.
    FOO[1]:ADD(20).    // Element 1,1 is now 20.
    FOO:ADD(LIST()).   // Element 2 is now itself a list.

    FOO[ FOO:LENGTH -1 ]:ADD(3.14159).
        // Element 2,0 is now 3.1519, using a more complex
        //     expression to dynamically obtain the current
        //     maximum index of '2'.

    FOO[ FOO:LENGTH -1 ]:ADD(7).
        // Element 2,1 is now 7, using a more complex
        //     expression to dynamically obtain the current
        //     maximum index of '2'.

    // FOO is now a 2x3 matrix looking like this:
    //    A         B
    //    10        20
    //    3.14159   7

    // or like this, depending on how you want
    // to visualize it as a row-first or column-first table:
    //    A    10     3.14159
    //    B    20     7

    PRINT FOO[0][0]. // Prints A.
    PRINT FOO[0][1]. // Prints B.
    PRINT FOO[1][0]. // Prints 10.
    PRINT FOO[1][1]. // Prints 20.
    PRINT FOO[2][0]. // Prints 3.14159.
    PRINT FOO[2][1]. // Prints 7.

    PRINT FOO#2#0.   // Prints 3.14159, using deprecated syntax.

Comparing two lists
-------------------

Note that if you have two lists, LISTA and LISTB, and you tried to compare
if they were the same, in this way::

    if LISTA = LISTB {
      print "they are equal".
    }

Then the check will only be true if LISTA and LISTB are both actually the
same list - not just two lists with equal contents, but in fact just two
variables pointing to the same list.

This is because a LIST is a complex structure object, and like most complex
structure objects, the equality check is just testing whether or not
they refer to the same object, not whether or not they have equivalent
content.

To test if the contents are equivalent, you have to check them item
by item, like so::

    set still_same to true.
    FROM {local i is 0.}
      UNTIL i > LISTA:LENGTH or not still_same
      STEP {set i to i + 1.}
    DO
    {
      set still_same to (LISTA[i] = LISTB[i]).
    }
    if still_same {
      print "they are equal".
    }
